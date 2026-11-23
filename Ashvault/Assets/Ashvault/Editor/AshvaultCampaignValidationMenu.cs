#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ashvault.EditorTools
{
    public static class AshvaultCampaignValidationMenu
    {
        [MenuItem("Tools/Ashvault/Validate All Nine Campaign Maps")]
        public static void Validate()
        {
            int checkedRoutes = 0, secrets = 0, encounters = 0;
            for (int episode = 1; episode <= CampaignCatalog.Episodes; episode++)
            for (int floor = 1; floor <= CampaignCatalog.MapsPerEpisode; floor++)
            {
                LevelBlueprint level = CampaignCatalog.Load(episode, floor);
                if (level == null) throw new Exception("Missing episode map " + episode + "/" + floor);
                if (level.classDrops == null || level.classDrops.Length != GameCatalog.ClassCount)
                    throw new Exception("Missing class cache definitions: " + level.id);
                int bossCount = 0;
                foreach (var encounter in level.enemies)
                    if (encounter.mode == "Boss") bossCount++;
                if (bossCount != (floor == 3 ? 1 : 0))
                    throw new Exception("Unexpected boss count for " + level.id);
                encounters += level.enemies.Length;
                for (int id = 0; id < GameCatalog.ClassCount; id++)
                {
                    var map = new DungeonMap(level);
                    int numSecrets = 0;
                    for (int x = 0; x < map.Width; x++)
                    for (int z = 0; z < map.Height; z++)
                    {
                        char mark = map.Cell(x, z);
                        if (mark == 's') { map.Unseal(x, z); numSecrets++; }
                        if (mark == 'B' || mark == 'A' || mark == 'C' || mark == 'Z' ||
                            mark == ('1' + id))
                            map.Unseal(x, z);
                    }
                    if (numSecrets < 3) throw new Exception("Insufficient secrets: " + level.id);
                    var start = map.Locate('P');
                    var exit = map.Locate('X');
                    var waypoints = new List<Vector3>();
                    if (!GridPathfinder.Find(map, map.World(start.x, start.y),
                        map.World(exit.x, exit.y), waypoints))
                        throw new Exception("No NavGrid route for " + level.id + " / " + (HeroClass)id);
                    var bonus = level.classDrops[id];
                    if (!GridPathfinder.Find(map, map.World(start.x, start.y),
                        map.World(bonus.x, bonus.z), waypoints))
                        throw new Exception("Class cache unreachable: " + level.id + " / " + (HeroClass)id);
                    checkedRoutes += 2;
                    if (id == 0) secrets += numSecrets;
                }
                Debug.Log("Ashvault checked: " + level.id + " / " + level.title);
            }
            Debug.Log("ASHVAULT VALIDATION PASSED: " + checkedRoutes +
                " class navigation routes, " + secrets + " secrets, " +
                encounters + " authored encounter positions. Unity physics playtest still required.");
        }
    }
}
#endif
