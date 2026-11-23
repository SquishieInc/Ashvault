using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnvironmentSpriteAnimator : MonoBehaviour
    {
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
        private SpriteRenderer sprite;
        private Sprite[] frames;
        private float speed, offset;
        public void Initialize(string kind, float fps = 4f)
        {
            sprite = GetComponent<SpriteRenderer>();
            string path = "Textures/Animated/" + kind + "_frames";
            if (!Cache.TryGetValue(path, out frames))
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex == null || tex.width != 512 || tex.height != 128) return;
                tex.filterMode = FilterMode.Point;
                frames = new Sprite[4];
                for (int i = 0; i < 4; i++) frames[i] = Sprite.Create(tex,
                    new Rect(i * 128, 0, 128, 128), new Vector2(.5f, .5f), 64f,
                    0, SpriteMeshType.FullRect);
                Cache[path] = frames;
            }
            speed = Mathf.Max(.5f, fps);
            offset = Mathf.Abs(transform.position.x * 2.1f + transform.position.z * .83f) % 4f;
            if (frames != null) sprite.sprite = frames[0];
        }
        private void Update()
        {
            if (frames == null || sprite == null) return;
            sprite.sprite = frames[Mathf.FloorToInt(Time.time * speed + offset) % frames.Length];
        }
    }

    // Scrolling stone-floor overlays: lava, mire water, or astral currents.
    // Geometry is decorative, while existing RuneTraps remain authoritative for damage.
    public sealed class FlowingSurface : MonoBehaviour
    {
        private Material localMaterial;
        private Vector2 speed;
        private Vector2 offset;
        public static void Spawn(Transform root, DungeonMap map, AnimatedSurfaceData data)
        {
            if (!map.IsOpen(data.x, data.z)) return;
            string texture = data.kind == "lava" ? "env_lava" :
                data.kind == "mire" ? "env_mire" : "env_aether";
            var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "AMBIENT / FLOWING " + data.kind;
            panel.transform.SetParent(root, false);
            panel.transform.position = map.World(data.x, data.z) + Vector3.up * .035f;
            panel.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            float width = Mathf.Clamp(data.width, .25f, 2.6f) * DungeonMap.Tile;
            panel.transform.localScale = new Vector3(width, width, 1f);
            UnityEngine.Object.Destroy(panel.GetComponent<Collider>());
            var flow = panel.AddComponent<FlowingSurface>();
            flow.localMaterial = new Material(VisualFactory.StoneMaterial(texture, Color.white));
            flow.localMaterial.name = "Unique scrolling liquid / " + data.kind;
            flow.localMaterial.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            panel.GetComponent<Renderer>().sharedMaterial = flow.localMaterial;
            flow.speed = new Vector2(.022f, -.055f);
        }
        private void Update()
        {
            if (localMaterial == null) return;
            offset += speed * Time.deltaTime;
            if (localMaterial.HasProperty("_BaseMap")) localMaterial.SetTextureOffset("_BaseMap", offset);
            if (localMaterial.HasProperty("_MainTex")) localMaterial.SetTextureOffset("_MainTex", offset);
        }
        private void OnDestroy() { if (localMaterial != null) Destroy(localMaterial); }
    }
}
