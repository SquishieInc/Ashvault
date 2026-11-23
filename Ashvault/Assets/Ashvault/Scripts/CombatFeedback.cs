using UnityEngine;

namespace Ashvault
{
    // Light-weight feedback deliberately designed to remain legible at 426 x 240.
    public sealed class CombatFeedback : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private float started, lifetime, rise, spin;
        private Color tint;
        private Vector3 startScale;

        public static void Spawn(Vector3 position, Color color, bool large = false)
        {
            var game = AshGame.Instance;
            if (game == null || game.WorldRoot == null) return;
            var spark = VisualFactory.Billboard("fx_impact", position,
                Vector2.one * (large ? 1.5f : .72f), "FX / HIT BURST");
            spark.transform.SetParent(game.WorldRoot.transform, true);
            var effect = spark.AddComponent<CombatFeedback>();
            effect.sprite = spark.GetComponent<SpriteRenderer>();
            effect.sprite.sharedMaterial = VisualFactory.UnlitSpriteMaterial("fx_impact");
            effect.started = Time.time;
            effect.lifetime = large ? .50f : .31f;
            effect.tint = color;
            effect.rise = large ? .8f : .35f;
            effect.startScale = spark.transform.localScale;
            effect.spin = (position.x * 47f + position.z * 17f) % 60f;
        }
        private void Update()
        {
            float u = (Time.time - started) / Mathf.Max(.01f, lifetime);
            if (u >= 1f) { Destroy(gameObject); return; }
            transform.position += Vector3.up * (rise * Time.deltaTime);
            transform.localScale = startScale * (1f + u * .8f);
            transform.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
            VisualFactory.SetSpriteTint(sprite, new Color(tint.r, tint.g, tint.b, (1f - u) * .9f));
        }
    }
}

namespace Ashvault
{
    // Minimal, pooled-object-free damage text; only spawned on actual hits.
    public sealed class FloatingDamageNumber : MonoBehaviour
    {
        private TextMesh label;
        private float started;
        private Color tint;
        public static void Show(Vector3 location, int amount, bool boss)
        {
            var game = AshGame.Instance;
            if (game == null || game.WorldRoot == null) return;
            var go = new GameObject("FX / DAMAGE / " + amount);
            go.transform.SetParent(game.WorldRoot.transform, false);
            go.transform.position = location;
            var text = go.AddComponent<TextMesh>();
            text.text = amount.ToString();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 55;
            text.characterSize = boss ? .042f : .027f;
            text.fontStyle = boss ? FontStyle.Bold : FontStyle.Normal;
            var popup = go.AddComponent<FloatingDamageNumber>();
            popup.label = text;
            popup.started = Time.time;
            popup.tint = boss ? new Color(1f, .84f, .29f) : new Color(1f, .94f, .88f);
        }
        private void LateUpdate()
        {
            if (label == null) return;
            float elapsed = Time.time - started;
            if (elapsed >= .68f) { Destroy(gameObject); return; }
            transform.position += Vector3.up * Time.deltaTime * .85f;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir = transform.position - cam.transform.position;
                if (dir.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(dir);
            }
            label.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01((.68f - elapsed) / .30f));
        }
    }
}
