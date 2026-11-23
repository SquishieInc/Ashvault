using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ashvault
{
    public static class VisualFactory
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        // Static dictionaries may survive exiting Play mode when Domain Reload is disabled.
        // Their UnityEngine.Object values do not necessarily survive, so clear them every run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            Sprites.Clear();
            Materials.Clear();
        }

        public static Sprite LoadSprite(string name, float pixelsPerUnit = 45f)
        {
            string key = name + ":" + pixelsPerUnit;
            if (Sprites.TryGetValue(key, out var cached))
            {
                if (cached != null) return cached;
                Sprites.Remove(key);
            }
            var image = Resources.Load<Texture2D>("Textures/" + name);
            if (image == null)
            {
                Debug.LogError("Missing prototype art: Resources/Textures/" + name);
                return null;
            }
            image.filterMode = FilterMode.Point;
            image.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(image, new Rect(0, 0, image.width, image.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            Sprites[key] = sprite;
            return sprite;
        }

        public static Material StoneMaterial(string textureName, Color tint, Vector2? tileRepeat = null)
        {
            Vector2 repeat = tileRepeat ?? Vector2.one;
            string key = textureName + ":" + ColorUtility.ToHtmlStringRGBA(tint) + ":" + repeat.x + ":" + repeat.y;
            if (Materials.TryGetValue(key, out var cached))
            {
                if (cached != null) return cached;
                // Unity destroyed the native material after the previous Play session.
                Materials.Remove(key);
            }
            bool usingScriptablePipeline = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = usingScriptablePipeline
                ? (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                : (Shader.Find("Standard") ?? Shader.Find("Unlit/Texture"));
            var material = new Material(shader) { name = "Ashvault_" + textureName };
            var texture = Resources.Load<Texture2D>("Textures/" + textureName);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", repeat);
                }
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                    material.SetTextureScale("_MainTex", repeat);
                }
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .09f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            Materials.Add(key, material);
            return material;
        }

        // SpriteRenderer with URP *3D* Lit alpha-clipped material. In a Universal Renderer
        // this receives point lights and geometry shadows; NOT the separate 2D Renderer.
        public static Material LitSpriteMaterial(string image) => SpriteMaterial(image, true);
        public static Material UnlitSpriteMaterial(string image) => SpriteMaterial(image, false);
        private static Material SpriteMaterial(string image, bool lit)
        {
            string key = (lit ? "lit:" : "unlit:") + image;
            if (Materials.TryGetValue(key, out var cached))
            {
                if (cached != null) return cached;
                // Unity destroyed the native material after the previous Play session.
                Materials.Remove(key);
            }
            Shader shader = lit ? Shader.Find("Universal Render Pipeline/Lit") :
                Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find(lit ? "Standard" : "Unlit/Transparent Cutout");
            var material = new Material(shader) { name = "Ashvault sprite / " + key };
            var tex = Resources.Load<Texture2D>("Textures/" + image);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", tex);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", .18f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .0f);
            if (material.HasProperty("_ReceiveShadows")) material.SetFloat("_ReceiveShadows", 1f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            Materials.Add(key, material);
            return material;
        }

        public static void SetSpriteTint(SpriteRenderer renderer, Color tint)
        {
            if (renderer == null) return;
            renderer.color = tint;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }

        public static GameObject Billboard(string image, Vector3 position, Vector2 size, string title)
        {
            var go = new GameObject(title);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(image);
            sr.sharedMaterial = LitSpriteMaterial(image);
            sr.shadowCastingMode = ShadowCastingMode.On;
            sr.receiveShadows = true;
            // Sprite pixels-per-unit is 45, so requested size roughly matches an enemy's height.
            if (sr.sprite != null)
            {
                var originalSize = sr.sprite.bounds.size;
                go.transform.localScale = new Vector3(size.x / originalSize.x, size.y / originalSize.y, 1);
            }
            go.AddComponent<CameraBillboard>();
            return go;
        }
    }

    public sealed class CameraBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 direction = cam.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
