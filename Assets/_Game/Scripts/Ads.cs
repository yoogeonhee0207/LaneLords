using System;
using System.Collections;
using UnityEngine;
#if CRAZY_SDK
using CrazyGames;
#endif

namespace TinySwordsBattle
{
    /// <summary>
    /// One place for CrazyGames ads and gameplay events.
    /// Built with the CrazyGames SDK when the CRAZY_SDK define is set (added automatically once the SDK is imported).
    /// Without the SDK: midgame ads do nothing, rewarded ads are simulated in the editor and hidden in builds.
    /// The game always keeps working: errors, adblock and empty ad slots just skip the ad.
    /// </summary>
    public static class Ads
    {
        static bool ready;
        static bool adblock;
        static bool gameplayOn;
        static float savedTimeScale = 1f;

        /// <summary>True while an ad is playing (the game is paused and muted).</summary>
        public static bool Busy { get; private set; }

        /// <summary>Show "watch an ad" buttons only when an ad can actually be shown.</summary>
        public static bool RewardedAvailable
        {
            get
            {
#if CRAZY_SDK
                return ready && !adblock;
#else
                return Application.isEditor;
#endif
            }
        }

        // After the first scene: the SDK creates its own GameObject during Init.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
#if CRAZY_SDK
            if (!CrazySDK.IsAvailable) return;
            CrazySDK.Init(() =>
            {
                ready = true;
                CrazySDK.Ad.HasAdblock(present => adblock = present);
            });
#endif
        }

        // ---------------- Gameplay events ----------------

        public static void GameplayStart()
        {
            if (gameplayOn) return;
            gameplayOn = true;
#if CRAZY_SDK
            if (ready) CrazySDK.Game.GameplayStart();
#endif
        }

        public static void GameplayStop()
        {
            if (!gameplayOn) return;
            gameplayOn = false;
#if CRAZY_SDK
            if (ready) CrazySDK.Game.GameplayStop();
#endif
        }

        // ---------------- Ads ----------------

        /// <summary>Between stages. Always calls done (right away if no ad shows).</summary>
        public static void Midgame(Action done)
        {
#if CRAZY_SDK
            if (ready && !Busy)
            {
                CrazySDK.Ad.RequestAd(CrazyAdType.Midgame, Begin, error => { End(); done?.Invoke(); }, () => { End(); done?.Invoke(); });
                return;
            }
#endif
            done?.Invoke();
        }

        /// <summary>Player chose to watch an ad. onReward runs only if the ad finished.</summary>
        public static void Rewarded(Action onReward, Action onFail = null)
        {
            if (Busy) return;
#if CRAZY_SDK
            if (ready)
            {
                CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded, Begin, error => { End(); onFail?.Invoke(); }, () => { End(); onReward?.Invoke(); });
                return;
            }
            onFail?.Invoke();
#else
            if (Application.isEditor) Runner.Instance.StartCoroutine(FakeAd(onReward));
            else onFail?.Invoke();
#endif
        }

        static void Begin()
        {
            if (Busy) return;
            Busy = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        static void End()
        {
            if (!Busy) return;
            Busy = false;
            Time.timeScale = savedTimeScale;
            AudioListener.pause = false;
        }

        // Editor stand-in for a rewarded ad: a one-second pause, then the reward.
        static IEnumerator FakeAd(Action onReward)
        {
            Begin();
            Debug.Log("[Lane Lords] (test) rewarded ad playing...");
            yield return new WaitForSecondsRealtime(1f);
            End();
            onReward?.Invoke();
        }

        class Runner : MonoBehaviour
        {
            static Runner instance;
            public static Runner Instance
            {
                get
                {
                    if (instance == null)
                    {
                        var go = new GameObject("Ads Runner");
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<Runner>();
                    }
                    return instance;
                }
            }
        }
    }
}
