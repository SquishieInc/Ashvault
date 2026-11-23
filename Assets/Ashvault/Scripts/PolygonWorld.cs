using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    // Polygon sectors use editor-friendly cell-space X/Z coordinates, not grid-cell rectangles.
    // The floor outline accepts simple clockwise or counter-clockwise concave polygons.
    // Ear clipping intentionally rejects self-intersections and zero-area outlines.
    public static class SectorPolygon
    {
        public static bool Contains(Vector2[] ring, Vector2 point)
        {
            if (ring == null || ring.Length < 3) return false;
            bool inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                Vector2 a = ring[i], b = ring[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x-a.x)*(point.y-a.y)/(b.y-a.y) + a.x) inside = !inside;
            }
            return inside;
        }
        private static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
            (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        public static float SignedArea(Vector2[] ring)
        {
            if (ring == null || ring.Length < 3) return 0;
            float area = 0;
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i+1)%ring.Length];
                area += a.x*b.y-b.x*a.y;
            }
            return area*.5f;
        }
        public static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // Proper intersection test; common endpoints are permitted for adjoining walls.
            const float epsilon = .00001f;
            float ac = Cross(a,b,c), ad = Cross(a,b,d), ca = Cross(c,d,a), cb = Cross(c,d,b);
            return (ac*ad < -epsilon) && (ca*cb < -epsilon);
        }
        public static bool IsSimple(Vector2[] points)
        {
            if (points == null || points.Length < 3 || Mathf.Abs(SignedArea(points)) < .0001f) return false;
            for (int a = 0; a < points.Length; a++)
            {
                if (Vector2.Distance(points[a],points[(a+1)%points.Length]) < .001f) return false;
                for (int b = a+1; b < points.Length; b++)
                {
                    if (b==a || (a+1)%points.Length==b || (b+1)%points.Length==a) continue;
                    if (SegmentsCross(points[a],points[(a+1)%points.Length],
                        points[b],points[(b+1)%points.Length])) return false;
                }
            }
            return true;
        }
        public static bool Triangulate(Vector2[] ring, List<int> tris)
        {
            tris.Clear();
            if (!IsSimple(ring)) return false;
            var available = new List<int>();
            if (SignedArea(ring) > 0f)
            {
                for (int i=0;i<ring.Length;i++) available.Add(i);
            }
            else for (int i=ring.Length-1;i>=0;i--) available.Add(i);
            int safety=ring.Length*ring.Length;
            while (available.Count>3 && safety-- > 0)
            {
                bool removed=false;
                for (int i=0;i<available.Count;i++)
                {
                    int ia=available[(i+available.Count-1)%available.Count];
                    int ib=available[i];
                    int ic=available[(i+1)%available.Count];
                    if (Cross(ring[ia],ring[ib],ring[ic]) <= .00001f) continue;
                    bool blocked=false;
                    for (int j=0;j<available.Count;j++)
                    {
                        int p=available[j];if(p==ia||p==ib||p==ic)continue;
                        if (InsideTriangle(ring[p],ring[ia],ring[ib],ring[ic])) { blocked=true;break; }
                    }
                    if (blocked) continue;
                    tris.Add(ia);tris.Add(ib);tris.Add(ic);
                    available.RemoveAt(i);removed=true;break;
                }
                if (!removed) {tris.Clear();return false;}
            }
            if (available.Count!=3) {tris.Clear();return false;}
            tris.Add(available[0]);tris.Add(available[1]);tris.Add(available[2]);
            return true;
        }
        private static bool InsideTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            const float eps=.00001f;
            return Cross(a,b,p)>=-eps && Cross(b,c,p)>=-eps && Cross(c,a,p)>=-eps;
        }
    }

    public static class PolygonWorldBuilder
    {
        // Create floors and vertical perimeter faces from arbitrary polygon outlines.
        public static void Build(AshGame game, Transform parent, DungeonMap map, Material walls, Material floors)
        {
            if (map.Level.polygonSectors != null)
                foreach (var data in map.Level.polygonSectors)
                {
                    if (data.vertices == null || !SectorPolygon.IsSimple(data.vertices))
                    {
                        Debug.LogWarning("Ashvault: skipping invalid polygon " + data.id);
                        continue;
                    }
                    BuildPolygon(parent, data, walls, floors);
                }
            if (map.Level.stairs != null)
                foreach (var stair in map.Level.stairs) BuildStairs(parent, stair, floors);
            if (map.Level.wallSegments != null)
                for (int i = 0; i < map.Level.wallSegments.Length; i++)
                    BuildWall(game, parent, map, map.Level.wallSegments[i], i, walls);
        }

        private static void BuildPolygon(Transform parent, PolygonSectorData data, Material walls, Material floors)
        {
            var indices = new List<int>();
            if (!SectorPolygon.Triangulate(data.vertices, indices)) return;
            int n=data.vertices.Length;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            float surface=data.floor;
            // Unity uses clockwise front-facing triangle winding from above.
            for(int i=0;i<n;i++)
            {
                Vector2 v=data.vertices[i];vertices.Add(new Vector3(v.x*DungeonMap.Tile,surface,v.y*DungeonMap.Tile));
                uv.Add(v);
            }
            for(int i=0;i<indices.Count;i+=3)
            { triangles.Add(indices[i]);triangles.Add(indices[i+2]);triangles.Add(indices[i+1]); }
            float wallBottom=data.kind=="Pit"? data.floor : 0f;
            float wallTop=data.kind=="Pit"? 0f :
                data.kind=="Solid"? Mathf.Max(data.ceiling,data.floor):surface;
            if (Mathf.Abs(wallTop-wallBottom)>.02f)
            {
                for(int i=0;i<n;i++)
                {
                    Vector2 a=data.vertices[i],b=data.vertices[(i+1)%n];
                    int k=vertices.Count;
                    vertices.Add(new Vector3(a.x*DungeonMap.Tile,wallBottom,a.y*DungeonMap.Tile));
                    vertices.Add(new Vector3(b.x*DungeonMap.Tile,wallBottom,b.y*DungeonMap.Tile));
                    vertices.Add(new Vector3(a.x*DungeonMap.Tile,wallTop,a.y*DungeonMap.Tile));
                    vertices.Add(new Vector3(b.x*DungeonMap.Tile,wallTop,b.y*DungeonMap.Tile));
                    float width=Vector2.Distance(a,b);
                    uv.Add(new Vector2(0,0));uv.Add(new Vector2(width,0));
                    uv.Add(new Vector2(0,wallTop-wallBottom));uv.Add(new Vector2(width,wallTop-wallBottom));
                    triangles.Add(k);triangles.Add(k+2);triangles.Add(k+1);
                    triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+3);
                }
            }
            var mesh=new Mesh { name="Ashvault polygon / "+data.id };
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("POLYGON / "+data.kind+" / "+data.id);
            go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=data.kind=="Solid"?walls:floors;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        private static void BuildStairs(Transform parent, StaircaseData d, Material material)
        {
            int steps=Mathf.Clamp(d.steps,1,30);
            Vector2 direction=new Vector2(d.axisX,d.axisZ).normalized;
            if (direction.sqrMagnitude <.5f) direction=Vector2.up;
            float run=Mathf.Max(.4f,d.depth);
            float width=Mathf.Max(.4f,d.width);
            for (int i=0;i<steps;i++)
            {
                float rise=Mathf.Max(.08f,d.rise);
                float top=d.baseHeight+rise*(i+1);
                var stair=GameObject.CreatePrimitive(PrimitiveType.Cube);
                stair.name="STAIR / "+d.id+" / "+(i+1);
                stair.transform.SetParent(parent,false);
                Vector2 at=new Vector2(d.x,d.z)+direction*(i+.5f)*run;
                stair.transform.position=new Vector3(at.x*DungeonMap.Tile,top*.5f,at.y*DungeonMap.Tile);
                stair.transform.localScale=new Vector3(width*DungeonMap.Tile,Mathf.Max(.08f,top),run*DungeonMap.Tile);
                stair.transform.rotation=Quaternion.Euler(0f,Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg,0f);
                stair.GetComponent<Renderer>().sharedMaterial=material;
            }
        }

        private static void BuildWall(AshGame game,Transform parent,DungeonMap map,WallSegmentData d,int index,Material stone)
        {
            Vector3 a=new Vector3(d.x1*DungeonMap.Tile,d.bottom,d.z1*DungeonMap.Tile);
            Vector3 b=new Vector3(d.x2*DungeonMap.Tile,d.bottom,d.z2*DungeonMap.Tile);
            Vector3 line=b-a;
            if (line.sqrMagnitude<.01f)return;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name=(d.secret?"SECRET WALL / ":"FREEFORM WALL / ")+d.id;
            wall.transform.SetParent(parent,false);
            wall.transform.position=(a+b)*.5f+Vector3.up*((d.top-d.bottom)*.5f);
            wall.transform.localScale=new Vector3(line.magnitude,Mathf.Max(.1f,d.top-d.bottom),Mathf.Max(.12f,d.thickness));
            wall.transform.rotation=Quaternion.Euler(0f,-Mathf.Atan2(line.z,line.x)*Mathf.Rad2Deg,0f);
            wall.GetComponent<Renderer>().sharedMaterial=stone;
            if (d.secret)
            {
                wall.AddComponent<PolygonSecretWall>().Configure(game,map,index);
                game.RegisterSecret();
            }
        }
    }

    public sealed class PolygonSecretWall : MonoBehaviour, IUsable
    {
        private DungeonMap map;
        private AshGame game;
        private int index;
        private float elapsed;
        private bool opening;
        private Vector3 initial;
        public void Configure(AshGame owner,DungeonMap level,int wallIndex)
        {game=owner;map=level;index=wallIndex;initial=transform.position;}
        public void Use(AshPlayer actor)
        {
            if (opening)return;
            opening=true;
            actor.Owner.CollectSecret();
            AshAudio.OneShot("secret_reveal",transform.position);
        }
        private void Update()
        {
            if(!opening)return;
            elapsed+=Time.deltaTime;
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/1.15f));
            transform.position=initial+Vector3.down*5.6f*t;
            if(t<1f)return;
            var collider=GetComponent<Collider>();if(collider!=null)collider.enabled=false;
            map.UnsealPolygonWall(index);
            enabled=false;
        }
    }
}
