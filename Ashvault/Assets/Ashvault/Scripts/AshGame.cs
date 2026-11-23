using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    // Campaign director; a class choice persists through all 3x3 maps of a run.
    public sealed class AshGame : MonoBehaviour
    {
        public static AshGame Instance { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.ClassSelect;
        public HeroClass SelectedClass { get; private set; }
        public int SelectedEpisode { get; private set; } = 1;
        public int CurrentEpisode { get; private set; } = 1;
        public int CurrentMapNumber { get; private set; } = 1;
        public LevelBlueprint Blueprint { get; private set; }
        public DungeonMap Map { get; private set; }
        public AshPlayer Player { get; private set; }
        public Camera PlayerCamera { get; private set; }
        public GameObject WorldRoot { get; private set; }
        public int SecretsFound { get; private set; }
        public int TotalSecrets { get; private set; }
        public int Kills { get; private set; }
        public int TotalEnemies { get; private set; }
        public bool Bronze { get; private set; }
        public bool Azure { get; private set; }
        public bool Crimson { get; private set; }
        public string Message { get; private set; }
        public float MessageUntil { get; private set; }
        public bool HasNextMap => CurrentEpisode < CampaignCatalog.Episodes ||
            CurrentMapNumber < CampaignCatalog.MapsPerEpisode;
        public EnemyAI CurrentBoss { get; private set; }

        private RetroScreen screen;
        private readonly HashSet<EnemyAI> activeEnemies = new HashSet<EnemyAI>();
        public IEnumerable<EnemyAI> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            screen = gameObject.AddComponent<RetroScreen>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (Phase == GamePhase.Playing && PlayerInput.Pause()) SetPaused(true);
            else if (Phase == GamePhase.Paused && PlayerInput.Pause()) SetPaused(false);
        }

        public void ChooseEpisode(int episode)
        {
            if (Phase == GamePhase.ClassSelect)
                SelectedEpisode = Mathf.Clamp(episode, 1, CampaignCatalog.Episodes);
        }
        public void StartRun(HeroClass hero)
        {
            SelectedClass = hero;
            CurrentEpisode = SelectedEpisode;
            CurrentMapNumber = 1;
            LoadMap(null);
        }
        public void RestartMap() => LoadMap(null);

        public void AdvanceLevel()
        {
            if (Phase != GamePhase.Victory) return;
            if (!HasNextMap)
            {
                Phase = GamePhase.CampaignComplete;
                return;
            }
            HeroCarry carry = Player != null ? Player.SaveCarry() : default;
            if (CurrentMapNumber < CampaignCatalog.MapsPerEpisode) CurrentMapNumber++;
            else { CurrentEpisode++; CurrentMapNumber = 1; }
            LoadMap(carry);
        }

        private void LoadMap(HeroCarry? carry)
        {
            LevelBlueprint level = CampaignCatalog.Load(CurrentEpisode, CurrentMapNumber);
            if (level == null) { ReturnToClasses(); return; }
            Time.timeScale = 1f;
            screen.Unbind();
            if (WorldRoot != null) Destroy(WorldRoot);
            if (Player != null) Destroy(Player.gameObject);
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.enabled = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) listener.enabled = false;

            activeEnemies.Clear();
            CurrentBoss = null;
            Blueprint = level;
            Map = new DungeonMap(level);
            Bronze = Azure = Crimson = false;
            SecretsFound = Kills = TotalSecrets = TotalEnemies = 0;
            Phase = GamePhase.Playing;
            WorldRoot = new GameObject("LEVEL / " + level.id + " / " + level.title);
            Atmosphere.Configure(level.episode);
            DungeonBuilder.Build(this, WorldRoot.transform, Map);

            Vector2Int start = Map.Locate('P');
            var pawn = new GameObject("PLAYER / " + GameCatalog.Title(SelectedClass));
            pawn.transform.position = Map.World(start.x, start.y) + Vector3.up * .15f;
            pawn.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Player = pawn.AddComponent<AshPlayer>();
            Player.Initialize(this, SelectedClass);
            if (carry.HasValue) Player.RestoreCarry(carry.Value);
            PlayerCamera = Player.View;
            screen.Bind(PlayerCamera);
            Notify(level.intro + " / FIND 3 SIGILS");
            LockPointer();
        }

        public void SetPaused(bool paused)
        {
            if (Phase != GamePhase.Playing && Phase != GamePhase.Paused) return;
            Phase = paused ? GamePhase.Paused : GamePhase.Playing;
            Time.timeScale = paused ? 0f : 1f;
            if (paused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else LockPointer();
        }

        private static void LockPointer()
        {
            if (Application.isMobilePlatform) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReturnToClasses()
        {
            Time.timeScale = 1f;
            Phase = GamePhase.ClassSelect;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (screen != null) screen.Unbind();
            if (WorldRoot != null) Destroy(WorldRoot);
            if (Player != null) Destroy(Player.gameObject);
            WorldRoot = null;
            Player = null;
            PlayerCamera = null;
            CurrentBoss = null;
            Blueprint = null;
            Map = null;
            activeEnemies.Clear();
        }

        public void AcquireKey(SigilKey key)
        {
            if (key == SigilKey.Bronze) Bronze = true;
            if (key == SigilKey.Azure) Azure = true;
            if (key == SigilKey.Crimson) Crimson = true;
            Notify(key.ToString().ToUpperInvariant() + " SIGIL ACQUIRED!");
        }
        public void CollectSecret()
        {
            SecretsFound++;
            Notify("SECRET FOUND! " + SecretsFound + " / " + TotalSecrets);
        }
        public void RegisterSecret() { TotalSecrets++; }
        public void RegisterEnemy() { TotalEnemies++; }
        public void EnemyKilled() { Kills++; }
        public void TrackEnemy(EnemyAI enemy)
        {
            activeEnemies.Add(enemy);
            if (enemy.IsBoss) CurrentBoss = enemy;
        }
        public void UntrackEnemy(EnemyAI enemy)
        {
            activeEnemies.Remove(enemy);
            if (CurrentBoss == enemy) CurrentBoss = null;
        }
        public void AlertEnemies(Vector3 origin, float radius)
        {
            foreach (var enemy in activeEnemies)
                if (enemy != null) enemy.Hear(origin, radius);
        }
        public void Notify(string message)
        {
            Message = message;
            MessageUntil = Time.unscaledTime + 3.7f;
        }
        public void PlayerDied()
        {
            Phase = GamePhase.Defeat;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        public void FinishLevel()
        {
            if (Phase != GamePhase.Playing) return;
            if (!(Bronze && Azure && Crimson)) { Notify("THREE SIGILS REQUIRED"); return; }
            if (CurrentBoss != null && CurrentBoss.Health > 0)
            {
                Notify("DEFEAT " + CurrentBoss.Spec.Name + " TO EXIT!");
                return;
            }
            Phase = GamePhase.Victory;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public static class AshvaultBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartDemo()
        {
            if (Object.FindAnyObjectByType<AshGame>() != null) return;
            new GameObject("ASHVAULT / GAME DIRECTOR").AddComponent<AshGame>();
        }
    }
}
