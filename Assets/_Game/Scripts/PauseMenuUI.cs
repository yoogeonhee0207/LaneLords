using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Top-right MENU in battle: pauses the game and offers Resume / Restart / Stages / Sound.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject panel;
        public Text musicLabel;
        public Text sfxLabel;

        float speedBeforePause = 1f;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
            RefreshLabels();
        }

        void Update()
        {
            // Esc toggles the menu (PC / web)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (panel != null && panel.activeSelf) Resume();
                else Open();
            }
        }

        public void Open()
        {
            var bm = BattleManager.Instance;
            if (bm == null || !bm.IsRunning || bm.IsPaused) return;
            speedBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            bm.IsPaused = true;
            RefreshLabels();
            Sfx.Play("ui_pause");
            if (panel != null) panel.SetActive(true);
        }

        public void Resume()
        {
            var bm = BattleManager.Instance;
            if (bm != null) bm.IsPaused = false;
            Time.timeScale = speedBeforePause;
            if (panel != null && panel.activeSelf) Sfx.Play("ui_unpause");
            if (panel != null) panel.SetActive(false);
        }

        public void Restart()
        {
            var bm = BattleManager.Instance;
            if (bm != null) { bm.IsPaused = false; bm.Restart(); }
        }

        public void BackToStages()
        {
            var bm = BattleManager.Instance;
            if (bm != null) { bm.IsPaused = false; bm.BackToStageSelect(); }
        }

        public void CycleMusic()
        {
            GameSettings.CycleMusic();
            RefreshLabels();
        }

        public void CycleSfx()
        {
            GameSettings.CycleSfx();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (musicLabel != null) musicLabel.text = GameSettings.MusicLabel;
            if (sfxLabel != null) sfxLabel.text = GameSettings.SfxLabel;
        }
    }
}
