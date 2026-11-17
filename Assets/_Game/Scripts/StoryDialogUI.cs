using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Story dialogue before a stage (first visit) and after the first clear of a boss stage.
    /// Big pixel portraits on the left (allies) or right (enemies), a name ribbon and typewriter text.
    /// The battle is paused while it is open. Tap / click / Space = next line, SKIP = close.
    /// </summary>
    public class StoryDialogUI : MonoBehaviour
    {
        public static StoryDialogUI Instance { get; private set; }

        public GameObject panel;
        public Image leftPortrait;
        public Image rightPortrait;
        public Image nameRibbon;
        public Sprite allyRibbon;
        public Sprite enemyRibbon;
        public Text nameText;
        public Text bodyText;
        public Text nextHint;
        public Text skipLabel;

        [Header("Banner (chapter title / BOSS / THE END)")]
        public GameObject banner;
        public Image bannerRibbon;
        public Text bannerTitle;
        public Text bannerSub;

        [Header("Portraits (same order)")]
        public string[] speakerKeys;
        public Sprite[] speakerSprites;

        [Header("Full-screen pictures (chapter title cards, ending)")]
        public Image backdrop;
        public Sprite[] chapterBackdrops;
        public Sprite endingBackdrop;
        Coroutine backdropFade;

        public bool IsShowing { get; private set; }

        StoryLine[] lines;
        int index;
        bool typing;
        string fullText;
        float speedBefore = 1f;
        bool pausedBattle;
        Vector2 leftBase, rightBase;
        const float CharsPerSecond = 40f;

        void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
            if (leftPortrait != null) leftBase = leftPortrait.rectTransform.anchoredPosition;
            if (rightPortrait != null) rightBase = rightPortrait.rectTransform.anchoredPosition;
        }

        IEnumerator Start()
        {
            if (banner != null) banner.SetActive(false);
            yield return null; // let BattleManager.Start run first
            var bm = BattleManager.Instance;
            if (bm == null || bm.stage == null) yield break;
            string name = bm.stage.stageName;
            StoryLine[] intro;
            string key = "story_in_" + name;
            if (PlayerPrefs.GetInt(key, 0) != 0 || !StoryScript.Intro.TryGetValue(name, out intro)) yield break;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            // First stage of a chapter: big title card first.
            if (name.EndsWith("-1"))
            {
                int chapter = CastleTiers.TierForStage(bm.stage) + 1;
                PauseBattle();
                Sprite pic = chapterBackdrops != null && chapter - 1 < chapterBackdrops.Length ? chapterBackdrops[chapter - 1] : null;
                if (pic != null) yield return FadeBackdrop(pic, true);
                yield return ShowBanner(StoryScript.ChapterLabel(chapter), StoryScript.ChapterName(chapter), 2.2f, true);
                if (pic != null) yield return FadeBackdrop(pic, false);
            }
            Play(intro, true);
        }

        // ---------- pausing ----------
        void PauseBattle()
        {
            var bm = BattleManager.Instance;
            if (bm != null) bm.IsPaused = true;
            if (Time.timeScale > 0f) speedBefore = Time.timeScale;
            Time.timeScale = 0f;
        }

        void ResumeBattle()
        {
            var bm = BattleManager.Instance;
            if (bm != null) bm.IsPaused = false;
            Time.timeScale = speedBefore > 0f ? speedBefore : 1f;
        }

        /// <summary>Big ribbon in the middle of the screen (unscaled time, works while paused).</summary>
        public IEnumerator ShowBanner(string title, string sub, float seconds, bool ally)
        {
            if (banner == null) yield break;
            if (bannerRibbon != null)
            {
                Sprite r = ally ? allyRibbon : enemyRibbon;
                if (r != null) bannerRibbon.sprite = r;
            }
            if (bannerTitle != null) bannerTitle.text = title;
            if (bannerSub != null) bannerSub.text = sub;
            banner.transform.SetAsLastSibling();
            banner.SetActive(true);
            var rt = banner.transform;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                // pop in, hold, shrink away
                float s = t < 0.2f ? Mathf.Lerp(0.3f, 1.1f, t / 0.2f) : t < 0.3f ? Mathf.Lerp(1.1f, 1f, (t - 0.2f) / 0.1f)
                        : t > seconds - 0.2f ? Mathf.Lerp(1f, 0.3f, (t - (seconds - 0.2f)) / 0.2f) : 1f;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            banner.SetActive(false);
        }

        /// <summary>
        /// The boss just walked out: pause, show the camera on it, "BOSS" banner, then its lines
        /// (lines only the first time; the banner every time).
        /// </summary>
        public void BossAppeared(string stageName, Unit boss)
        {
            StartCoroutine(BossRoutine(stageName, boss));
        }

        IEnumerator BossRoutine(string stageName, Unit boss)
        {
            MusicPlayer.Play(stageName == "4-6" ? "bgm_boss_final" : "bgm_boss");
            PauseBattle();
            var pan = Camera.main != null ? Camera.main.GetComponent<CameraPan>() : null;
            if (pan != null && boss != null) pan.LookAt(boss.transform.position.x, 2f);
            string bossName = boss != null && boss.Data != null ? BossName(boss.Data.displayName) : "";
            if (boss != null && boss.Data != null)
            {
                string n = boss.Data.displayName;
                Sfx.Play(n == "Hex Shaman" ? "boss_shaman" : n == "Panda" ? "boss_panda" : n == "Minotaur" ? "boss_mino" : "boss_troll");
            }
            yield return ShowBanner("BOSS!", bossName, 1.6f, false);

            StoryLine[] lines;
            string key = "story_boss_" + stageName;
            if (PlayerPrefs.GetInt(key, 0) == 0 && StoryScript.BossEntry.TryGetValue(stageName, out lines))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                Play(lines, true); // resumes the battle when closed
            }
            else ResumeBattle();
        }

        static string BossName(string unitName)
        {
            switch (unitName)
            {
                case "Hex Shaman": return StoryScript.SpeakerName("shaman");
                case "Panda": return StoryScript.SpeakerName("panda");
                case "Minotaur": return StoryScript.SpeakerName("minotaur");
                case "Troll": return StoryScript.SpeakerName("troll");
            }
            return unitName;
        }

        /// <summary>After the 4-6 outro: ending lines (first clear only). Returns false if already seen.</summary>
        public bool PlayEnding()
        {
            if (PlayerPrefs.GetInt("story_ending", 0) != 0) return false;
            PlayerPrefs.SetInt("story_ending", 1);
            PlayerPrefs.Save();
            MusicPlayer.Play("bgm_ending");
            ShowBackdrop(endingBackdrop);
            Play(StoryScript.Ending, false);
            return true;
        }

        // ---------- full-screen pictures ----------
        /// <summary>Fades a full-screen picture in behind the dialogue and banner (stays until HideBackdrop).</summary>
        public void ShowBackdrop(Sprite pic)
        {
            if (backdrop == null || pic == null) return;
            if (backdropFade != null) StopCoroutine(backdropFade);
            backdropFade = StartCoroutine(FadeBackdrop(pic, true));
        }

        public void HideBackdrop()
        {
            if (backdrop == null || !backdrop.gameObject.activeSelf) return;
            if (backdropFade != null) StopCoroutine(backdropFade);
            backdropFade = StartCoroutine(FadeBackdrop(backdrop.sprite, false));
        }

        IEnumerator FadeBackdrop(Sprite pic, bool show)
        {
            if (backdrop == null) yield break;
            backdrop.sprite = pic;
            backdrop.gameObject.SetActive(true);
            backdrop.transform.SetAsLastSibling();               // above the battle UI...
            if (panel != null) panel.transform.SetAsLastSibling(); // ...but under the dialogue box
            const float time = 0.3f;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float a = show ? t / time : 1f - t / time;
                backdrop.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            backdrop.color = new Color(1f, 1f, 1f, show ? 1f : 0f);
            if (!show) backdrop.gameObject.SetActive(false);
        }

        /// <summary>Plays the outro of a stage once (after its first clear). Returns false if there is nothing to show.</summary>
        public bool PlayOutro(string stageName)
        {
            StoryLine[] outro;
            string key = "story_out_" + stageName;
            if (PlayerPrefs.GetInt(key, 0) != 0 || !StoryScript.Outro.TryGetValue(stageName, out outro)) return false;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Play(outro, false);
            return true;
        }

        public void Play(StoryLine[] story, bool pauseBattle)
        {
            if (story == null || story.Length == 0 || panel == null) return;
            lines = story;
            index = 0;
            IsShowing = true;
            pausedBattle = pauseBattle;
            if (pauseBattle) PauseBattle();
            if (skipLabel != null) skipLabel.text = StoryScript.Korean ? "건너뛰기" : "SKIP";
            panel.SetActive(true);
            ShowLine();
        }

        void Update()
        {
            if (!IsShowing) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) Next();

            // "Talking": the speaker bobs while the text types out; the hint blinks when the line is done.
            float t = Time.unscaledTime;
            float bob = typing ? Mathf.Abs(Mathf.Sin(t * 14f)) * 10f : 0f;
            if (leftPortrait != null) leftPortrait.rectTransform.anchoredPosition = leftBase + new Vector2(0f, bob);
            if (rightPortrait != null) rightPortrait.rectTransform.anchoredPosition = rightBase + new Vector2(0f, bob);
            if (nextHint != null) nextHint.enabled = !typing && Mathf.Repeat(t, 1f) < 0.6f;
        }

        /// <summary>Called by the full-screen tap area.</summary>
        public void Next()
        {
            if (!IsShowing) return;
            if (typing)
            {
                StopAllCoroutines();
                typing = false;
                bodyText.text = fullText;
                return;
            }
            index++;
            if (index >= lines.Length) Close();
            else ShowLine();
        }

        /// <summary>Called by the SKIP button.</summary>
        public void Skip()
        {
            if (IsShowing) Close();
        }

        void ShowLine()
        {
            StoryLine line = lines[index];
            bool ally = StoryScript.IsAlly(line.speaker);
            Sprite portrait = PortraitFor(line.speaker);

            if (leftPortrait != null)
            {
                leftPortrait.gameObject.SetActive(ally && portrait != null);
                if (ally) leftPortrait.sprite = portrait;
            }
            if (rightPortrait != null)
            {
                rightPortrait.gameObject.SetActive(!ally && portrait != null);
                if (!ally) rightPortrait.sprite = portrait;
            }

            if (nameRibbon != null)
            {
                Sprite r = ally ? allyRibbon : enemyRibbon;
                if (r != null) nameRibbon.sprite = r;
                var rt = nameRibbon.rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Abs(rt.anchoredPosition.x) * (ally ? -1f : 1f), rt.anchoredPosition.y);
            }
            if (nameText != null) nameText.text = StoryScript.SpeakerName(line.speaker);

            fullText = StoryScript.Korean ? line.ko : line.en;
            StopAllCoroutines();
            StartCoroutine(TypeOut());
        }

        IEnumerator TypeOut()
        {
            typing = true;
            float shown = 0f;
            bodyText.text = "";
            while (shown < fullText.Length)
            {
                shown += CharsPerSecond * Time.unscaledDeltaTime;
                bodyText.text = fullText.Substring(0, Mathf.Min(fullText.Length, (int)shown));
                yield return null;
            }
            bodyText.text = fullText;
            typing = false;
        }

        Sprite PortraitFor(string speaker)
        {
            if (speakerKeys == null || speakerSprites == null) return null;
            for (int i = 0; i < speakerKeys.Length && i < speakerSprites.Length; i++)
                if (speakerKeys[i] == speaker) return speakerSprites[i];
            return null;
        }

        void Close()
        {
            StopAllCoroutines();
            typing = false;
            IsShowing = false;
            panel.SetActive(false);
            if (pausedBattle) ResumeBattle();
        }
    }
}
