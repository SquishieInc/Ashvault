using UnityEngine;

namespace Ashvault
{
    // Fixed-resolution world render; native-resolution GUI temporarily retained for prototyping.
    // TODO before mobile release: replace immediate-mode HUD with safe-area aware uGUI/UIToolkit.
    public sealed class RetroScreen : MonoBehaviour
    {
        private RenderTexture lowRes;
        private Camera attachedCamera;
        private GUIStyle header, subheader, caption, button, hud;
        private Texture2D backdrop, whitePixel;

        public void Bind(Camera camera)
        {
            Unbind();
            attachedCamera = camera;
            if (lowRes == null)
            {
                lowRes = new RenderTexture(426, 240, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Ashvault / 426x240 retro framebuffer",
                    filterMode = FilterMode.Point, antiAliasing = 1, useMipMap = false
                };
                lowRes.Create();
            }
            attachedCamera.targetTexture = lowRes;
        }
        public void Unbind()
        {
            if (attachedCamera != null) attachedCamera.targetTexture = null;
            attachedCamera = null;
        }
        private void OnDestroy()
        {
            Unbind();
            if (lowRes != null) { lowRes.Release(); Destroy(lowRes); }
            if (whitePixel != null) Destroy(whitePixel);
        }
        private void MakeStyles()
        {
            if (header != null) return;
            header = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                fontSize = 40, normal = { textColor = new Color(1f, .78f, .42f) }
            };
            subheader = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                fontSize = 20, normal = { textColor = new Color(.95f, .87f, .73f) }
            };
            caption = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 13, wordWrap = true,
                normal = { textColor = new Color(.85f, .83f, .78f) }
            };
            button = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                fontSize = 14, wordWrap = true, normal = { textColor = Color.white }
            };
            hud = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                fontSize = 17, normal = { textColor = new Color(1f, .84f, .53f) }
            };
            backdrop = Resources.Load<Texture2D>("Textures/menu_backdrop");
            whitePixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();
        }
        private void Solid(Rect rect, Color tint)
        {
            Color old = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, whitePixel);
            GUI.color = old;
        }
        private void OnGUI()
        {
            AshGame game = AshGame.Instance;
            if (game == null) return;
            MakeStyles();
            float w = Screen.width, h = Screen.height;
            if (game.Phase == GamePhase.ClassSelect)
            {
                ShowClassMenu(game, w, h);
                return;
            }
            if (lowRes != null) GUI.DrawTexture(new Rect(0, 0, w, h), lowRes, ScaleMode.StretchToFill, false);
            else Solid(new Rect(0, 0, w, h), new Color(.09f, .08f, .11f));
            if (game.Player != null && game.Player.DamageFlashAlpha > 0f)
                Solid(new Rect(0, 0, w, h), new Color(.82f, .075f, .065f, game.Player.DamageFlashAlpha));
            if (game.Player != null) ShowHUD(game, w, h);
            if (game.Phase == GamePhase.Paused)
                Modal(game, w, h, "PAUSED", "The dungeon awaits.");
            else if (game.Phase == GamePhase.Defeat)
                Modal(game, w, h, "YOU HAVE FALLEN", game.Blueprint != null ? game.Blueprint.title : "Ashvault");
            else if (game.Phase == GamePhase.Victory)
                Modal(game, w, h, "LEVEL CLEARED", "Kills " + game.Kills + "/" + game.TotalEnemies +
                    "   Secrets " + game.SecretsFound + "/" + game.TotalSecrets);
            else if (game.Phase == GamePhase.CampaignComplete)
                Modal(game, w, h, "ASHVAULT CLEARED", "All three episodes conquered!");
        }

        private void ShowClassMenu(AshGame game, float w, float h)
        {
            if (backdrop != null) GUI.DrawTexture(new Rect(0, 0, w, h), backdrop, ScaleMode.StretchToFill);
            else Solid(new Rect(0, 0, w, h), new Color(.1f, .07f, .11f));
            Solid(new Rect(0, 0, w, h), new Color(0, 0, 0, .50f));
            header.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .087f), 27, 48);
            subheader.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .045f), 13, 22);
            caption.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .025f), 10, 14);
            GUI.Label(new Rect(0, h * .01f, w, h * .105f), "ASHVAULT", header);
            GUI.Label(new Rect(0, h * .105f, w, h * .055f), "CLASS-BASED FANTASY RETRO FPS", subheader);
            float selectY = h * .17f, edge = w * .15f, selectorWidth = (w - 2f * edge - 12f) / 3f;
            for (int episode = 1; episode <= CampaignCatalog.Episodes; episode++)
            {
                int choice = episode;
                string label = (episode == game.SelectedEpisode ? "[ SELECTED ]  " : "") +
                    (episode == 1 ? "I  CINDER KEEP" : episode == 2 ? "II  WEEPING FEN" : "III  ASTRAL CITADEL");
                if (GUI.Button(new Rect(edge + (episode - 1) * (selectorWidth + 6f), selectY,
                    selectorWidth, Mathf.Max(29f, h * .078f)), label, button)) game.ChooseEpisode(choice);
            }
            float gap = Mathf.Max(4f, w * .008f), margin = Mathf.Max(12f, w * .045f);
            float width = (w - 2f * margin - 3f * gap) / 4f;
            float startY = h * .275f;
            float height = Mathf.Max(90f, h * .245f);
            for (int i = 0; i < GameCatalog.ClassCount; i++)
            {
                HeroClass kind = (HeroClass)i;
                int col = i % 4, row = i / 4;
                float x = margin + col * (width + gap);
                float y = startY + row * (height + gap);
                Solid(new Rect(x, y, width, height), new Color(.11f, .10f, .16f, .93f));
                subheader.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .037f), 12, 17);
                GUI.Label(new Rect(x + 2, y + 3f, width - 4f, 23f), GameCatalog.Title(kind), subheader);
                GUI.Label(new Rect(x + 6, y + 27f, width - 12f, height - 59f),
                    GameCatalog.Description(kind), caption);
                if (GUI.Button(new Rect(x + 6, y + height - 30f, width - 12f, 26f), "PLAY", button))
                    game.StartRun(kind);
            }
            caption.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .029f), 10, 13);
            GUI.Label(new Rect(12, h * .823f, w - 24f, h * .12f),
                "WASD: move   MOUSE: aim   CLICK: attack   E: doors/secrets   Q: weapon   ESC: pause", caption);
        }

        private void ShowHUD(AshGame game, float w, float h)
        {
            float bar = Mathf.Max(74f, h * .165f);
            if (game.Phase == GamePhase.Playing)
            {
                WeaponSpec weapon = game.Player.Equipped;
                Texture2D picture = Resources.Load<Texture2D>("Textures/" + weapon.SpriteName);
                if (picture != null)
                {
                    picture.filterMode = FilterMode.Point;
                    float size = Mathf.Min(w * .30f, h * .43f);
                    GUI.DrawTexture(new Rect((w - size) * .5f, h - bar - size + 22f, size, size),
                        picture, ScaleMode.ScaleToFit, true);
                }
                GUI.Label(new Rect(w * .5f - 15f, h * .46f - 12f, 30f, 30f), "+", subheader);
            }
            Solid(new Rect(0, h - bar, w, bar), new Color(.085f, .070f, .085f, .97f));
            Solid(new Rect(0, h - bar, w, 4f), new Color(.57f, .35f, .19f));
            hud.fontSize = Mathf.Clamp(Mathf.RoundToInt(bar * .18f), 12, 18);
            float y = h - bar + 5f;
            GUI.Label(new Rect(12, y, w * .21f, bar * .45f),
                "HP " + game.Player.Health + "/" + game.Player.MaxHealth, hud);
            GUI.Label(new Rect(w * .23f, y, w * .46f, bar * .45f), game.Player.Equipped.Name, hud);
            GUI.Label(new Rect(w * .72f, y, w * .26f, bar * .45f), "AMMO " +
                (game.Player.Equipped.Ammo < 0 ? "∞" : game.Player.Equipped.Ammo.ToString()), hud);
            hud.fontSize = Mathf.Clamp(Mathf.RoundToInt(bar * .145f), 10, 14);
            GUI.Label(new Rect(12, y + bar * .40f, w * .28f, bar * .39f),
                "SIGILS " + (game.Bronze ? "B" : "-") + (game.Azure ? "A" : "-") +
                (game.Crimson ? "C" : "-"), hud);
            GUI.Label(new Rect(w * .24f, y + bar * .40f, w * .43f, bar * .39f),
                "K " + game.Kills + "/" + game.TotalEnemies + "  S " +
                game.SecretsFound + "/" + game.TotalSecrets, hud);
            GUI.Label(new Rect(w * .63f, y + bar * .40f, w * .37f, bar * .39f),
                "E" + game.CurrentEpisode + "-M" + game.CurrentMapNumber, hud);
            if (game.CurrentBoss != null && game.CurrentBoss.Health > 0)
            {
                Solid(new Rect(w * .19f, h * .105f, w * .62f, 16f), new Color(.12f, .1f, .16f));
                Solid(new Rect(w * .19f, h * .105f, w * .62f * game.CurrentBoss.HealthFraction, 16f),
                    new Color(.89f, .31f, .24f));
                GUI.Label(new Rect(w * .17f, h * .060f, w * .66f, 30f),
                    game.CurrentBoss.Spec.Name + "  PHASE " + game.CurrentBoss.Phase, caption);
            }
            if (Time.unscaledTime < game.MessageUntil)
            {
                Solid(new Rect(w * .11f, 7f, w * .78f, Mathf.Max(30f, h * .06f)),
                    new Color(0, 0, 0, .82f));
                GUI.Label(new Rect(w * .12f, 7f, w * .76f, Mathf.Max(29f, h * .06f)), game.Message, caption);
            }
            if (Application.isMobilePlatform && game.Phase == GamePhase.Playing) MobileButtons(game, w, h, bar);
        }

        private void MobileButtons(AshGame game, float w, float h, float bar)
        {
            float size = Mathf.Max(64f, h * .15f);
            if (GUI.RepeatButton(new Rect(w - size - 12f, h - bar - size - 12f, size, size),
                "FIRE", button)) game.Player.TryFire();
            if (GUI.Button(new Rect(w - 2f * size - 26f, h - bar - size - 12f,
                size, size), "USE", button)) game.Player.TryInteract();
            if (GUI.Button(new Rect(w - size - 12f, 12f, size, 37f), "SWAP", button))
                game.Player.SwapWeapon();
            if (GUI.Button(new Rect(12f, 12f, size, 37f), "PAUSE", button)) game.SetPaused(true);
        }

        private void Modal(AshGame game, float w, float h, string title, string info)
        {
            Solid(new Rect(0, 0, w, h), new Color(0, 0, 0, .78f));
            float width = Mathf.Min(w * .82f, 580f);
            float left = (w - width) * .5f;
            header.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .085f), 26, 43);
            subheader.fontSize = Mathf.Clamp(Mathf.RoundToInt(h * .046f), 15, 21);
            GUI.Label(new Rect(left, h * .13f, width, 60f), title, header);
            GUI.Label(new Rect(left, h * .33f, width, 54f), info, subheader);
            if (game.Phase == GamePhase.Paused)
            {
                if (GUI.Button(new Rect(left, h * .54f, width, 45f), "RESUME", button))
                    game.SetPaused(false);
            }
            else if (game.Phase == GamePhase.Victory)
            {
                string next = game.HasNextMap ? "CONTINUE TO NEXT MAP" : "COMPLETE CAMPAIGN";
                if (GUI.Button(new Rect(left, h * .52f, width, 45f), next, button)) game.AdvanceLevel();
            }
            if (game.Phase != GamePhase.CampaignComplete)
                if (GUI.Button(new Rect(left, h * .68f, width, 40f), "RESTART THIS MAP", button))
                    game.RestartMap();
            if (GUI.Button(new Rect(left, h * .84f, width, 40f), "CHOOSE CLASS / EPISODE", button))
                game.ReturnToClasses();
        }
    }
}
