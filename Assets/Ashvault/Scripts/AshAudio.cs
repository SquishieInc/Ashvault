using System.Collections.Generic;
using UnityEngine;

namespace Ashvault
{
    // All included effects are locally generated original PCM waves; no SDK or network dependency.
    public static class AshAudio
    {
        private static readonly Dictionary<string, AudioClip> Cached = new Dictionary<string, AudioClip>();
        public static AudioClip Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Cached.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogWarning("Ashvault missing audio: " + name);
            Cached[name] = clip;
            return clip;
        }
        public static void OneShot(string name, Vector3 position, float gain = 0.75f, float pitch = 1f)
        {
            AudioClip clip = Get(name);
            if (clip == null) return;
            var go = new GameObject("SFX / " + name);
            var game = AshGame.Instance;
            if (game != null && game.WorldRoot != null) go.transform.SetParent(game.WorldRoot.transform);
            go.transform.position = position;
            var audio = go.AddComponent<AudioSource>();
            audio.clip = clip;
            audio.volume = Mathf.Clamp01(gain);
            audio.pitch = Mathf.Clamp(pitch, .6f, 1.8f);
            audio.spatialBlend = .9f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 2.5f;
            audio.maxDistance = 27f;
            audio.Play();
            Object.Destroy(go, clip.length / audio.pitch + .15f);
        }
        public static void Ambient(Transform root, int episode)
        {
            string name = episode == 1 ? "ambient_cinder" : episode == 2 ? "ambient_fen" : "ambient_astral";
            AudioClip clip = Get(name);
            if (clip == null) return;
            var go = new GameObject("AMBIENCE / " + name);
            go.transform.SetParent(root, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = .16f;
            source.Play();
        }
    }
}
