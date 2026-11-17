using System.Collections;
using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Background music that survives scene loads and crossfades between tracks.
    /// Tracks are loaded by name from Resources/BGM (e.g. MusicPlayer.Play("bgm_menu")).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public const string VolumeKey = "music_volume";
        const float FadeTime = 0.8f;

        static MusicPlayer instance;
        AudioSource[] sources;
        AudioSource jingle;
        float duck = 1f; // music volume share while a jingle plays
        const float DuckLevel = 0.25f;
        int active;
        string current;
        Coroutine fade;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.6f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                if (instance != null && instance.fade == null) instance.sources[instance.active].volume = Volume;
            }
        }

        public static string ChapterTrack(int chapter) => "bgm_ch" + Mathf.Clamp(chapter, 1, 4);

        /// <summary>Play a track (looping). Does nothing if it is already playing.</summary>
        public static void Play(string track)
        {
            Ensure().PlayTrack(track);
        }

        static MusicPlayer Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("MusicPlayer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MusicPlayer>();
            instance.sources = new[] { go.AddComponent<AudioSource>(), go.AddComponent<AudioSource>() };
            foreach (var s in instance.sources)
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
            }
            instance.jingle = go.AddComponent<AudioSource>();
            instance.jingle.loop = false;
            instance.jingle.playOnAwake = false;
            return instance;
        }

        /// <summary>One-shot music sting (victory / defeat). The background music dips while it plays.</summary>
        public static void PlayJingle(string name)
        {
            var mp = Ensure();
            var clip = Resources.Load<AudioClip>("BGM/" + name);
            if (clip == null)
            {
                Debug.LogWarning("[Lane Lords] Jingle not found: Resources/BGM/" + name);
                return;
            }
            mp.jingle.clip = clip;
            mp.jingle.volume = Volume;
            mp.jingle.Play();
        }

        void Update()
        {
            // Duck the music under a playing jingle, then bring it back smoothly.
            float target = jingle != null && jingle.isPlaying ? DuckLevel : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * 2f);
            if (fade == null && sources != null) sources[active].volume = Volume * duck;
        }

        void PlayTrack(string track)
        {
            if (track == current) return;
            var clip = Resources.Load<AudioClip>("BGM/" + track);
            if (clip == null)
            {
                Debug.LogWarning("[Lane Lords] Music not found: Resources/BGM/" + track);
                return;
            }
            current = track;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Crossfade(clip));
        }

        IEnumerator Crossfade(AudioClip clip)
        {
            var from = sources[active];
            active = 1 - active;
            var to = sources[active];
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            float startFrom = from.volume;
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime) // works while the game is paused
            {
                float k = t / FadeTime;
                from.volume = startFrom * (1f - k);
                to.volume = Volume * duck * k;
                yield return null;
            }
            from.Stop();
            from.volume = 0f;
            to.volume = Volume * duck;
            fade = null;
        }
    }
}
