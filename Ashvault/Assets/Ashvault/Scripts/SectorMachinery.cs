using UnityEngine;

namespace Ashvault
{
    public enum MechanismKind { Lift, MovingPlatform, Crusher, RuneTrap }

    // One-tile machinery in optional rooms or hazards; map data authoring is in JSON.
    // Dynamic lift height is reflected in GridPathfinder through DungeonMap.SetDynamicFloor.
    // Moving platforms carry CharacterControllers manually (Unity does not automatically
    // parent/transport CharacterControllers placed on kinematic floors).
    public sealed class SectorMachinery : MonoBehaviour
    {
        private DungeonMap map;
        private AshGame game;
        private MechanismData data;
        private MechanismKind kind;
        private Vector3 initial;
        private float ground;
        private Renderer surface;
        private float lastPlayerHit = -10f;
        private readonly System.Collections.Generic.Dictionary<int, float> enemyHitTimes =
            new System.Collections.Generic.Dictionary<int, float>();
        private readonly System.Collections.Generic.List<EnemyAI> scanBuffer =
            new System.Collections.Generic.List<EnemyAI>();
        private Material dim, hot;
        private Light hazardGlow;

        public static void Spawn(AshGame owner, Transform root, DungeonMap map, MechanismData data,
            Material masonry, Material runes)
        {
            if (!map.IsOpen(data.x, data.z)) return;
            if (!System.Enum.TryParse(data.kind, true, out MechanismKind type)) return;
            var machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.name = "SECTOR / " + type + " / " + data.x + "," + data.z;
            machine.transform.SetParent(root, false);
            float floor = map.Surface(data.x, data.z);
            float top = type == MechanismKind.Crusher ? floor + 4.7f : floor + .14f;
            machine.transform.position = new Vector3(data.x * DungeonMap.Tile, top - .14f,
                data.z * DungeonMap.Tile);
            machine.transform.localScale = type == MechanismKind.Crusher ?
                new Vector3(DungeonMap.Tile * .9f, .36f, DungeonMap.Tile * .9f) :
                new Vector3(DungeonMap.Tile * .82f, .28f, DungeonMap.Tile * .82f);
            var r = machine.GetComponent<MeshRenderer>();
            r.sharedMaterial = type == MechanismKind.Crusher ? masonry : runes;
            var script = machine.AddComponent<SectorMachinery>();
            script.game = owner;
            script.map = map;
            script.data = data;
            script.kind = type;
            script.initial = machine.transform.position;
            script.ground = floor;
            script.surface = r;
            script.dim = masonry;
            script.hot = runes;
            // Collider is used as an actual rideable/solid surface; damage is evaluated
            // by a footprint and vertical sweep, not OnTriggerEnter (avoids double hits).
            machine.GetComponent<BoxCollider>().isTrigger = type == MechanismKind.RuneTrap;
            if (type == MechanismKind.RuneTrap || type == MechanismKind.Crusher)
            {
                var glowObject = new GameObject("HAZARD / TELEGRAPH LIGHT");
                glowObject.transform.SetParent(machine.transform, false);
                glowObject.transform.localPosition = new Vector3(0f, .35f, 0f);
                script.hazardGlow = glowObject.AddComponent<Light>();
                script.hazardGlow.type = LightType.Point;
                script.hazardGlow.color = type == MechanismKind.Crusher ?
                    new Color(1f, .14f, .10f) : new Color(1f, .34f, .11f);
                script.hazardGlow.range = 4.5f;
                script.hazardGlow.shadows = LightShadows.None;
                script.hazardGlow.intensity = .08f;
            }
        }

        private float Animation(float time)
        {
            float period = Mathf.Max(2.4f, data.period);
            // 0..1..0 with short restful plateaus at both ends.
            float cycle = Mathf.Repeat(time / period + data.phase, 1f);
            if (cycle < .18f) return 0;
            if (cycle < .43f) return Mathf.SmoothStep(0, 1, (cycle - .18f) / .25f);
            if (cycle < .58f) return 1;
            if (cycle < .83f) return Mathf.SmoothStep(1, 0, (cycle - .58f) / .25f);
            return 0;
        }

