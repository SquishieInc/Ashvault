using UnityEngine;

namespace Ashvault
{
    public enum EnemyClassification { Grunt, Skirmisher, Ranged, Brute, Flying, Summoner, Boss }
    public enum EnemyAttack { Claw, Bolt, Charge, Volley, Summon, BossPattern }
    public enum EnemyArchetype
    {
        SkeletonReaver, BoneRunner, HexCultist, IronWarden,
        GraveHound, FenWisp, PlagueShaman, MossGolem,
        VoidStalker, AstralAdept, StarSentinel, RiftSummoner,
        EmberLord, FenMatriarch, AstralLich
    }

    public sealed class EnemySpec
    {
        public readonly string Name, Sprite;
        public readonly EnemyClassification Classification;
        public readonly EnemyAttack Attack;
        public readonly int MaxHealth, Damage;
        public readonly float MoveSpeed, SightRange, AttackRange, Cooldown, ProjectileSpeed, VisualHeight;
        public readonly Color Tint;
        public readonly EnemyArchetype Minion;
        public bool IsBoss => Classification == EnemyClassification.Boss;

        public EnemySpec(string name, string sprite, EnemyClassification type, EnemyAttack attack,
            int hp, float speed, int damage, float sight, float range, float cooldown,
            float projectileSpeed, float height, Color tint, EnemyArchetype minion = EnemyArchetype.SkeletonReaver)
        {
            Name = name; Sprite = sprite; Classification = type; Attack = attack;
            MaxHealth = hp; MoveSpeed = speed; Damage = damage; SightRange = sight;
            AttackRange = range; Cooldown = cooldown; ProjectileSpeed = projectileSpeed;
            VisualHeight = height; Tint = tint; Minion = minion;
        }
    }

    public static class EnemyCatalog
    {
        public static EnemySpec Get(EnemyArchetype kind)
        {
            Color white = Color.white;
            switch (kind)
            {
                case EnemyArchetype.SkeletonReaver: return new EnemySpec("Crypt Reaver", "enemy_skeleton",
                    EnemyClassification.Grunt, EnemyAttack.Claw, 85, 3.25f, 13, 22, 2.4f, 1.25f, 0, 2.5f, white);
                case EnemyArchetype.BoneRunner: return new EnemySpec("Bone Runner", "enemy_bonerunner",
                    EnemyClassification.Skirmisher, EnemyAttack.Claw, 57, 5.3f, 9, 27, 2.3f, .82f, 0, 2.2f, white);
                case EnemyArchetype.HexCultist: return new EnemySpec("Hex Cultist", "enemy_cultist",
                    EnemyClassification.Ranged, EnemyAttack.Bolt, 70, 2.6f, 12, 28, 14f, 1.72f, 13f, 2.4f, white);
                case EnemyArchetype.IronWarden: return new EnemySpec("Iron Warden", "enemy_guardian",
                    EnemyClassification.Brute, EnemyAttack.Charge, 220, 2.1f, 26, 25, 2.8f, 3.1f, 0, 3.3f, white);
                case EnemyArchetype.GraveHound: return new EnemySpec("Grave Hound", "enemy_hound",
                    EnemyClassification.Skirmisher, EnemyAttack.Claw, 76, 5.6f, 12, 28, 2.4f, .95f, 0, 1.85f, white);
                case EnemyArchetype.FenWisp: return new EnemySpec("Fen Wisp", "enemy_wisp",
                    EnemyClassification.Flying, EnemyAttack.Volley, 60, 3.9f, 9, 29, 13f, 2.2f, 14f, 1.8f, white);
                case EnemyArchetype.PlagueShaman: return new EnemySpec("Plague Shaman", "enemy_shaman",
                    EnemyClassification.Summoner, EnemyAttack.Summon, 110, 2.0f, 10, 28, 13f, 2.6f, 10.8f, 2.6f, white,
                    EnemyArchetype.GraveHound);
                case EnemyArchetype.MossGolem: return new EnemySpec("Moss Golem", "enemy_golem",
                    EnemyClassification.Brute, EnemyAttack.Charge, 270, 1.8f, 32, 24, 2.85f, 3.3f, 0, 3.65f, white);
                case EnemyArchetype.VoidStalker: return new EnemySpec("Void Stalker", "enemy_stalker",
                    EnemyClassification.Skirmisher, EnemyAttack.Claw, 105, 5.0f, 18, 30, 2.5f, 1.05f, 0, 2.5f, white);
                case EnemyArchetype.AstralAdept: return new EnemySpec("Astral Adept", "enemy_adept",
                    EnemyClassification.Ranged, EnemyAttack.Volley, 96, 2.9f, 12, 32, 15f, 2.2f, 16f, 2.6f, white);
                case EnemyArchetype.StarSentinel: return new EnemySpec("Star Sentinel", "enemy_sentinel",
                    EnemyClassification.Brute, EnemyAttack.Charge, 320, 2.0f, 34, 30, 3.0f, 3.5f, 0, 3.65f, white);
                case EnemyArchetype.RiftSummoner: return new EnemySpec("Rift Summoner", "enemy_summoner",
                    EnemyClassification.Summoner, EnemyAttack.Summon, 145, 2.4f, 17, 34, 15f, 2.3f, 15f, 2.75f, white,
                    EnemyArchetype.VoidStalker);
                case EnemyArchetype.EmberLord: return new EnemySpec("THE EMBER KING", "boss_ember",
                    EnemyClassification.Boss, EnemyAttack.BossPattern, 850, 2.55f, 25, 42, 21f, 2.15f, 16f, 4.65f,
                    new Color(1f, .77f, .66f), EnemyArchetype.BoneRunner);
                case EnemyArchetype.FenMatriarch: return new EnemySpec("MOTHER OF THE MIRE", "boss_fen",
                    EnemyClassification.Boss, EnemyAttack.BossPattern, 1050, 2.25f, 27, 42, 20f, 2.3f, 14f, 4.9f,
                    new Color(.67f, 1f, .69f), EnemyArchetype.FenWisp);
                default: return new EnemySpec("THE STARLESS LICH", "boss_astral",
                    EnemyClassification.Boss, EnemyAttack.BossPattern, 1250, 3.0f, 31, 45, 23f, 1.85f, 19f, 4.9f,
                    new Color(.82f, .75f, 1f), EnemyArchetype.VoidStalker);
            }
        }
    }
}
