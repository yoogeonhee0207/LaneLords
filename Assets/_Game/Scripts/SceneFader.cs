using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Scene changes with a short fade to black. Shows "Loading..." if the next scene takes a while.
    /// Use SceneFader.Load(name) instead of SceneManager.LoadScene.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        const float FadeTime = 0.3f;
        const float LoadingTextAfter = 0.5f;

        static SceneFader instance;
        CanvasGroup group;
        Text loadingText;
        bool busy;

        public static void Load(string sceneName)
        {
            Ensure().StartLoad(sceneName);
        }

        public static void Reload()
        {
            Load(SceneManager.GetActiveScene().name);
        }

        static SceneFader Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("SceneFader", typeof(RectTransform));
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above every scene canvas
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            instance = go.AddComponent<SceneFader>();
            instance.group = go.AddComponent<CanvasGroup>();
            instance.group.alpha = 0f;
            instance.group.blocksRaycasts = false;

            var black = UIKit.Stretch("Black", go.transform);
            black.gameObject.AddComponent<Image>().color = Color.black;

            Font font = UISkin.Instance != null ? UISkin.Instance.font : null;
            var t = UIKit.Rect("Loading", go.transform, new Vector2(1f, 0f), new Vector2(-220f, 60f), new Vector2(400f, 80f));
            instance.loadingText = t.gameObject.AddComponent<Text>();
            instance.loadingText.font = font;
            instance.loadingText.fontSize = 40;
            instance.loadingText.alignment = TextAnchor.MiddleRight;
            instance.loadingText.color = Color.white;
            instance.loadingText.horizontalOverflow = HorizontalWrapMode.Overflow;
            instance.loadingText.enabled = false;
            return instance;
        }

        void StartLoad(string sceneName)
        {
            if (busy) return;
            StartCoroutine(Run(sceneName));
        }

        IEnumerator Run(string sceneName)
        {
            busy = true;
            group.blocksRaycasts = true;
            if (loadingText.font == null && UISkin.Instance != null) loadingText.font = UISkin.Instance.font;
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / FadeTime;
                yield return null;
            }
            group.alpha = 1f;

            var op = SceneManager.LoadSceneAsync(sceneName);
            float waited = 0f;
            while (op != null && !op.isDone)
            {
                waited += Time.unscaledDeltaTime;
                if (waited > LoadingTextAfter && !loadingText.enabled)
                {
                    loadingText.text = Loc.T("Loading...", "불러오는 중...");
                    loadingText.enabled = true;
                }
                yield return null;
            }
            loadingText.enabled = false;
            yield return null; // let the new scene run its Start first

            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeTime;
                yield return null;
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
            busy = false;
        }
    }
}
