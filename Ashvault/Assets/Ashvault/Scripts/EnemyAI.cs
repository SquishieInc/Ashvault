using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    public enum EnemyAwareness { Dormant, Patrolling, Investigating, Hunting, Windup, Charging, Dead }

    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyAI : MonoBehaviour
    {
        public EnemySpec Spec { get; private set; }
        public EnemyAwareness Awareness { get; private set; }
        public int Health { get; private set; }
        public int Phase { get; private set; } = 1;
        public bool IsBoss => Spec != null && Spec.IsBoss;
        public bool IsVisuallyMoving { get; private set; }
        public float HealthFraction => Spec == null ? 0f : Mathf.Clamp01((float)Health / Spec.MaxHealth);

        private AshGame game;
        private CharacterController motor;
        private SpriteRenderer sprite;
        private DirectionalSpriteAnimator directional;
        private MaterialPropertyBlock tintBlock;
        private Vector2Int home;
        private Vector3 lastKnown, patrolTarget, chargeHeading;
        private bool patrol, heard, chargeConnected;
        private float alertUntil, nextAttack, nextPath, nextPatrol, fallSpeed, windupEnds, chargeEnds, flashUntil;
        private float queuedImpactTime;
        private Vector3 queuedTarget;
        private float queuedDistance;
        private bool queuedAttack;
        private float strafeSign = 1f;
        private int nextPathIndex, attackCounter, minionSummons;
        private readonly List<Vector3> route = new List<Vector3>();

        public void Initialize(AshGame owner, EnemyArchetype kind, Vector2Int spawn, bool isPatrol, SpriteRenderer art)
        {
            game = owner; home = spawn; patrol = isPatrol; sprite = art;
            directional = art == null ? null : art.GetComponent<DirectionalSpriteAnimator>();
            tintBlock = new MaterialPropertyBlock();
            Spec = EnemyCatalog.Get(kind);
            Health = Spec.MaxHealth;
            Awareness = patrol ? EnemyAwareness.Patrolling : EnemyAwareness.Dormant;
            motor = GetComponent<CharacterController>();
            motor.height = Mathf.Clamp(Spec.VisualHeight * .76f, 1.55f, 4.1f);
            motor.center = Vector3.up * (motor.height * 0.5f);
            motor.radius = Spec.IsBoss ? .77f : .4f;
            motor.stepOffset = .42f;
            motor.skinWidth = .06f;
            lastKnown = game.Map.World(home.x, home.y);
            patrolTarget = lastKnown;
            nextPatrol = Time.time + 1.8f;
            nextAttack = Time.time + (Spec.IsBoss ? 2.0f : .65f);
            nextPath = Time.time + ((spawn.x + spawn.y) % 7) * .07f;
            if (sprite != null) ApplyTint(Spec.Tint);
            game.TrackEnemy(this);
        }

        private void OnDestroy()
        {
            if (game != null) game.UntrackEnemy(this);
        }

        public void Hear(Vector3 origin, float radius)
        {
            if (Awareness == EnemyAwareness.Dead || Vector3.Distance(transform.position, origin) > radius) return;
            Alert(origin);
        }

        private void Alert(Vector3 origin)
        {
            lastKnown = origin;
            alertUntil = Time.time + (Spec != null && Spec.IsBoss ? 25f : 9f);
            heard = true;
            if (Awareness != EnemyAwareness.Windup && Awareness != EnemyAwareness.Charging)
                Awareness = EnemyAwareness.Investigating;
        }

        private bool HasSight(out Vector3 target, out float distance)
        {
            target = game.Player.transform.position + Vector3.up * 1.0f;
            Vector3 eye = transform.position + Vector3.up * 1.15f;
            Vector3 delta = target - eye;
            distance = delta.magnitude;
            if (distance > Spec.SightRange || distance < 0.01f) return false;
            if (!heard && !Spec.IsBoss && distance > 7f &&
                Vector3.Angle(transform.forward, new Vector3(delta.x, 0f, delta.z)) > 125f) return false;
            return Physics.Raycast(eye, delta.normalized, out RaycastHit hit, distance + .55f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<AshPlayer>() != null;
        }

        private void Update()
        {
            if (game == null || game.Phase != GamePhase.Playing || game.Player == null || Health <= 0) return;
            float now = Time.time;
            if (queuedAttack && now >= queuedImpactTime)
            {
                queuedAttack = false;
                ApplyAttack(queuedTarget,queuedDistance);
            }
            if (sprite != null && now >= flashUntil && sprite.color != Spec.Tint)
                ApplyTint(Spec.Tint);
            bool visible = HasSight(out Vector3 aim, out float distance);
            if (visible)
            {
                heard = true;
                lastKnown = game.Player.transform.position;
                alertUntil = now + (Spec.IsBoss ? 25f : 9f);
                if (Awareness != EnemyAwareness.Windup && Awareness != EnemyAwareness.Charging)
                    Awareness = EnemyAwareness.Hunting;
            }
            else if (now > alertUntil && Awareness != EnemyAwareness.Charging && Awareness != EnemyAwareness.Windup)
            {
                heard = false;
                Awareness = patrol ? EnemyAwareness.Patrolling : EnemyAwareness.Dormant;
            }
            if (Awareness == EnemyAwareness.Windup)
            {
                GravityMove(Vector3.zero);
                if (now >= windupEnds)
                {
                    Awareness = EnemyAwareness.Charging;
                    chargeEnds = now + .56f;
                    chargeConnected = false;
                }
                return;
            }
            if (Awareness == EnemyAwareness.Charging)
            {
                GravityMove(chargeHeading * (Spec.MoveSpeed * 3.1f));
                if (!chargeConnected && Vector3.Distance(game.Player.transform.position, transform.position) < 2.5f)
                {
                    game.Player.TakeDamage(Spec.Damage);
                    chargeConnected = true;
                }
                if (now >= chargeEnds) Awareness = EnemyAwareness.Hunting;
                return;
            }
            if (visible && distance <= Spec.AttackRange && now >= nextAttack)
            {
                Face(aim - transform.position);
                ExecuteAttack(aim, distance);
                nextAttack = now + Spec.Cooldown * (Phase == 3 ? .65f : Phase == 2 ? .82f : 1f);
            }
            Vector3 destination;
            bool hasGoal = now <= alertUntil;
            if (hasGoal) destination = lastKnown;
            else
            {
                if (!patrol) { GravityMove(Vector3.zero); Hover(); return; }
                if (now > nextPatrol || Vector3.Distance(transform.position, patrolTarget) < 1.2f)
                    SelectPatrol();
                destination = patrolTarget;
            }
            if (visible && (Spec.Classification == EnemyClassification.Ranged ||
                Spec.Classification == EnemyClassification.Flying ||
                Spec.Classification == EnemyClassification.Summoner || IsBoss))
            {
                if (distance < 5.2f)
                {
                    Vector3 away = transform.position - game.Player.transform.position;
                    away.y = 0f;
                    GravityMove(away.normalized * Spec.MoveSpeed * .60f);
                }
                else if (distance <= Mathf.Min(Spec.AttackRange * .84f, 13f))
                {
                    GravityMove(transform.right * (Spec.MoveSpeed * .30f * strafeSign));
                }
                else Navigate(destination);
            }
            else if (visible && distance <= 1.85f) GravityMove(Vector3.zero);
            else Navigate(destination);
            Hover();
        }

        private void Hover()
        {
            if (sprite != null && Spec.Classification == EnemyClassification.Flying)
                sprite.transform.localPosition = Vector3.up *
                    (Spec.VisualHeight * .5f + .22f + Mathf.Sin(Time.time * 3f + home.x) * .19f);
        }

        private void SelectPatrol()
        {
            nextPatrol = Time.time + 3.4f;
            var offsets = new[] { new Vector2Int(2, 0), new Vector2Int(0, 2),
                new Vector2Int(-2, 0), new Vector2Int(0, -2), Vector2Int.zero };
            var target = home + offsets[(nextPathIndex++) % offsets.Length];
            if (game.Map.IsOpen(target.x, target.y)) patrolTarget = game.Map.World(target.x, target.y);
            else patrolTarget = game.Map.World(home.x, home.y);
            nextPath = 0f;
        }

        private void Navigate(Vector3 target)
        {
            if (Time.time >= nextPath || route.Count == 0)
            {
                nextPath = Time.time + .62f + (home.x % 4) * .085f;
                GridPathfinder.Find(game.Map, transform.position, target, route);
            }
            if (route.Count == 0) { GravityMove(Vector3.zero); return; }
            Vector3 waypoint = route[0];
            Vector3 direction = waypoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < .55f * .55f)
            {
                route.RemoveAt(0);
                if (route.Count == 0) { GravityMove(Vector3.zero); return; }
                direction = route[0] - transform.position;
                direction.y = 0f;
            }
            Face(direction);
            GravityMove(direction.normalized * Spec.MoveSpeed);
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > .05f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction.normalized), 540f * Time.deltaTime);
        }

        public void MoveWithPlatform(Vector3 delta)
        {
            if (motor != null && motor.enabled) motor.Move(delta);
        }

        private void GravityMove(Vector3 planar)
        {
            IsVisuallyMoving = planar.sqrMagnitude > .45f;
            if (motor.isGrounded && fallSpeed < 0f) fallSpeed = -1.4f;
            fallSpeed += Physics.gravity.y * Time.deltaTime;
            motor.Move((planar + Vector3.up * fallSpeed) * Time.deltaTime);
        }

        private void ExecuteAttack(Vector3 target, float distance)
        {
            if (directional != null) directional.PlayAttack();
            CombatFeedback.Spawn(transform.position + Vector3.up * (Spec.VisualHeight * .65f),
                Spec.Tint, IsBoss);
            queuedTarget=target;queuedDistance=distance;
            queuedImpactTime=Time.time + (IsBoss ? .34f : .20f);
            queuedAttack=true;
            AshAudio.OneShot(Spec.Attack == EnemyAttack.Claw || Spec.Attack == EnemyAttack.Charge ?
                "reaver_attack" : "bolt_cast",transform.position,IsBoss ? .85f : .55f);
        }

        private void ApplyAttack(Vector3 target,float distance)
        {
            if (Health <= 0 || game == null || game.Phase != GamePhase.Playing || game.Player == null)return;
            attackCounter++;
            switch (Spec.Attack)
            {
                case EnemyAttack.Claw:
                    if (distance <= Spec.AttackRange + .2f && HasSight(out _, out _))
                        game.Player.TakeDamage(Spec.Damage);
                    break;
                case EnemyAttack.Charge:
                    chargeHeading = target - transform.position;
                    chargeHeading.y = 0f;
                    chargeHeading.Normalize();
                    windupEnds = Time.time + .68f;
                    Awareness = EnemyAwareness.Windup;
                    game.Notify(Spec.Name.ToUpperInvariant() + " IS CHARGING!");
                    break;
                case EnemyAttack.Bolt: Shoot(target, 1, 0f); break;
                case EnemyAttack.Volley: Shoot(target, 3, 11f); break;
                case EnemyAttack.Summon:
                    if (minionSummons < 3 && attackCounter % 3 == 0) Summon(1);
                    else Shoot(target, 1, 0f);
                    break;
                case EnemyAttack.BossPattern:
                    if (Phase >= 2 && attackCounter % 3 == 0)
                        BossHazard.Create(game, game.Player.transform.position, Spec.Damage + Phase * 5,
                            Phase == 3 ? 3.5f : 2.9f);
                    else Shoot(target, Phase >= 2 ? 5 : 3, Phase >= 2 ? 12f : 10f);
                    break;
            }
        }

        private void Shoot(Vector3 target, int count, float spread)
        {
            Vector3 start = transform.position + Vector3.up * (IsBoss ? 2.0f : 1.2f);
            Vector3 aim = (target - start).normalized;
            float middle = (count - 1) * .5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.AngleAxis((i - middle) * spread, Vector3.up) * aim;
                Projectile.CreateEnemy(start + direction * (IsBoss ? 1.0f : .6f),
                    direction, Spec.Damage, Spec.ProjectileSpeed,
                    Spec.IsBoss ? Spec.Tint : Color.white);
            }
        }

        private void Summon(int count)
        {
            for (int i = 0; i < count && minionSummons < (IsBoss ? 7 : 3); i++)
            {
                var offset = new Vector2Int((minionSummons % 2 == 0 ? 3 : -3),
                    (minionSummons % 3 - 1) * 2);
                var goal = game.Map.ToCell(transform.position) + offset;
                if (game.Map.FindNearbyFree(goal, 4, out Vector2Int spot) &&
                    Vector3.Distance(game.Map.World(spot.x, spot.y), game.Player.transform.position) > 5f)
                {
                    EnemyFactory.Spawn(game, Spec.Minion, spot, false, true);
                    minionSummons++;
                }
            }
        }

        private void ApplyTint(Color tint)
        {
            sprite.color = tint;
            sprite.GetPropertyBlock(tintBlock);
            tintBlock.SetColor("_BaseColor", tint);
            tintBlock.SetColor("_Color", tint);
            sprite.SetPropertyBlock(tintBlock);
        }

        public void TakeDamage(int amount, AshPlayer source = null)
        {
            if (Health <= 0) return;
            Health = Mathf.Max(0, Health - Mathf.Max(1, amount));
            if (directional != null) directional.PlayHurt();
            AshAudio.OneShot(Health <= 0 ? (IsBoss ? "boss_death" : "enemy_death") : "enemy_hurt",
                transform.position,IsBoss ? 1f : .56f);
            ImpactDecal.AtFeet(Health <= 0 ? "blood" : "crack",transform.position,IsBoss ? 1.5f : .68f);
            CombatFeedback.Spawn(transform.position + Vector3.up * Spec.VisualHeight * .55f,
                IsBoss ? Spec.Tint : new Color(.95f, .24f, .18f), IsBoss);
            FloatingDamageNumber.Show(transform.position + Vector3.up * (Spec.VisualHeight * .95f),
                amount, IsBoss);
            if (sprite != null) ApplyTint(new Color(1f, .27f, .20f));
            flashUntil = Time.time + .14f;
            if (source != null) Alert(source.transform.position);
            if (Health <= 0)
            {
                Awareness = EnemyAwareness.Dead;
                queuedAttack = false;
                IsVisuallyMoving = false;
                if (directional != null) directional.PlayDeath();
                if (motor != null) motor.enabled = false;
                if (sprite != null) ApplyTint(Spec.Tint);
                CombatFeedback.Spawn(transform.position + Vector3.up * (Spec.VisualHeight * .4f),
                    Spec.Tint, true);
                game.UntrackEnemy(this);
                game.EnemyKilled();
                if (source != null) source.OnEnemyKilled();
                Destroy(gameObject, IsBoss ? 3.25f : 2.1f);
                return;
            }
            if (!IsBoss) return;
            int newPhase = HealthFraction <= .34f ? 3 : HealthFraction <= .66f ? 2 : 1;
            if (newPhase > Phase)
            {
                Phase = newPhase;
                game.Notify(Spec.Name + " / PHASE " + Phase + "! WATCH FOR NEW ATTACKS");
                Summon(Phase == 3 ? 3 : 2);
            }
        }
    }

    // Fixed map-authored spawn anchors: encounters activate when nearby, avoiding
    // excessive active enemies and creating planned ambush windows.
    public sealed class EnemySpawnAnchor : MonoBehaviour
    {
        private AshGame game;
        private EnemyArchetype kind;
        private Vector2Int cell;
        private float radius;
        private bool used;

        public void Initialize(AshGame owner, EnemyArchetype type, Vector2Int location, float triggerRadius)
        {
            game = owner; kind = type; cell = location; radius = triggerRadius;
        }
        private void Update()
        {
            if (used || game == null || game.Phase != GamePhase.Playing || game.Player == null) return;
            if (Vector3.Distance(game.Player.transform.position, transform.position) > radius) return;
            used = true;
            EnemyFactory.Spawn(game, kind, cell, false, false);
            Destroy(gameObject);
        }
    }

    public static class EnemyFactory
    {
        public static EnemyAI Spawn(AshGame game, EnemyArchetype kind, Vector2Int cell,
            bool patrol, bool registerInTotals)
        {
            if (game == null || game.Map == null || !game.Map.IsOpen(cell.x, cell.y)) return null;
            EnemySpec spec = EnemyCatalog.Get(kind);
            var go = new GameObject("ENEMY / " + spec.Name);
            go.transform.SetParent(game.WorldRoot.transform, false);
            go.transform.position = game.Map.World(cell.x, cell.y) + Vector3.up * .10f;
            go.transform.rotation = Quaternion.Euler(0f, (cell.x * 81 + cell.y * 37) % 360, 0f);
            var motor = go.AddComponent<CharacterController>();
            motor.slopeLimit = 55f;
            var picture = VisualFactory.Billboard(spec.Sprite, Vector3.zero,
                new Vector2(spec.VisualHeight * .70f, spec.VisualHeight), "2D monster billboard");
            picture.transform.SetParent(go.transform, false);
            picture.transform.localPosition = Vector3.up * (spec.VisualHeight * .5f);
            var renderer = picture.GetComponent<SpriteRenderer>();
            renderer.color = spec.Tint;
            var directional = picture.AddComponent<DirectionalSpriteAnimator>();
            directional.Setup(spec.Sprite, go.transform);
            if (renderer.sprite != null)
            {
                Vector3 imported = renderer.sprite.bounds.size;
                picture.transform.localScale = new Vector3(
                    spec.VisualHeight * .70f / imported.x,
                    spec.VisualHeight / imported.y, 1f);
            }
            var enemy = go.AddComponent<EnemyAI>();
            enemy.Initialize(game, kind, cell, patrol, renderer);
            if (registerInTotals) game.RegisterEnemy();
            return enemy;
        }
    }

    // Boss area-attack warning: ground indicator appears before it deals damage.
    public sealed class BossHazard : MonoBehaviour
    {
        private AshGame owner;
        private float triggeredAt, radius;
        private int damage;
        public static void Create(AshGame game, Vector3 location, int damage, float radius)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "BOSS / TELEGRAPHED GROUND BURST";
            marker.transform.SetParent(game.WorldRoot.transform, false);
            var cell = game.Map.ToCell(location);
            marker.transform.position = new Vector3(location.x, game.Map.Surface(cell.x, cell.y) + .06f, location.z);
            marker.transform.localScale = new Vector3(radius * 2f, .02f, radius * 2f);
            Object.Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = VisualFactory.StoneMaterial("door_runes",
                new Color(1f, .30f, .22f));
            var hazard = marker.AddComponent<BossHazard>();
            hazard.owner = game; hazard.damage = damage; hazard.radius = radius;
            hazard.triggeredAt = Time.time + 1.2f;
        }
        private void Update()
        {
            if (owner == null || owner.Phase != GamePhase.Playing) return;
            if (Time.time < triggeredAt) return;
            if (owner.Player != null && Vector3.Distance(
                new Vector3(owner.Player.transform.position.x, 0f, owner.Player.transform.position.z),
                new Vector3(transform.position.x, 0f, transform.position.z)) <= radius)
                owner.Player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