        private void Update()
        {
            if (game == null || game.Phase != GamePhase.Playing) return;
            float normalized = Animation(Time.time);
            float travel = Mathf.Max(.1f, data.travel);
            Vector3 next = initial;
            switch (kind)
            {
                case MechanismKind.Lift:
                    next.y += travel * normalized;
                    map.SetDynamicFloor(data.x, data.z, ground + travel * normalized);
                    break;
                case MechanismKind.MovingPlatform:
                    next += new Vector3(data.axisX, 0, data.axisZ).normalized * (travel * normalized);
                    break;
                case MechanismKind.Crusher:
                    next.y -= travel * normalized;
                    break;
            }
            if (hazardGlow != null)
                hazardGlow.intensity = kind == MechanismKind.Crusher ?
                    (.18f + 1.65f * normalized) : (normalized > .45f ? 2.2f : .12f);
            Vector3 delta = next - transform.position;
            if ((kind == MechanismKind.Lift || kind == MechanismKind.MovingPlatform) &&
                delta.sqrMagnitude > .0000001f) CarryRiders(delta);
            transform.position = next;
            if (kind == MechanismKind.Crusher && normalized > .62f) DamageCrushed();
            if (kind == MechanismKind.RuneTrap)
            {
                bool armed = normalized > .45f;
                surface.sharedMaterial = armed ? hot : dim;
                if (armed) DamageFootprint(1.05f, ground + .8f);
            }
        }

        private bool WithinFootprint(Vector3 position, float buffer = .7f)
        {
            float radius = DungeonMap.Tile * .39f + buffer;
            return Mathf.Abs(position.x - transform.position.x) <= radius &&
                   Mathf.Abs(position.z - transform.position.z) <= radius;
        }

        private void CarryRiders(Vector3 delta)
        {
            if (game.Player != null && WithinFootprint(game.Player.transform.position, -.2f) &&
                Mathf.Abs(game.Player.transform.position.y - (transform.position.y + .14f)) < .50f)
                game.Player.MoveWithPlatform(delta);
            foreach (var foe in game.ActiveEnemies)
                if (foe != null && foe.Health > 0 && WithinFootprint(foe.transform.position, -.2f) &&
                    Mathf.Abs(foe.transform.position.y - (transform.position.y + .14f)) < .50f)
                    foe.MoveWithPlatform(delta);
        }

        private void DamageCrushed()
        {
            float slabBottom = transform.position.y - .18f;
            // Only damage if headroom actually intersects the descending slab.
            DamageFootprint(slabBottom - .35f, ground + 2.1f);
        }

        private void DamageFootprint(float minimumHeight, float maximumHeight)
        {
            if (game.Player != null && WithinFootprint(game.Player.transform.position) &&
                game.Player.transform.position.y + 1.55f >= minimumHeight &&
                game.Player.transform.position.y <= maximumHeight && Time.time >= lastPlayerHit + 1f)
            {
                lastPlayerHit = Time.time;
                game.Player.TakeDamage(Mathf.Max(4, data.damage));
                AshAudio.OneShot(kind == MechanismKind.Crusher ? "crusher_impact" : "rune_trap",
                    transform.position,.45f);
                ImpactDecal.AtFeet("crack",game.Player.transform.position,.8f);
                CombatFeedback.Spawn(game.Player.transform.position + Vector3.up * 1f,
                    new Color(1f, .28f, .08f), true);
            }
            // Snapshot because lethal trap damage unregisters enemies from AshGame.
            scanBuffer.Clear();
            scanBuffer.AddRange(game.ActiveEnemies);
            foreach (var foe in scanBuffer)
            {
                if (foe == null || foe.Health <= 0 || !WithinFootprint(foe.transform.position) ||
                    foe.transform.position.y + 1.3f < minimumHeight ||
                    foe.transform.position.y > maximumHeight) continue;
                int id = foe.GetInstanceID();
                if (enemyHitTimes.TryGetValue(id, out float previous) && Time.time < previous + 1f) continue;
                enemyHitTimes[id] = Time.time;
                foe.TakeDamage(Mathf.Max(4, data.damage));
            }
        }

        private void OnDestroy()
        {
            if (map != null && kind == MechanismKind.Lift)
                map.ClearDynamicFloor(data.x, data.z);
        }
    }
}
