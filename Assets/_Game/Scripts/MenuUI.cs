using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Saved player settings.</summary>
    public static class GameSettings
    {
        public static void Apply()
        {
            AudioListener.volume = 1f; // music and effects have their own volume settings
        }

        static readonly float[] Levels = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        static float Next(float v)
        {
            for (int i = 0; i < Levels.Length; i++)
                if (v < Levels[i] - 0.01f) return Levels[i];
            return 0f;
        }

        /// <summary>Music volume: 0 -> 25 -> 50 -> 75 -> 100 -> 0 %.</summary>
        public static void CycleMusic() { MusicPlayer.Volume = Next(MusicPlayer.Volume); PlayerPrefs.Save(); }
        public static void CycleSfx() { Sfx.Volume = Next(Sfx.Volume); PlayerPrefs.Save(); Sfx.Play("gem"); } // short preview of the new volume

        static string Percent(float v) => v <= 0.001f ? Loc.T("OFF", "끔") : Mathf.RoundToInt(v * 100f) + "%";
        public static string MusicLabel => Loc.T("Music: ", "음악: ") + Percent(MusicPlayer.Volume);
        public static string SfxLabel => Loc.T("Effects: ", "효과음: ") + Percent(Sfx.Volume);
    }

    /// <summary>Top-right menu on the stage select screen: Shop, Settings.</summary>
    public class MenuUI : MonoBehaviour
    {
        public GameObject menuPanel;
        public GameObject settingsPanel;
        public UpgradeShopUI shop;
        public Text musicLabel;
        public Text sfxLabel;
        public Text resetLabel;
        public Text languageLabel;
        [Tooltip("Red ! shown on the MENU button / SHOP entry when a new unit can be unlocked")]
        public GameObject[] shopBadges;
        [Tooltip("Red ! on the ACHIEVEMENTS entry when a reward can be claimed")]
        public GameObject achievementBadge;

        bool resetArmed;

        System.Collections.IEnumerator Start()
        {
            GameSettings.Apply();
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            RefreshLabels();
            // Wait one frame: UpgradeShopUI.Start hides the shop panel, and Start order between scripts is not fixed.
            yield return null;
            if (GameSession.OpenSettingsOnLoad)
            {
                GameSession.OpenSettingsOnLoad = false;
                OpenSettings();
            }
            if (GameSession.OpenShopOnLoad)
            {
                GameSession.OpenShopOnLoad = false;
                OpenShop();
            }
            else DailyReward.ShowIfAvailable();
        }

        float badgeTimer;

        void Update()
        {
            badgeTimer -= Time.unscaledDeltaTime;
            if (badgeTimer > 0f || shopBadges == null) return;
            badgeTimer = 0.5f;
            bool shopNew = UnitProgress.AnyNewUnlockAvailable();
            bool achNew = Achievements.AnyClaimable;
            // shopBadges[0] sits on the MENU button: it lights up for either reason
            for (int i = 0; i < shopBadges.Length; i++)
            {
                bool show = i == 0 ? shopNew || achNew : shopNew;
                if (shopBadges[i] != null && shopBadges[i].activeSelf != show) shopBadges[i].SetActive(show);
            }
            if (achievementBadge != null && achievementBadge.activeSelf != achNew) achievementBadge.SetActive(achNew);
        }

        public void ToggleMenu()
        {
            if (menuPanel != null) menuPanel.SetActive(!menuPanel.activeSelf);
        }

        public void OpenCodex()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            Codex.Open();
        }

        public void OpenAchievements()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            Achievements.Open();
        }

        public void OpenDaily()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            DailyReward.Open();
        }

        public void OpenCredits()
        {
            CreditsUI.Open();
        }

        public void OpenShop()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (shop != null) shop.Open();
        }

        public void OpenSettings()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            resetArmed = false;
            RefreshLabels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            resetArmed = false;
            if (settingsPanel != null) settingsPanel.SetActive(false);
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

        /// <summary>Auto -> English -> Korean. Reloads the scene so every label switches.</summary>
        public void CycleLanguage()
        {
            Loc.Setting = (Loc.Setting + 1) % 3;
            GameSession.OpenSettingsOnLoad = true;
            SceneFader.Reload();
        }

        /// <summary>First press arms, second press wipes all saved progress.</summary>
        public void ResetProgress()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                RefreshLabels();
                return;
            }
            float music = MusicPlayer.Volume, sfx = Sfx.Volume;
            PlayerPrefs.DeleteAll(); // also brings back the story and the 1-1 tutorial
            MusicPlayer.Volume = music; // keep the volume settings
            Sfx.Volume = sfx;
            SceneFader.Reload();
        }

        void RefreshLabels()
        {
            if (musicLabel != null) musicLabel.text = GameSettings.MusicLabel;
            if (sfxLabel != null) sfxLabel.text = GameSettings.SfxLabel;
            if (resetLabel != null) resetLabel.text = resetArmed ? Loc.T("Tap again to reset!", "한 번 더 누르면 초기화!") : Loc.T("Reset progress", "진행 초기화");
            if (languageLabel != null) languageLabel.text = Loc.SettingLabel();
        }
    }
}
