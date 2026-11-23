using UnityEngine;

namespace Ashvault
{
    public struct HeroCarry
    {
        public int Health, PrimaryAmmo, SecondaryAmmo;
        public bool HasUpgrade, UsingUpgrade;
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class AshPlayer : MonoBehaviour
    {
        public AshGame Owner { get; private set; }
        public HeroClass Kind { get; private set; }
        public Camera View { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public WeaponSpec Equipped => activeSecondary ? secondary : primary;
        public bool HasUpgrade { get; private set; }

        private CharacterController motor;
        private WeaponSpec primary, secondary;
        private bool activeSecondary;
        private float verticalSpeed, pitch, nextShot;
        private float damageFeedbackUntil;
        public float DamageFlashAlpha => Mathf.Clamp01((damageFeedbackUntil - Time.unscaledTime) / .28f) * .30f;
        public void MoveWithPlatform(Vector3 delta)
        {
            if (motor != null && motor.enabled) motor.Move(delta);
        }

        public void Initialize(AshGame game, HeroClass kind)
        {
            Owner = game; Kind = kind;
            MaxHealth = GameCatalog.MaxHealth(kind);
            Health = MaxHealth;
            primary = GameCatalog.StartingWeapon(kind);
            secondary = GameCatalog.UpgradeWeapon(kind);
            motor = GetComponent<CharacterController>();
            motor.height = 1.85f;
            motor.center = Vector3.up * .94f;
            motor.radius = .37f;
            motor.stepOffset = .42f;  // Climb authored 0.22–0.30 m sector thresholds.
            motor.skinWidth = .06f;
            var eye = new GameObject("Eye / FPS camera");
            eye.transform.SetParent(transform, false);
            eye.transform.localPosition = Vector3.up * 1.57f;
            View = eye.AddComponent<Camera>();
            View.tag = "MainCamera";
            View.fieldOfView = 70f;
            View.nearClipPlane = .04f;
            View.farClipPlane = 180f;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.backgroundColor = new Color(.075f, .065f, .085f);
            View.allowHDR = false;
            View.allowMSAA = false;
            eye.AddComponent<AudioListener>();
        }

        public HeroCarry SaveCarry()
        {
            return new HeroCarry { Health = Health, PrimaryAmmo = primary.Ammo,
                SecondaryAmmo = secondary.Ammo, HasUpgrade = HasUpgrade, UsingUpgrade = activeSecondary };
        }
        public void RestoreCarry(HeroCarry carry)
        {
            Health = Mathf.Clamp(carry.Health, 1, MaxHealth);
            primary.Ammo = carry.PrimaryAmmo;
            secondary.Ammo = carry.SecondaryAmmo;
            HasUpgrade = carry.HasUpgrade;
            activeSecondary = carry.HasUpgrade && carry.UsingUpgrade;
        }

        private void Update()
        {
            if (Owner == null || Owner.Phase != GamePhase.Playing) return;
            Move();
            if (PlayerInput.Fire()) TryFire();
            if (PlayerInput.Interact()) TryInteract();
            if (PlayerInput.SwapWeapon()) SwapWeapon();
        }

        private void Move()
        {
            Vector2 look = PlayerInput.Look();
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -75f, 75f);
            View.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector2 axis = PlayerInput.Movement();
            Vector3 planar = transform.right * axis.x + transform.forward * axis.y;
            if (planar.sqrMagnitude > 1f) planar.Normalize();
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -1.5f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            motor.Move((planar * GameCatalog.Speed(Kind) +
                Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        public int AttackDamage(int baseDamage) => Mathf.Max(1,
            Mathf.RoundToInt(baseDamage * GameCatalog.DamageMultiplier(Kind, Health, MaxHealth)));

        public void TryFire()
        {
            if (Owner.Phase != GamePhase.Playing || Time.time < nextShot) return;
            var weapon = Equipped;
            if (weapon.Ammo == 0)
            {
                Owner.Notify("OUT OF AMMUNITION!");
                nextShot = Time.time + .25f;
                return;
            }
            if (weapon.Ammo > 0) weapon.Ammo--;
            nextShot = Time.time + weapon.Cooldown;
            Owner.AlertEnemies(transform.position, 20f);
            AshAudio.OneShot(weapon.Melee ? "melee_swing" : "bolt_cast", View.transform.position, .38f);
            if (weapon.Melee)
            {
                if (Physics.SphereCast(View.transform.position, .4f, View.transform.forward,
                    out RaycastHit hit, weapon.Range, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    var enemy = hit.collider.GetComponentInParent<EnemyAI>();
                    if (enemy != null) enemy.TakeDamage(AttackDamage(weapon.Damage), this);
                }
            }
            else
            {
                Vector3 position = View.transform.position + View.transform.forward * .8f;
                Projectile.Create(position, View.transform.forward, weapon, this);
            }
        }

        public void TryInteract()
        {
            if (Owner.Phase != GamePhase.Playing) return;
            if (Physics.Raycast(View.transform.position, View.transform.forward,
                out RaycastHit hit, 4.9f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                var usable = hit.collider.GetComponentInParent<IUsable>();
                if (usable != null) usable.Use(this);
            }
        }
        public void UnlockWeapon()
        {
            if (!HasUpgrade)
            {
                HasUpgrade = true;
                activeSecondary = true;
                Owner.Notify("NEW CLASS WEAPON: " + secondary.Name);
            }
            else
            {
                if (secondary.Ammo >= 0) secondary.Ammo += 35;
                Owner.Notify("WEAPON AMMUNITION REPLENISHED");
            }
        }
        public void SwapWeapon()
        {
            if (!HasUpgrade) { Owner.Notify("FIND YOUR CLASS WEAPON FIRST"); return; }
            activeSecondary = !activeSecondary;
            Owner.Notify("EQUIPPED: " + Equipped.Name);
        }
        public void Heal(int amount)
        {
            int gained = Mathf.Min(MaxHealth - Health, amount);
            Health += gained;
            if (gained > 0) Owner.Notify("+" + gained + " HEALTH");
        }
        public void OnEnemyKilled()
        {
            int restore = GameCatalog.HealOnKill(Kind);
            if (restore > 0) Health = Mathf.Min(MaxHealth, Health + restore);
        }
        public void TakeDamage(int amount)
        {
            if (Owner.Phase != GamePhase.Playing) return;
            int mitigated = Mathf.Max(1, Mathf.RoundToInt(amount * GameCatalog.IncomingMultiplier(Kind)));
            Health = Mathf.Max(0, Health - mitigated);
            damageFeedbackUntil = Time.unscaledTime + .28f;
            AshAudio.OneShot("player_hurt",transform.position,.55f);
            if (Health <= 0) Owner.PlayerDied();
        }
    }
}
