#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Ashvault.EditorTools
{
    public static class AshvaultV4ValidationMenu
    {
        [MenuItem("Tools/Ashvault/Validate V4 Art, Sectors and Audio")]
        public static void Validate()
        {
            string[] sounds={"enemy_hurt","enemy_death","boss_death","reaver_attack",
                "bolt_cast","weapon_impact","door_open","door_locked","switch_press",
                "secret_reveal","crusher_impact","rune_trap","ambient_cinder",
                "ambient_fen","ambient_astral"};
            foreach(string name in sounds)
                if(Resources.Load<AudioClip>("Audio/"+name)==null)
                    throw new Exception("Missing Ashvault sound: "+name);
            var atlas=Resources.Load<Texture2D>("Textures/Directional/enemy_skeleton_directions");
            if(atlas==null||atlas.width!=768||atlas.height!=1584)
                throw new Exception("Skeleton Reaver needs 8 directions x 11 pose rows at 96x144");
            foreach(var kind in new[]{"brazier","mushrooms","crystal","star_lantern"})
            {
                var tex=Resources.Load<Texture2D>("Textures/Animated/"+kind+"_frames");
                if(tex==null||tex.width!=512||tex.height!=128)
                    throw new Exception("Invalid four-frame decorative strip: "+kind);
            }
            foreach(var kind in new[]{"blood","scorch","crack","rune_impact"})
                if(Resources.Load<Texture2D>("Textures/Decals/"+kind)==null)
                    throw new Exception("Missing decal: "+kind);
            int polygons=0,walls=0,stairs=0,switches=0,flows=0;
            for(int ep=1;ep<=3;ep++)for(int map=1;map<=3;map++)
            {
                var data=CampaignCatalog.Load(ep,map);
                if(data==null || data.polygonSectors==null || data.wallSegments==null ||
                    data.stairs==null || data.switches==null || data.animatedSurfaces==null)
                    throw new Exception("Missing v4 level sections");
                var grid=new DungeonMap(data);
                foreach(var polygon in data.polygonSectors)
                {
                    if(!SectorPolygon.IsSimple(polygon.vertices))
                        throw new Exception("Invalid polygon "+polygon.id+" on "+data.id);
                    polygons++;
                }
                foreach(var wall in data.wallSegments)
                {
                    if(wall.thickness<=0 || wall.top<=wall.bottom)
                        throw new Exception("Invalid wall "+wall.id+" on "+data.id);
                    walls++;
                }
                foreach(var s in data.switches)
                {
                    if(!grid.IsOpen(s.x,s.z) ||
                        grid.Cell(s.targetX,s.targetZ)!='B' &&
                        grid.Cell(s.targetX,s.targetZ)!='A' &&
                        grid.Cell(s.targetX,s.targetZ)!='C')
                        throw new Exception("Invalid switch on "+data.id);
                    switches++;
                }
                foreach(var f in data.animatedSurfaces)
                {if(!grid.IsOpen(f.x,f.z))throw new Exception("Blocked flowing patch on "+data.id);flows++;}
                stairs+=data.stairs.Length;
            }
            Debug.Log("ASHVAULT V4 DATA CHECKS PASSED — "+polygons+" polygon sectors, "+
                walls+" walls, "+stairs+" staircases, "+switches+" switches, "+flows+
                " floor animations and 15+ sound references. Still requires a Unity Play mode test.");
        }
    }
}
#endif
