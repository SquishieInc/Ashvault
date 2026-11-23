using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    [Serializable]
    public sealed class SectorData
    {
        public string name;
        public int minX, minZ, maxX, maxZ;
        public float floor, ceiling;
        public bool Contains(int x, int z) => x >= minX && x <= maxX && z >= minZ && z <= maxZ;
    }

    [Serializable]
    public sealed class EnemySpawnData
    {
        public int x, z;
        public string kind, mode;
    }

    [Serializable]
    public sealed class ClassDropData
    {
        public int classId, x, z;
    }

    [Serializable]
    public sealed class DecorationData
    {
        public int x, z;
        public string kind;
        public float width = 1.8f, height = 2.4f;
        public bool glow;
    }

    [Serializable]
    public sealed class FixtureData
    {
        public int x, z;
        public string kind;
        public bool shadows;
    }

    [Serializable]
    public sealed class MechanismData
    {
        public int x, z;
        public string kind;
        public float travel = 1f, period = 5f, phase;
        public float axisX, axisZ;
        public int damage = 16;
    }

    [Serializable]
    public sealed class PolygonSectorData
    {
        public string id = "sector", kind = "Platform";  // Platform, Solid or Pit
        public Vector2[] vertices;
        public float floor = .25f, ceiling = 4f;
    }

    [Serializable]
    public sealed class StaircaseData
    {
        public string id = "stairs";
        public float x, z, axisX = 0f, axisZ = 1f;
        public int steps = 4;
        public float width = 1f, depth = .55f, baseHeight = 0f, rise = .25f;
    }

    [Serializable]
    public sealed class WallSegmentData
    {
        public string id = "wall";
        public float x1, z1, x2, z2, bottom = 0f, top = 4f, thickness = .25f;
        public bool secret = false, blocksMovement = true;
    }

    [Serializable]
    public sealed class DoorSwitchData
    {
        public int x,z,targetX,targetZ;
        public string name;
    }

    [Serializable]
    public sealed class AnimatedSurfaceData
    {
        public int x, z;
        public string kind;
        public float width = .9f;
    }

    [Serializable]
    public sealed class LevelBlueprint
    {
        public string id, title, episodeTitle, intro;
        public int episode, level, width, height;
        public string[] rows;
        public SectorData[] sectors;
        public EnemySpawnData[] enemies;
        public ClassDropData[] classDrops;
        public DecorationData[] decorations;
        public FixtureData[] fixtures;
        public MechanismData[] mechanisms;
        public PolygonSectorData[] polygonSectors;
        public WallSegmentData[] wallSegments;
        public StaircaseData[] stairs;
        public DoorSwitchData[] switches;
        public AnimatedSurfaceData[] animatedSurfaces;
    }

    public static class CampaignCatalog
    {
        public const int Episodes = 3;
        public const int MapsPerEpisode = 3;
        public static LevelBlueprint Load(int episode, int map)
        {
            string id = "E" + episode.ToString("00") + "_L" + map.ToString("00");
            var text = Resources.Load<TextAsset>("Maps/" + id);
            if (text == null)
            {
                Debug.LogError("Missing Ashvault campaign map " + id);
                return null;
            }
            var blueprint = JsonUtility.FromJson<LevelBlueprint>(text.text);
            if (blueprint == null || blueprint.rows == null || blueprint.rows.Length != blueprint.height)
                throw new InvalidOperationException("Invalid Ashvault map " + id);
            foreach (string row in blueprint.rows)
                if (row.Length != blueprint.width) throw new InvalidOperationException("Unequal map row in " + id);
            return blueprint;
        }
    }

    // The same map file drives physical geometry, enemy navigation and progression.
    // Cells with unopened seals and secrets are never traversable to AI.
    public sealed class DungeonMap
    {
        public const float Tile = 3f;
        public readonly LevelBlueprint Level;
        private readonly char[,] cells;
        private readonly bool[,] opened;
        private readonly float[,] elevations;
        private readonly float[,] dynamicElevations;
        private readonly bool[,] hasDynamicElevation;
        private readonly HashSet<int> openedPolygonWalls = new HashSet<int>();
        public int Width => Level.width;
        public int Height => Level.height;

        public DungeonMap(LevelBlueprint level)
        {
            Level = level;
            cells = new char[Width, Height];
            opened = new bool[Width, Height];
            elevations = new float[Width, Height];
            dynamicElevations = new float[Width, Height];
            hasDynamicElevation = new bool[Width, Height];
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                {
                    cells[x, z] = level.rows[Height - 1 - z][x];
                    float elevation = 0f;
                    if (level.sectors != null)
                        foreach (var sector in level.sectors)
                            if (sector.Contains(x, z)) elevation = Mathf.Max(0f, sector.floor);
                    elevations[x, z] = elevation;
                }
        }

        public bool Inside(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Height;
        public char Cell(int x, int z) => Inside(x, z) ? cells[x, z] : '#';
        public float Surface(int x, int z)
        {
            if (!Inside(x,z)) return 0f;
            if (hasDynamicElevation[x,z]) return dynamicElevations[x,z];
            float height = elevations[x,z];
            if (Level.polygonSectors != null)
                foreach (var poly in Level.polygonSectors)
                    if (poly.kind != "Solid" && SectorPolygon.Contains(poly.vertices, new Vector2(x,z)))
                        height = poly.floor;
            if (Level.stairs != null)
                foreach (var stair in Level.stairs)
                {
                    Vector2 d = new Vector2(stair.axisX,stair.axisZ).normalized;
                    if (d.sqrMagnitude <.5f) d=Vector2.up;
                    Vector2 relative = new Vector2(x-stair.x,z-stair.z);
                    float forward=Vector2.Dot(relative,d);
                    float side=Mathf.Abs(relative.x*d.y-relative.y*d.x);
                    if (forward >= 0 && forward < Mathf.Max(1,stair.steps)*stair.depth &&
                        side <= stair.width*.5f)
                        height = Mathf.Max(height,stair.baseHeight+
                            (Mathf.FloorToInt(forward/Mathf.Max(.1f,stair.depth))+1)*stair.rise);
                }
            return height;
        }
        public bool IsPit(int x,int z)
        {
            if(Level.polygonSectors==null)return false;
            foreach(var p in Level.polygonSectors)
                if(p.kind=="Pit" && SectorPolygon.Contains(p.vertices,new Vector2(x,z)))return true;
            return false;
        }
        public void UnsealPolygonWall(int index) => openedPolygonWalls.Add(index);
        public void SetDynamicFloor(int x, int z, float height)
        {
            if (!Inside(x, z)) return;
            dynamicElevations[x, z] = height;
            hasDynamicElevation[x, z] = true;
        }
        public void ClearDynamicFloor(int x, int z)
        {
            if (Inside(x, z)) hasDynamicElevation[x, z] = false;
        }
        public Vector3 World(int x, int z) => new Vector3(x * Tile, Surface(x, z), z * Tile);
        public Vector2Int ToCell(Vector3 world) => new Vector2Int(
            Mathf.RoundToInt(world.x / Tile), Mathf.RoundToInt(world.z / Tile));

        public void Unseal(int x, int z)
        {
            if (Inside(x, z)) opened[x, z] = true;
        }

        public bool IsOpen(int x, int z)
        {
            if (!Inside(x, z)) return false;
            char c = cells[x, z];
            if (c == '#') return false;
            if (Level.polygonSectors != null)
                foreach (var p in Level.polygonSectors)
                    if (p.kind == "Solid" && SectorPolygon.Contains(p.vertices,new Vector2(x,z))) return false;
            if (c == 's' || c == 'B' || c == 'A' || c == 'C' || c == 'Z' ||
                (c >= '1' && c <= '8')) return opened[x, z];
            return true;
        }

        public bool CanStep(Vector2Int from, Vector2Int to)
        {
            if (!IsOpen(to.x,to.y) || !Inside(from.x,from.y) ||
                Mathf.Abs(Surface(from.x,from.y)-Surface(to.x,to.y))>.43f) return false;
            if (Level.wallSegments != null)
                for (int i=0;i<Level.wallSegments.Length;i++)
                {
                    var w = Level.wallSegments[i];
                    if (!w.blocksMovement || openedPolygonWalls.Contains(i)) continue;
                    if (SectorPolygon.SegmentsCross(new Vector2(from.x,from.y),new Vector2(to.x,to.y),
                        new Vector2(w.x1,w.z1),new Vector2(w.x2,w.z2)))return false;
                }
            return true;
        }

        public Vector2Int Locate(char marker)
        {
            for (int z = 0; z < Height; z++)
                for (int x = 0; x < Width; x++)
                    if (cells[x, z] == marker) return new Vector2Int(x, z);
            throw new InvalidOperationException("Marker " + marker + " missing from " + Level.id);
        }

        public bool FindNearbyFree(Vector2Int origin, int radius, out Vector2Int result)
        {
            for (int distance = 0; distance <= radius; distance++)
                for (int dx = -distance; dx <= distance; dx++)
                    for (int dz = -distance; dz <= distance; dz++)
                    {
                        var cell = origin + new Vector2Int(dx, dz);
                        if (Mathf.Abs(dx) + Mathf.Abs(dz) != distance || !IsOpen(cell.x, cell.y)) continue;
                        result = cell;
                        return true;
                    }
            result = origin;
            return false;
        }
    }

    // Small, deterministic 4-way grid search; no runtime NavMesh bake or external package.
    // When doors or secrets open, the next search automatically sees the new route.
    public static class GridPathfinder
    {
        private static readonly Vector2Int[] Directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1) };

        public static bool Find(DungeonMap map, Vector3 origin, Vector3 destination, List<Vector3> path)
        {
            path.Clear();
            if (map == null) return false;
            Vector2Int start = map.ToCell(origin), goal = map.ToCell(destination);
            if (!map.IsOpen(start.x, start.y) || !map.IsOpen(goal.x, goal.y)) return false;
            int w = map.Width, h = map.Height;
            var visited = new bool[w, h];
            var previous = new Vector2Int[w, h];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start); visited[start.x, start.y] = true;
            bool found = start == goal;
            while (queue.Count > 0 && !found)
            {
                var here = queue.Dequeue();
                foreach (var direction in Directions)
                {
                    var next = here + direction;
                    if (!map.Inside(next.x, next.y) || visited[next.x, next.y] ||
                        !map.CanStep(here, next)) continue;
                    visited[next.x, next.y] = true;
                    previous[next.x, next.y] = here;
                    if (next == goal) { found = true; break; }
                    queue.Enqueue(next);
                }
            }
            if (!found) return false;
            var chain = new List<Vector2Int>();
            Vector2Int at = goal;
            while (at != start)
            {
                chain.Add(at);
                at = previous[at.x, at.y];
                if (chain.Count > w * h) return false;
            }
            chain.Reverse();
            foreach (var cell in chain) path.Add(map.World(cell.x, cell.y));
            return true;
        }
    }
}
