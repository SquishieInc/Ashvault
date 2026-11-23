#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Ashvault.EditorTools
{
    public static class AshvaultEnvironmentValidationMenu
    {
        [MenuItem("Tools/Ashvault/Validate Visuals, Lighting and Machinery")]
        public static void Validate()
        {
            string[] art = {
                "enemy_skeleton", "enemy_bonerunner", "enemy_cultist", "enemy_guardian",
                "enemy_hound", "enemy_wisp", "enemy_shaman", "enemy_golem",
                "enemy_stalker", "enemy_adept", "enemy_sentinel", "enemy_summoner",
                "boss_ember", "boss_fen", "boss_astral"
            };
            string[] decorations = {
                "brazier", "banner", "sarcophagus", "cage", "mushrooms", "roots",
                "idol", "totem", "crystal", "armillary", "star_lantern", "monolith"
            };
            foreach (string enemy in art)
            {
                Texture2D atlas = Resources.Load<Texture2D>(
                    "Textures/Directional/" + enemy + "_directions");
                if (atlas == null || atlas.width != 768 ||
                    atlas.height != (enemy == "enemy_skeleton" ? 1584 : 864))
                    throw new Exception("Invalid eight-angle enemy atlas: " + enemy);
            }
            foreach (string kind in decorations)
                if (Resources.Load<Texture2D>("Textures/deco_" + kind) == null)
                    throw new Exception("Missing environment sprite: " + kind);
            if (Resources.Load<Texture2D>("Textures/fx_impact") == null)
                throw new Exception("Missing combat impact sprite");
            int props = 0, mechanisms = 0, lights = 0;
            for (int episode = 1; episode <= 3; episode++)
            for (int levelNumber = 1; levelNumber <= 3; levelNumber++)
            {
                var data = CampaignCatalog.Load(episode, levelNumber);
                if (data == null || data.decorations == null || data.decorations.Length < 12 ||
                    data.fixtures == null || data.fixtures.Length < 5 ||
                    data.mechanisms == null || data.mechanisms.Length != 4)
                    throw new Exception("Visual expansion incomplete in E" + episode + " L" + levelNumber);
                var map = new DungeonMap(data);
                foreach (var prop in data.decorations)
                {
                    if (!map.IsOpen(prop.x, prop.z)) throw new Exception("Prop in blocked cell: " + data.id);
                    props++;
                }
                foreach (var fixture in data.fixtures)
                {
                    if (!map.IsOpen(fixture.x, fixture.z)) throw new Exception("Light in blocked cell: " + data.id);
                    lights++;
                }
                foreach (var mechanism in data.mechanisms)
                {
                    if (!map.IsOpen(mechanism.x, mechanism.z)) throw new Exception("Machinery in blocked cell: " + data.id);
                    mechanisms++;
                }
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                Debug.LogWarning("Ashvault: URP Lit shader not found. Use a Universal 3D project for 3D lighting/shadows.");
            Debug.Log("ASHVAULT ENVIRONMENT QA PASSED: 15 enemy atlases, 12 decorative sprite sources, " +
                mechanisms + " mechanisms, " + lights + " lights, " + props +
                " props. Next: compile/playtest geometry, light shadows, mobile settings.");
        }
    }
}
#endif
