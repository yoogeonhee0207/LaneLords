using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Title screen: floating logo, twinkling sparkles, blinking "Tap to start". Any tap goes to the stage select screen.
    /// The background and logo are plain images (Assets/_Game/Art/Title/title_bg.png, title_logo.png) so they can be swapped.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public string nextScene = "StageSelect";
        public RectTransform logo;
        public Text logoText;
        public Text tapText;
        public Text versionText;
        public RectTransform[] sparkles;

        Vector2 logoStart;
        float[] sparklePhase;
        bool leaving;

        void Start()
        {
            Time.timeScale = 1f;
            MusicPlayer.Play("bgm_title");
            if (logo != null) logoStart = logo.anchoredPosition;
            if (logoText != null) logoText.text = GameInfo.Name + "\n<size=40>" + GameInfo.Subtitle + "</size>";
            if (tapText != null) tapText.text = Loc.T("TAP TO START", "터치해서 시작");
            if (versionText != null) versionText.text = GameInfo.Version;
            sparklePhase = new float[sparkles != null ? sparkles.Length : 0];
            for (int i = 0; i < sparklePhase.Length; i++) sparklePhase[i] = Random.Range(0f, 10f);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            if (logo != null) logo.anchoredPosition = logoStart + new Vector2(0f, Mathf.Sin(t * 1.6f) * 10f);
            if (tapText != null)
            {
                var c = tapText.color;
                c.a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 4f));
                tapText.color = c;
            }
            for (int i = 0; i < sparklePhase.Length; i++)
            {
                var s = sparkles[i];
                if (s == null) continue;
                float k = Mathf.Max(0f, Mathf.Sin(t * 2.2f + sparklePhase[i]));
                s.localScale = Vector3.one * (0.3f + k);
                s.localRotation = Quaternion.Euler(0f, 0f, 45f + t * 40f);
                var img = s.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.95f, 0.6f, k);
            }
        }

        /// <summary>Called by the full-screen tap button.</summary>
        public void StartGame()
        {
            if (leaving) return;
            leaving = true;
            SceneFader.Load(nextScene);
        }
    }
}
