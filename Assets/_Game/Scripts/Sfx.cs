using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Sound effects. Each key is a folder in Resources/SFX; a random clip from that folder is played.
    /// Limits spam: the same key plays at most once per MinGap seconds and at most MaxVoices sounds at once.
    /// Long source files can be cut short with maxLength (faded out).
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public const string VolumeKey = "sfx_volume";
        const int MaxVoices = 10;
        const float MinGap = 0.08f;
        const float FadeOut = 0.12f;

        static Sfx instance;
        readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        AudioSource[] voices;
        float[] stopAt;
        float[] baseVolume;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        }

        // Per-key loudness and length caps (seconds, 0 = full clip).
        static readonly Dictionary<string, float> Loudness = new Dictionary<string, float>
        {
            { "hit_metal", 0.35f }, { "hit_enemy", 0.35f }, { "hit_animal", 0.35f }, { "bow", 0.4f }, { "throw", 0.35f },
            { "death", 0.4f }, { "castle_hit", 0.3f }, { "monk_burst", 0.5f }, { "spawn", 0.6f }, { "spawn_worker", 0.6f },
            { "boss_shaman", 1f }, { "boss_panda", 1f }, { "boss_mino", 1f }, { "boss_troll", 1f }, { "castle_break", 1f },
        };
        static readonly Dictionary<string, float> MaxLength = new Dictionary<string, float>
        {
            { "bow", 0.6f }, { "hit_metal", 0.6f }, { "castle_hit", 0.5f }, { "curse_free", 1.5f }, { "death", 1.0f },
            { "ui_fail", 1.2f }, { "monk_burst", 1.2f }, { "bomb", 1.2f },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Ensure();
            // Plain button clicks are silent on purpose (they felt noisy); buttons with a meaning
            // (buy, denied, pause, spawn...) still play their own sounds.
        }

        static Sfx Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sfx>();
            instance.voices = new AudioSource[MaxVoices];
            instance.stopAt = new float[MaxVoices];
            instance.baseVolume = new float[MaxVoices];
            for (int i = 0; i < MaxVoices; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                instance.voices[i] = s;
            }
            return instance;
        }

        /// <summary>Play a sound effect (UI, banners: always audible).</summary>
        public static void Play(string key, float volume = 1f)
        {
            Ensure().PlayKey(key, volume);
        }

        /// <summary>Play a battle sound at a world position. Skipped when the spot is off screen.</summary>
        public static void PlayAt(string key, Vector3 position, float volume = 1f)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 v = cam.WorldToViewportPoint(position);
                if (v.x < -0.05f || v.x > 1.05f) return;
            }
            Ensure().PlayKey(key, volume);
        }

        void PlayKey(string key, float volume)
        {
            if (lastPlayed.TryGetValue(key, out float t) && Time.unscaledTime - t < MinGap) return;
            AudioClip[] set = Load(key);
            if (set == null || set.Length == 0) return;

            int v = FreeVoice();
            if (v < 0) return;
            lastPlayed[key] = Time.unscaledTime;

            float loud = Loudness.TryGetValue(key, out float l) ? l : 0.8f;
            var src = voices[v];
            src.clip = set[Random.Range(0, set.Length)];
            src.pitch = Random.Range(0.92f, 1.08f);
            baseVolume[v] = Volume * loud * volume;
            src.volume = baseVolume[v];
            stopAt[v] = MaxLength.TryGetValue(key, out float m) ? Time.unscaledTime + m : 0f;
            src.Play();
        }

        int FreeVoice()
        {
            for (int i = 0; i < voices.Length; i++)
                if (!voices[i].isPlaying) return i;
            return -1; // all busy: drop the sound rather than cut another
        }

        AudioClip[] Load(string key)
        {
            if (clips.TryGetValue(key, out var set)) return set;
            set = Resources.LoadAll<AudioClip>("SFX/" + key);
            if (set == null || set.Length == 0) Debug.LogWarning("[Lane Lords] Sound not found: Resources/SFX/" + key);
            clips[key] = set;
            return set;
        }

        void Update()
        {
            // Cut long clips: fade out over FadeOut seconds before stopAt.
            float now = Time.unscaledTime;
            for (int i = 0; i < voices.Length; i++)
            {
                if (stopAt[i] <= 0f || !voices[i].isPlaying) continue;
                float left = stopAt[i] - now;
                if (left <= 0f) { voices[i].Stop(); stopAt[i] = 0f; }
                else if (left < FadeOut) voices[i].volume = baseVolume[i] * (left / FadeOut);
            }
        }

    }
}
