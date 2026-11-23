using UnityEngine;

namespace Ashvault
{
    public interface IUsable { void Use(AshPlayer actor); }

    public sealed class KeyPickup : MonoBehaviour
    {
        public SigilKey Kind;
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<AshPlayer>();
            if (player == null) return;
            player.Owner.AcquireKey(Kind);
            Destroy(gameObject);
        }
    }

    public sealed class WeaponPickup : MonoBehaviour
    {
        public HeroClass ClassRequired;
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<AshPlayer>();
            if (player == null || player.Kind != ClassRequired) return;
            player.UnlockWeapon();
            Destroy(gameObject);
        }
    }

    public sealed class HealthPickup : MonoBehaviour
    {
        public int Amount = 35;
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<AshPlayer>();
            if (player == null || player.Health >= player.MaxHealth) return;
            player.Heal(Amount);
            Destroy(gameObject);
        }
    }

    public sealed class LockedDoor : MonoBehaviour, IUsable
    {
        private DungeonMap map;
        private int x, z;
        private char symbol;
        private bool opening;
        private float started;
        private Vector3 closedPosition;
        private Collider barrier;
        private float openedAmount;
        public float OpenAmount => openedAmount;
        public bool IsOpening => opening;
        private const float TileSlide = DungeonMap.Tile*1.25f;
        public bool Matches(int cellX,int cellZ) => x==cellX && z==cellZ;

        public void Configure(AshGame game, DungeonMap level, int cellX, int cellZ, char gate)
        {
            map = level; x = cellX; z = cellZ; symbol = gate;
            closedPosition = transform.position;
            barrier = GetComponent<Collider>();
        }

        public void Use(AshPlayer actor)
        {
            if (opening) return;
            string needed = symbol == 'B' ? "BRONZE" : symbol == 'A' ? "AZURE" :
                symbol == 'C' ? "CRIMSON" : "ALL THREE";
            if (symbol == 'B' && !actor.Owner.Bronze ||
                symbol == 'A' && !actor.Owner.Azure ||
                symbol == 'C' && !actor.Owner.Crimson ||
                symbol == 'Z' && !(actor.Owner.Bronze && actor.Owner.Azure && actor.Owner.Crimson))
            {
                actor.Owner.Notify("SEALED: " + needed + " SIGIL REQUIRED");
                AshAudio.OneShot("door_locked",transform.position,.42f);
                return;
            }
            if (symbol >= '1' && symbol <= '8' && (int)actor.Kind != symbol - '1')
            {
                actor.Owner.Notify("SEALED: " + GameCatalog.Title((HeroClass)(symbol - '1')) + " PATH");
                AshAudio.OneShot("door_locked",transform.position,.42f);
                return;
            }
            opening = true;
            started = Time.time;
            actor.Owner.Notify("THE STONE SEAL RISES");
            AshAudio.OneShot("door_open",transform.position,.9f);
        }

        private void Update()
        {
            if (!opening) return;
            float percent = Mathf.Clamp01((Time.time - started) / 1.05f);
            openedAmount = Mathf.SmoothStep(0f, 1f, percent);
            if (symbol == 'A')
                transform.position = closedPosition; // Two visual wings open sideways.
            else if (symbol == 'C')
                transform.position = closedPosition + Vector3.right * (TileSlide * openedAmount);
            else transform.position = closedPosition + Vector3.up * (5.25f * openedAmount);
            if (percent < 1f) return;
            barrier.enabled = false;
            map.Unseal(x, z);
            enabled = false;
        }
    }

    public sealed class SecretWall : MonoBehaviour, IUsable
    {
        private DungeonMap map;
        private int x, z;
        private bool opening;
        private float started;
        private Vector3 closedPosition;

        public void Configure(DungeonMap grid, int cellX, int cellZ)
        {
            map = grid; x = cellX; z = cellZ;
            closedPosition = transform.position;
        }
        public void Use(AshPlayer actor)
        {
            if (opening) return;
            opening = true;
            started = Time.time;
            actor.Owner.CollectSecret();
            AshAudio.OneShot("secret_reveal",transform.position,.82f);
        }
        private void Update()
        {
            if (!opening) return;
            float percent = Mathf.Clamp01((Time.time - started) / .9f);
            transform.position = closedPosition + Vector3.up * (5.8f * percent);
            if (percent < 1f) return;
            GetComponent<Collider>().enabled = false;
            map.Unseal(x, z);
            enabled = false;
        }
    }

    public sealed class ExitSeal : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<AshPlayer>();
            if (player != null) player.Owner.FinishLevel();
        }
        // If the player reaches the portal before killing the boss, standing
        // inside should still complete the level once the boss is defeated.
        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponent<AshPlayer>();
            if (player == null || player.Owner.Phase != GamePhase.Playing) return;
            var game = player.Owner;
            if (game.Bronze && game.Azure && game.Crimson &&
                (game.CurrentBoss == null || game.CurrentBoss.Health <= 0)) game.FinishLevel();
        }
    }

    public sealed class PickupSpin : MonoBehaviour
    {
        private float y;
        private void Start() { y = transform.position.y; }
        private void Update()
        {
            transform.Rotate(0, Time.deltaTime * 45f, 0, Space.World);
            var p = transform.position;
            p.y = y + Mathf.Sin(Time.time * 2.2f) * .09f;
            transform.position = p;
        }
    }
}
