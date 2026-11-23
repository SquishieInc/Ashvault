using UnityEngine;

namespace Ashvault
{
    // Generic fantasy archetypes only; no third-party setting, artwork or trademarks.
    public enum HeroClass { Knight, Ranger, Arcanist, Shadowblade, Cleric, Berserker, Druid, Warlock }
    public enum SigilKey { None, Bronze, Azure, Crimson }
    public enum GamePhase { ClassSelect, Playing, Paused, Defeat, Victory, CampaignComplete }

    public sealed class WeaponSpec
    {
        public string Name;
        public string SpriteName;
        public string ProjectileSprite;
        public int Damage;
        public float Cooldown, Range, ProjectileSpeed, BlastRadius;
        public int Ammo; // -1 = no ammo consumption
        public bool Melee;
        public Color Tint;

        public WeaponSpec(string name, string image, string projectile, int damage, float cooldown,
            float range, float speed, int ammo, bool melee, Color tint, float blastRadius = 0f)
        {
            Name = name; SpriteName = image; ProjectileSprite = projectile;
            Damage = damage; Cooldown = cooldown; Range = range;
            ProjectileSpeed = speed; Ammo = ammo; Melee = melee;
            Tint = tint; BlastRadius = blastRadius;
        }
    }

    public static class GameCatalog
    {
        public const int ClassCount = 8;
        public static string Title(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return "IRON KNIGHT";
                case HeroClass.Ranger: return "WILDRANGER";
                case HeroClass.Arcanist: return "ARCANIST";
                case HeroClass.Shadowblade: return "SHADOWBLADE";
                case HeroClass.Cleric: return "DAWN CLERIC";
                case HeroClass.Berserker: return "BERSERKER";
                case HeroClass.Druid: return "THORN DRUID";
                default: return "VOID WARLOCK";
            }
        }
        public static string Description(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return "Armoured sword-and-hammer brawler. 15% damage resistance.";
                case HeroClass.Ranger: return "Mobile crossbow specialist. Fast long-range bursts.";
                case HeroClass.Arcanist: return "Unending wand bolts; powerful explosive fire magic.";
                case HeroClass.Shadowblade: return "Swift dual blades and thrown toxins; small heal on kill.";
                case HeroClass.Cleric: return "Durable radiant mace and holy orbs; strong heal on kill.";
                case HeroClass.Berserker: return "Massive axe melee; stronger hits when critically wounded.";
                case HeroClass.Druid: return "Thorn projectiles and spreading nature magic.";
                default: return "Cursed spell bolts; lifesteal on enemy eliminations.";
            }
        }
        public static int MaxHealth(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return 150;
                case HeroClass.Ranger: return 105;
                case HeroClass.Arcanist: return 85;
                case HeroClass.Shadowblade: return 90;
                case HeroClass.Cleric: return 125;
                case HeroClass.Berserker: return 170;
                case HeroClass.Druid: return 110;
                default: return 95;
            }
        }
        public static float Speed(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return 7.4f;
                case HeroClass.Ranger: return 9.0f;
                case HeroClass.Arcanist: return 8.0f;
                case HeroClass.Shadowblade: return 10.3f;
                case HeroClass.Cleric: return 7.8f;
                case HeroClass.Berserker: return 8.3f;
                case HeroClass.Druid: return 8.8f;
                default: return 8.1f;
            }
        }
        public static float DamageMultiplier(HeroClass kind, int health, int maxHealth)
        {
            return kind == HeroClass.Berserker && health * 2 < maxHealth ? 1.30f : 1f;
        }
        public static float IncomingMultiplier(HeroClass kind) => kind == HeroClass.Knight ? .85f : 1f;
        public static int HealOnKill(HeroClass kind) => kind == HeroClass.Cleric ? 9 :
            kind == HeroClass.Warlock ? 6 : kind == HeroClass.Shadowblade ? 3 : 0;

        public static WeaponSpec StartingWeapon(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return new WeaponSpec("OATHBREAKER SWORD", "weapon_sword", "projectile_arrow",
                    42, .44f, 3.2f, 0f, -1, true, new Color(1f, .75f, .4f));
                case HeroClass.Ranger: return new WeaponSpec("HUNTER CROSSBOW", "weapon_crossbow", "projectile_arrow",
                    29, .36f, 30f, 24f, 85, false, new Color(.8f, 1f, .6f));
                case HeroClass.Arcanist: return new WeaponSpec("RUNEWAND", "weapon_staff", "projectile_orb",
                    23, .32f, 30f, 15f, -1, false, new Color(.45f, .7f, 1f));
                case HeroClass.Shadowblade: return new WeaponSpec("DUAL FANGS", "weapon_daggers", "projectile_arrow",
                    19, .19f, 2.4f, 0f, -1, true, new Color(.9f, .7f, 1f));
                case HeroClass.Cleric: return new WeaponSpec("DAWNMACE", "weapon_mace", "projectile_orb",
                    37, .48f, 3.2f, 0f, -1, true, new Color(1f, .93f, .5f));
                case HeroClass.Berserker: return new WeaponSpec("RAGE CLEAVER", "weapon_axe", "projectile_arrow",
                    54, .63f, 3.4f, 0f, -1, true, new Color(1f, .48f, .36f));
                case HeroClass.Druid: return new WeaponSpec("THORN STAFF", "weapon_sickle", "projectile_thorn",
                    22, .31f, 27f, 17f, -1, false, new Color(.44f, 1f, .55f));
                default: return new WeaponSpec("HEX GRIMOIRE", "weapon_grimoire", "projectile_orb",
                    26, .37f, 29f, 17f, -1, false, new Color(.7f, .46f, 1f));
            }
        }
        public static WeaponSpec UpgradeWeapon(HeroClass kind)
        {
            switch (kind)
            {
                case HeroClass.Knight: return new WeaponSpec("WARHAMMER", "weapon_hammer", "projectile_arrow",
                    86, .80f, 3.1f, 0f, -1, true, new Color(1f, .55f, .25f));
                case HeroClass.Ranger: return new WeaponSpec("STORM REPEATER", "weapon_crossbow", "projectile_arrow",
                    18, .12f, 34f, 31f, 210, false, new Color(1f, .9f, .55f));
                case HeroClass.Arcanist: return new WeaponSpec("EMBER SCEPTRE", "weapon_staff", "projectile_orb",
                    46, .60f, 30f, 12f, 65, false, new Color(1f, .4f, .2f), 3f);
                case HeroClass.Shadowblade: return new WeaponSpec("VENOM THROWERS", "weapon_daggers", "projectile_arrow",
                    27, .18f, 25f, 29f, 135, false, new Color(.55f, 1f, .4f));
                case HeroClass.Cleric: return new WeaponSpec("JUDGEMENT ORB", "weapon_mace", "projectile_orb",
                    45, .62f, 28f, 18f, 80, false, new Color(1f, .95f, .55f), 2.5f);
                case HeroClass.Berserker: return new WeaponSpec("SIEGE AXE", "weapon_axe", "projectile_arrow",
                    103, .9f, 3.7f, 0f, -1, true, new Color(1f, .36f, .3f));
                case HeroClass.Druid: return new WeaponSpec("WILD BLOOM", "weapon_sickle", "projectile_thorn",
                    36, .45f, 28f, 17f, 80, false, new Color(.6f, 1f, .25f), 2.7f);
                default: return new WeaponSpec("SOUL NOVA", "weapon_grimoire", "projectile_orb",
                    52, .65f, 30f, 18f, 85, false, new Color(.83f, .33f, 1f), 2.9f);
            }
        }
    }
}
