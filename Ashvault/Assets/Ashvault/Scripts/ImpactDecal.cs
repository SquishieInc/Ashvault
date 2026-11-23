using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ashvault
{
    // Lightweight portable decals on actual wall/floor surfaces; avoids a mandatory
    // URP Decal Renderer Feature, which must otherwise be configured in every pipeline asset.
    // These quads do not receive decals themselves and do not participate in physics.
    public sealed class ImpactDecal : MonoBehaviour
    {
        private const int DesktopMaximum = 64;
        private const int MobileMaximum = 20;
        private static readonly Queue<ImpactDecal> Active = new Queue<ImpactDecal>();
        private Material instance;
        private Color tint;
        private float expires, fadesAt;

        public static void Place(string kind, Vector3 position, Vector3 normal, float size = .6f)
        {
            if (AshGame.Instance == null || AshGame.Instance.WorldRoot == null) return;
            var tex = Resources.Load<Texture2D>("Textures/Decals/" + kind);
            if (tex == null) return;
            int maximum = Application.isMobilePlatform ? MobileMaximum : DesktopMaximum;
            while (Active.Count >= maximum)
            {
                var oldest = Active.Dequeue();
                if (oldest != null) Destroy(oldest.gameObject);
            }
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = "DECAL / " + kind;
            decal.transform.SetParent(AshGame.Instance.WorldRoot.transform, false);
            decal.transform.position = position + normal.normalized * .022f;
            decal.transform.rotation = Quaternion.LookRotation(normal.normalized);
            decal.transform.localScale = new Vector3(size, size, 1f);
            Destroy(decal.GetComponent<Collider>());
            var script = decal.AddComponent<ImpactDecal>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            script.instance = new Material(shader) { name = "Instanced decal / " + kind };
            if (script.instance.HasProperty("_BaseMap")) script.instance.SetTexture("_BaseMap", tex);
            if (script.instance.HasProperty("_MainTex")) script.instance.SetTexture("_MainTex", tex);
            if (script.instance.HasProperty("_Surface")) script.instance.SetFloat("_Surface", 1);
            if (script.instance.HasProperty("_Blend")) script.instance.SetFloat("_Blend", 0);
            if (script.instance.HasProperty("_SrcBlend")) script.instance.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (script.instance.HasProperty("_DstBlend")) script.instance.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (script.instance.HasProperty("_ZWrite")) script.instance.SetFloat("_ZWrite", 0);
            script.instance.DisableKeyword("_ALPHATEST_ON");
            script.instance.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            script.instance.EnableKeyword("_ALPHABLEND_ON");
            script.instance.renderQueue = (int)RenderQueue.Transparent;
            decal.GetComponent<MeshRenderer>().sharedMaterial = script.instance;
            decal.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            script.tint = Color.white;
            script.expires = Time.time + (Application.isMobilePlatform ? 12f : 28f);
            script.fadesAt = script.expires - 7f;
            Active.Enqueue(script);
        }
        public static void AtFeet(string kind, Vector3 origin, float size)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin + Vector3.up * .65f,
                Vector3.down,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if (hit.collider.GetComponentInParent<EnemyAI>() != null ||
                    hit.collider.GetComponentInParent<AshPlayer>() != null ||
                    hit.collider.GetComponentInParent<ImpactDecal>() != null)continue;
                Place(kind,hit.point,hit.normal,size);
                return;
            }
        }
        private void Update()
        {
            if (Time.time >= expires) { Destroy(gameObject); return; }
            float fade = Time.time < fadesAt ? 1f : Mathf.Clamp01((expires-Time.time)/(expires-fadesAt));
            if (instance == null) return;
            Color c = new Color(tint.r, tint.g, tint.b, fade * .88f);
            if (instance.HasProperty("_BaseColor")) instance.SetColor("_BaseColor", c);
            if (instance.HasProperty("_Color")) instance.SetColor("_Color", c);
        }
        private void OnDestroy() { if (instance != null) Destroy(instance); }
    }
}
