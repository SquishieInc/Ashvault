using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    public sealed class Projectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed, remainingRange, blastRadius;
        private int damage;
        private bool hostile;
        private AshPlayer shooter;

        public static void Create(Vector3 position, Vector3 direction, WeaponSpec weapon, AshPlayer owner)
        {
            var go = VisualFactory.Billboard(weapon.ProjectileSprite, position,
                weapon.BlastRadius > 0f ? new Vector2(1.2f, 1.2f) : new Vector2(.65f, .65f),
                "PLAYER PROJECTILE / " + weapon.Name);
            go.transform.SetParent(owner.Owner.WorldRoot.transform);
            VisualFactory.SetSpriteTint(go.GetComponent<SpriteRenderer>(), weapon.Tint);
            var projectile = go.AddComponent<Projectile>();
            projectile.direction = direction.normalized;
            projectile.speed = weapon.ProjectileSpeed;
            projectile.remainingRange = weapon.Range;
            projectile.damage = owner.AttackDamage(weapon.Damage);
            projectile.blastRadius = weapon.BlastRadius;
            projectile.shooter = owner;
        }

        public static void CreateEnemy(Vector3 position, Vector3 direction, int damage,
            float projectileSpeed, Color tint)
        {
            var go = VisualFactory.Billboard("projectile_orb", position,
                new Vector2(.70f, .70f), "ENEMY / SPELL BOLT");
            VisualFactory.SetSpriteTint(go.GetComponent<SpriteRenderer>(), tint);
            if (AshGame.Instance != null && AshGame.Instance.WorldRoot != null)
                go.transform.SetParent(AshGame.Instance.WorldRoot.transform);
            var projectile = go.AddComponent<Projectile>();
            projectile.direction = direction.normalized;
            projectile.speed = projectileSpeed;
            projectile.remainingRange = 45f;
            projectile.damage = damage;
            projectile.hostile = true;
        }

        private void Update()
        {
            if (AshGame.Instance == null || AshGame.Instance.Phase != GamePhase.Playing) return;
            float travel = Mathf.Min(speed * Time.deltaTime, remainingRange);
            if (travel <= 0f) { Destroy(gameObject); return; }
            if (Physics.SphereCast(transform.position, .15f, direction, out RaycastHit hit,
                travel, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hostile)
                {
                    var victim = hit.collider.GetComponentInParent<AshPlayer>();
                    if (victim != null) victim.TakeDamage(damage);
                }
                else
                {
                    if (shooter != null) shooter.Owner.AlertEnemies(hit.point, 11f);
                    var struck = hit.collider.GetComponentInParent<EnemyAI>();
                    if (struck != null) struck.TakeDamage(damage, shooter);
                    if (blastRadius > 0f)
                    {
                        var visited = new HashSet<EnemyAI>();
                        if (struck != null) visited.Add(struck);
                        foreach (var collider in Physics.OverlapSphere(hit.point, blastRadius,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            var other = collider.GetComponentInParent<EnemyAI>();
                            if (other == null || !visited.Add(other)) continue;
                            other.TakeDamage(Mathf.RoundToInt(damage * .65f), shooter);
                        }
                    }
                }
                AshAudio.OneShot("weapon_impact", hit.point, .43f);
                if (hit.collider.GetComponentInParent<EnemyAI>() == null &&
                    hit.collider.GetComponentInParent<AshPlayer>() == null)
                    ImpactDecal.Place(hostile ? "rune_impact" : "scorch",hit.point,hit.normal,.45f);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * travel;
            remainingRange -= travel;
        }
    }
}
