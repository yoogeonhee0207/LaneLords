using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Stage select screen: one button per stage, showing stars and lock state.</summary>
    public class StageSelectUI : MonoBehaviour
    {
        public StageCatalog catalog;
        public Button[] stageButtons;
        public Text[] stageLabels;
        public Text[] starLabels;
        public Sprite unlockedSprite;
        public Sprite lockedSprite;
        public string battleSceneName = "Battle";

        [Header("Chapters (one page = stageButtons.Length stages)")]
        public Text chapterTitle;
        public Button prevButton;
        public Button nextButton;

        int page;
        int PerPage => Mathf.Max(1, stageButtons.Length);
        int PageCount => catalog == null ? 1 : Mathf.Max(1, (catalog.stages.Count + PerPage - 1) / PerPage);

        void Start()
        {
            Time.timeScale = 1f;
            MusicPlayer.Play("bgm_menu");
            // Open on the chapter of the furthest unlocked stage.
            int furthest = 0;
            if (catalog != null)
                for (int i = 0; i < catalog.stages.Count; i++)
                    if (SaveManager.IsUnlocked(catalog, i)) furthest = i;
            page = furthest / PerPage;
            Refresh();
        }

        public void NextPage() { page = Mathf.Min(page + 1, PageCount - 1); Refresh(); }
        public void PrevPage() { page = Mathf.Max(page - 1, 0); Refresh(); }

        void Refresh()
        {
            if (chapterTitle != null) chapterTitle.text = Loc.Korean ? $"{page + 1}장 {StoryScript.ChapterName(page + 1)}" : $"{page + 1}. {StoryScript.ChapterName(page + 1)}";
            if (prevButton != null) prevButton.gameObject.SetActive(page > 0);
            if (nextButton != null) nextButton.gameObject.SetActive(page < PageCount - 1);

            for (int slot = 0; slot < stageButtons.Length; slot++)
            {
                int i = page * PerPage + slot;
                bool exists = catalog != null && i < catalog.stages.Count && catalog.stages[i] != null;
                stageButtons[slot].gameObject.SetActive(exists);
                if (!exists) continue;
                bool unlocked = SaveManager.IsUnlocked(catalog, i);
                stageButtons[slot].interactable = unlocked;

                var img = stageButtons[slot].GetComponent<Image>();
                if (img != null)
                {
                    Sprite s = unlocked ? unlockedSprite : lockedSprite;
                    if (s != null) img.sprite = s;
                }

                if (slot < stageLabels.Length) stageLabels[slot].text = catalog.stages[i].stageName;
                if (slot < starLabels.Length)
                {
                    if (!unlocked) starLabels[slot].text = Loc.T("LOCKED", "잠김");
                    else
                    {
                        int stars = SaveManager.GetStars(catalog.stages[i].stageName);
                        starLabels[slot].text = stars > 0 ? Loc.T($"Stars {stars}/3", $"별 {stars}/3") : Loc.T("NEW", "새로");
                    }
                }
            }
        }

        /// <summary>Called by the stage buttons.</summary>
        public void Play(int slot)
        {
            int index = page * PerPage + slot;
            if (catalog == null || !SaveManager.IsUnlocked(catalog, index)) return;
            GameSession.SelectedStageIndex = index;
            SceneFader.Load(battleSceneName);
        }

        /// <summary>Debug helper: wipe saved stars.</summary>
        public void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            SceneFader.Reload();
        }
    }
}
