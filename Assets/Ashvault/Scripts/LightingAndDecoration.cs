using UnityEngine;
using UnityEngine.Rendering;

namespace Ashvault
{
    public static class Atmosphere
    {
        public static void Configure(int episode)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = episode == 1 ? new Color(.19f, .13f, .13f) :
                episode == 2 ? new Color(.10f, .18f, .13f) : new Color(.13f, .12f, .24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = episode == 1 ? new Color(.12f, .08f, .08f) :
                episode == 2 ? new Color(.055f, .11f, .085f) : new Color(.085f, .065f, .14f);
            RenderSettings.fogDensity = episode == 1 ? .012f : .017f;
        }
    }

    [RequireComponent(typeof(Light))]
    public sealed class TorchFlicker : MonoBehaviour
    {
        public float BaseIntensity = 2.5f;
        public float LowFrequency = 1.9f;
        public float HighFrequency = 9f;
        public float FlickerAmount = .22f;
        private Light fire;
        private float offset;
        private void Awake()
        {
            fire = GetComponent<Light>();
            offset = (transform.position.x * .173f + transform.position.z * .097f) % 6.28f;
        }
        private void Update()
        {
            float slow = Mathf.Sin(Time.time * LowFrequency + offset);
            float fast = Mathf.Sin(Time.time * HighFrequency + offset * 2.8f);
            float drift = Mathf.Sin(Time.time * 3.83f + offset * 1.7f);
            float flicker = .70f * slow + .21f * fast + .09f * drift;
            fire.intensity = BaseIntensity * Mathf.Max(.35f, 1f + FlickerAmount * flicker);
        }
    }

    public static class EnvironmentDecor
    {
        public static GameObject Spawn(Transform root, DungeonMap map, DecorationData data)
        {
            string art = "deco_" + data.kind;
            var go = VisualFactory.Billboard(art,
                map.World(data.x, data.z) + Vector3.up * Mathf.Max(.2f, data.height * .5f),
                new Vector2(data.width > 0 ? data.width : 1.8f, data.height > 0 ? data.height : 2.5f),
                "PROP / " + data.kind + " / " + data.x + "," + data.z);
            go.transform.SetParent(root, true);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.shadowCastingMode = ShadowCastingMode.On;
            sr.receiveShadows = true;
            if (data.kind == "brazier" || data.kind == "mushrooms" ||
                data.kind == "crystal" || data.kind == "star_lantern")
                go.AddComponent<EnvironmentSpriteAnimator>().Initialize(data.kind, 4.2f);
            if (data.glow)
            {
                var lamp = new GameObject("Prop glow / " + data.kind);
                lamp.transform.SetParent(go.transform, false);
                lamp.transform.localPosition = new Vector3(0f, .32f, -.25f);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 5.2f;
                light.intensity = 1.1f;
                light.color = map.Level.episode == 1 ? new Color(1f, .40f, .16f) :
                    map.Level.episode == 2 ? new Color(.25f, 1f, .48f) : new Color(.59f, .38f, 1f);
                light.shadows = LightShadows.None;
                var flicker = lamp.AddComponent<TorchFlicker>();
                flicker.BaseIntensity = light.intensity;
                flicker.FlickerAmount = .11f;
            }
            return go;
        }
    }
}
