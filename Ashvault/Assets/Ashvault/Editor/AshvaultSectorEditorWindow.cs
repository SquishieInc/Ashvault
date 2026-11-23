#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ashvault.EditorTools
{
    // Source-of-truth authoring window writes the existing nine Resources/Maps/*.json files.
    // 2D canvas: polygon mode left-click adds vertices, drag an existing vertex to move it,
    // right-click a vertex to remove it; walls use two consecutive clicks; stairs one click.
    public sealed class AshvaultSectorEditorWindow : EditorWindow
    {
        private LevelBlueprint level;
        private int chosenMap, selectedPolygon = -1, selectedWall = -1, selectedStair = -1;
        private string loadedId;
        private int dragging=-1;
        private bool awaitingWall;
        private Vector2 firstWallPoint;
        private bool dirty;
        private enum EditTool { Polygon, Wall, Stair }
        private EditTool tool;
        private readonly string[] maps = Enumerable.Range(1,3)
            .SelectMany(e=>Enumerable.Range(1,3).Select(m=>"E"+e.ToString("00")+"_L"+m.ToString("00")))
            .ToArray();

        [MenuItem("Tools/Ashvault/Open Polygon Sector Editor")]
        public static void Open() {GetWindow<AshvaultSectorEditorWindow>("Ashvault Sectors");}
        private void OnEnable() {Load();}
        private void Load()
        {
            if(chosenMap<0||chosenMap>=maps.Length)chosenMap=0;
            loadedId=maps[chosenMap];
            var text=AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Ashvault/Resources/Maps/"+loadedId+".json");
            if(text==null){level=null;return;}
            level=JsonUtility.FromJson<LevelBlueprint>(text.text);
            if(level.polygonSectors==null)level.polygonSectors=Array.Empty<PolygonSectorData>();
            if(level.wallSegments==null)level.wallSegments=Array.Empty<WallSegmentData>();
            if(level.stairs==null)level.stairs=Array.Empty<StaircaseData>();
            selectedPolygon=level.polygonSectors.Length-1;
            selectedWall=level.wallSegments.Length-1;
            selectedStair=level.stairs.Length-1;
            dirty=false;
            Repaint();
        }
        private bool Validate(out string error)
        {
            error="";
            if(level==null){error="Map not loaded";return false;}
            foreach(var p in level.polygonSectors)
            {
                if(!SectorPolygon.IsSimple(p.vertices)) {error="Invalid polygon: "+p.id;return false;}
                foreach(var v in p.vertices)
                    if(v.x<0||v.y<0||v.x>=level.width||v.y>=level.height)
                    {error="Out-of-bounds vertex: "+p.id;return false;}
            }
            foreach(var w in level.wallSegments)
                if((new Vector2(w.x1,w.z1)-new Vector2(w.x2,w.z2)).sqrMagnitude<.001f)
                {error="Zero-length wall: "+w.id;return false;}
            return true;
        }
        private void Save()
        {
            if(!Validate(out string error)){EditorUtility.DisplayDialog("Cannot save",error,"OK");return;}
            string path=Path.Combine(Application.dataPath,"Ashvault/Resources/Maps/"+loadedId+".json");
            File.WriteAllText(path,JsonUtility.ToJson(level,true)+"\n");
            AssetDatabase.ImportAsset("Assets/Ashvault/Resources/Maps/"+loadedId+".json");
            dirty=false;
            Debug.Log("Ashvault sector edit saved: "+loadedId);
        }
        private void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int next=EditorGUILayout.Popup(chosenMap,maps,GUILayout.Width(115));
                if(next!=chosenMap){if(dirty&&!EditorUtility.DisplayDialog("Unsaved changes","Discard edits?","Discard","Cancel"))next=chosenMap;
                    else {chosenMap=next;Load();}}
                GUILayout.Label(level!=null?level.title:"Map not found",EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Reload",EditorStyles.toolbarButton,GUILayout.Width(65)))Load();
                GUI.enabled=level!=null;
                if(GUILayout.Button(dirty?"Save *":"Save",EditorStyles.toolbarButton,GUILayout.Width(65)))Save();
                GUI.enabled=true;
            }
            if(level==null){EditorGUILayout.HelpBox("Missing campaign JSON",MessageType.Error);return;}
            EditorGUILayout.HelpBox("Polygon: click empty space to add points; drag a point to move; right-click a point to remove. " +
                "Wall: two clicks create a segment. Stair: click to create a staircase origin. Save commits to the actual JSON map.",MessageType.Info);
            tool=(EditTool)GUILayout.Toolbar((int)tool,new[]{"Polygon sector","Wall / secret","Stairs"});
            DrawInspector();
            Rect canvas=GUILayoutUtility.GetRect(340,1000,350,720,GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(canvas,new Color(.09f,.11f,.13f));
            float unit=Mathf.Min((canvas.width-26)/level.width,(canvas.height-26)/level.height);
            Rect mapRect=new Rect(canvas.x+(canvas.width-level.width*unit)*.5f,
                canvas.y+(canvas.height-level.height*unit)*.5f,level.width*unit,level.height*unit);
            DrawMap(mapRect,unit);
            HandleCanvas(mapRect,unit);
        }
        private void DrawInspector()
        {
            if(tool==EditTool.Polygon)
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("+ New polygon",GUILayout.Width(125)))
                    {
                        Array.Resize(ref level.polygonSectors,level.polygonSectors.Length+1);
                        selectedPolygon=level.polygonSectors.Length-1;
                        level.polygonSectors[selectedPolygon]=new PolygonSectorData
                        {id="Sector_"+(selectedPolygon+1),kind="Platform",vertices=Array.Empty<Vector2>()};dirty=true;
                    }
                    if(level.polygonSectors.Length>0)
                        selectedPolygon=EditorGUILayout.Popup("Active",Mathf.Clamp(selectedPolygon,0,level.polygonSectors.Length-1),
                            level.polygonSectors.Select(s=>s.id).ToArray());
                    GUI.enabled=selectedPolygon>=0&&selectedPolygon<level.polygonSectors.Length;
                    if(GUILayout.Button("Delete",GUILayout.Width(70)))
                    {level.polygonSectors=level.polygonSectors.Where((p,i)=>i!=selectedPolygon).ToArray();
                    selectedPolygon=level.polygonSectors.Length-1;dirty=true;}
                    GUI.enabled=true;
                }
                if(selectedPolygon>=0&&selectedPolygon<level.polygonSectors.Length)
                {
                    var p=level.polygonSectors[selectedPolygon];
                    EditorGUI.BeginChangeCheck();
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        p.id=EditorGUILayout.TextField("Name",p.id);
                        int kind=Array.IndexOf(new[]{"Platform","Pit","Solid"},p.kind);
                        p.kind=new[]{"Platform","Pit","Solid"}[EditorGUILayout.Popup(Mathf.Max(0,kind),new[]{"Platform","Pit","Solid"})];
                        p.floor=EditorGUILayout.FloatField("Floor",p.floor);
                        p.ceiling=EditorGUILayout.FloatField("Ceiling",p.ceiling);
                    }
                    if(EditorGUI.EndChangeCheck())dirty=true;
                    GUILayout.Label("Vertices: "+(p.vertices?.Length??0)+"  |  " +
                        (SectorPolygon.IsSimple(p.vertices)?"valid simple polygon":"Add at least 3 non-crossing points"),EditorStyles.miniLabel);
                }
            }
            else if(tool==EditTool.Wall)
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Click two points to create a wall. Right-click cancels.",EditorStyles.miniLabel);
                    if(level.wallSegments.Length>0)selectedWall=EditorGUILayout.Popup(
                        Mathf.Clamp(selectedWall,0,level.wallSegments.Length-1),
                        level.wallSegments.Select(s=>s.id).ToArray(),GUILayout.Width(170));
                    GUI.enabled=selectedWall>=0&&selectedWall<level.wallSegments.Length;
                    if(GUILayout.Button("Delete wall",GUILayout.Width(88)))
                    {level.wallSegments=level.wallSegments.Where((s,i)=>i!=selectedWall).ToArray();
                    selectedWall=level.wallSegments.Length-1;dirty=true;}
                    GUI.enabled=true;
                }
                if(selectedWall>=0&&selectedWall<level.wallSegments.Length)
                {
                    var w=level.wallSegments[selectedWall];
                    EditorGUI.BeginChangeCheck();
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        w.id=EditorGUILayout.TextField("Name",w.id);
                        w.secret=EditorGUILayout.Toggle("Secret",w.secret);
                        w.blocksMovement=EditorGUILayout.Toggle("Block AI",w.blocksMovement);
                        w.bottom=EditorGUILayout.FloatField("Base",w.bottom);
                        w.top=EditorGUILayout.FloatField("Top",w.top);
                        w.thickness=EditorGUILayout.FloatField("Thickness",w.thickness);
                    }
                    if(EditorGUI.EndChangeCheck())dirty=true;
                }
            }
            else
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Click to place a new staircase; edit steps, rise and direction below.",EditorStyles.miniLabel);
                    if(level.stairs.Length>0)selectedStair=EditorGUILayout.Popup(
                        Mathf.Clamp(selectedStair,0,level.stairs.Length-1),
                        level.stairs.Select(s=>s.id).ToArray(),GUILayout.Width(150));
                    GUI.enabled=selectedStair>=0&&selectedStair<level.stairs.Length;
                    if(GUILayout.Button("Delete",GUILayout.Width(70)))
                    {level.stairs=level.stairs.Where((s,i)=>i!=selectedStair).ToArray();
                    selectedStair=level.stairs.Length-1;dirty=true;}
                    GUI.enabled=true;
                }
                if(selectedStair>=0&&selectedStair<level.stairs.Length)
                {
                    var s=level.stairs[selectedStair];EditorGUI.BeginChangeCheck();
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        s.id=EditorGUILayout.TextField(s.id);
                        s.steps=EditorGUILayout.IntSlider("Steps",s.steps,1,16);
                        s.rise=EditorGUILayout.FloatField("Rise",s.rise);
                        s.width=EditorGUILayout.FloatField("Width",s.width);
                        s.depth=EditorGUILayout.FloatField("Run",s.depth);
                        s.axisX=EditorGUILayout.FloatField("Direction X",s.axisX);
                        s.axisZ=EditorGUILayout.FloatField("Z",s.axisZ);
                    }
                    if(EditorGUI.EndChangeCheck())dirty=true;
                }
            }
        }
        private Vector2 ToGUI(Vector2 point,Rect map,float unit) =>
            new Vector2(map.x+(point.x+.5f)*unit,map.y+(level.height-point.y-.5f)*unit);
        private Vector2 ToWorld(Vector2 point,Rect map,float unit) =>
            new Vector2(Mathf.Clamp((point.x-map.x)/unit-.5f,0,level.width-1),
                Mathf.Clamp(level.height-(point.y-map.y)/unit-.5f,0,level.height-1));
        private void DrawMap(Rect map,float unit)
        {
            for(int z=0;z<level.height;z++)for(int x=0;x<level.width;x++)
            {
                char tile=level.rows[level.height-z-1][x];
                Color c=tile=='#'?new Color(.19f,.23f,.26f):tile=='s'?new Color(.42f,.27f,.43f):
                    tile=='B'||tile=='A'||tile=='C'||tile=='Z'?new Color(.53f,.31f,.15f):
                    tile=='P'?new Color(.15f,.62f,.31f):new Color(.28f,.32f,.36f);
                EditorGUI.DrawRect(new Rect(map.x+x*unit+.5f,map.y+(level.height-z-1)*unit+.5f,
                    Mathf.Max(1,unit-.9f),Mathf.Max(1,unit-.9f)),c);
            }
            Handles.BeginGUI();
            foreach(var p in level.polygonSectors)
            {
                if(p.vertices==null||p.vertices.Length==0)continue;
                Handles.color=p.kind=="Pit"?new Color(1f,.25f,.26f):new Color(.17f,.85f,1f);
                if(p.vertices.Length>=2)
                {
                    for(int i=0;i<p.vertices.Length;i++)
                        Handles.DrawAAPolyLine(2.4f,ToGUI(p.vertices[i],map,unit),
                            ToGUI(p.vertices[(i+1)%p.vertices.Length],map,unit));
                }
            }
            foreach(var w in level.wallSegments)
            {
                Handles.color=w.secret?Color.magenta:Color.yellow;
                Handles.DrawAAPolyLine(3,ToGUI(new Vector2(w.x1,w.z1),map,unit),
                    ToGUI(new Vector2(w.x2,w.z2),map,unit));
            }
            if(selectedPolygon>=0&&selectedPolygon<level.polygonSectors.Length&&tool==EditTool.Polygon)
            {
                Handles.color=Color.white;
                var vertices=level.polygonSectors[selectedPolygon].vertices;
                if(vertices!=null)foreach(var v in vertices)
                    Handles.DrawSolidDisc(ToGUI(v,map,unit),Vector3.forward,4.2f);
            }
            if(awaitingWall){Handles.color=Color.yellow;Handles.DrawWireDisc(ToGUI(firstWallPoint,map,unit),Vector3.forward,6);}
            Handles.EndGUI();
        }
        private void HandleCanvas(Rect map,float unit)
        {
            Event ev=Event.current;
            if(!map.Contains(ev.mousePosition))
            {if(ev.type==EventType.MouseUp)dragging=-1;return;}
            Vector2 pt=ToWorld(ev.mousePosition,map,unit);
            if(tool==EditTool.Polygon && selectedPolygon>=0 && selectedPolygon<level.polygonSectors.Length)
            {
                var p=level.polygonSectors[selectedPolygon];
                if(p.vertices==null)p.vertices=Array.Empty<Vector2>();
                int near=-1;float best=10f;
                for(int i=0;i<p.vertices.Length;i++)
                {
                    float d=Vector2.Distance(ev.mousePosition,ToGUI(p.vertices[i],map,unit));
                    if(d<best){best=d;near=i;}
                }
                if(ev.type==EventType.MouseDown&&ev.button==0)
                {
                    if(near>=0)dragging=near;
                    else {int n=p.vertices.Length;Array.Resize(ref p.vertices,n+1);p.vertices[n]=pt;dirty=true;}
                    ev.Use();Repaint();
                }
                else if(ev.type==EventType.MouseDown&&ev.button==1&&near>=0)
                {p.vertices=p.vertices.Where((v,i)=>i!=near).ToArray();dirty=true;ev.Use();Repaint();}
                else if(ev.type==EventType.MouseDrag&&dragging>=0)
                {p.vertices[dragging]=pt;dirty=true;ev.Use();Repaint();}
                else if(ev.type==EventType.MouseUp)dragging=-1;
            }
            else if(tool==EditTool.Wall&&ev.type==EventType.MouseDown)
            {
                if(ev.button==1){awaitingWall=false;ev.Use();Repaint();return;}
                if(ev.button!=0)return;
                if(!awaitingWall){firstWallPoint=pt;awaitingWall=true;}
                else
                {
                    Array.Resize(ref level.wallSegments,level.wallSegments.Length+1);
                    selectedWall=level.wallSegments.Length-1;
                    level.wallSegments[selectedWall]=new WallSegmentData
                    {id="Wall_"+(selectedWall+1),x1=firstWallPoint.x,z1=firstWallPoint.y,
                     x2=pt.x,z2=pt.y,top=4f,thickness=.25f,blocksMovement=true};
                    awaitingWall=false;dirty=true;
                }
                ev.Use();Repaint();
            }
            else if(tool==EditTool.Stair&&ev.type==EventType.MouseDown&&ev.button==0)
            {
                Array.Resize(ref level.stairs,level.stairs.Length+1);
                selectedStair=level.stairs.Length-1;
                level.stairs[selectedStair]=new StaircaseData
                {id="Stair_"+(selectedStair+1),x=pt.x,z=pt.y,steps=4,rise=.2f,depth=.6f,width=.9f,axisZ=1f};
                dirty=true;ev.Use();Repaint();
            }
        }
    }
}
#endif
