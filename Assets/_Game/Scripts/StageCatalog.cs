using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>The ordered list of stages. Stage N+1 unlocks when stage N is cleared.</summary>
    [CreateAssetMenu(menuName = "Lane Lords/Stage Catalog", fileName = "StageCatalog")]
    public class StageCatalog : ScriptableObject
    {
        public List<StageData> stages = new List<StageData>();
    }

    /// <summary>Carries the chosen stage from the stage select screen into the battle scene.</summary>
    public static class GameSession
    {
        /// <summary>-1 = none chosen (e.g. pressing Play directly in the Battle scene).</summary>
        public static int SelectedStageIndex = -1;
        /// <summary>Set by the result screen's SHOP button: the stage select screen opens the shop right away.</summary>
        public static bool OpenShopOnLoad;
        public static bool OpenSettingsOnLoad;
    }

    /// <summary>Saves the best star count per stage in PlayerPrefs.</summary>
    public static class SaveManager
    {
        static string Key(string stageName) => "stars_" + stageName;

        // ---- Gems (upgrade currency) ----
        /// <summary>Editor-only test cheat (Tools > Lane Lords > Test: Infinite Gems). Always off in real builds.</summary>
        public static bool InfiniteGems
        {
#if UNITY_EDITOR
            get => PlayerPrefs.GetInt("test_infinite_gems", 0) == 1;
#else
            get => false;
#endif
        }

        public static int Gems => InfiniteGems ? 99999 : PlayerPrefs.GetInt("gems", 0);

        public static void AddGems(int amount)
        {
            if (InfiniteGems) return;
            PlayerPrefs.SetInt("gems", Mathf.Max(0, Gems + amount));
            PlayerPrefs.Save();
        }

        public static bool SpendGems(int amount)
        {
            if (Gems < amount) return false;
            AddGems(-amount);
            return true;
        }

        public static int GetStars(string stageName) => PlayerPrefs.GetInt(Key(stageName), 0);

        /// <summary>Test panel only (overwrites, unlike SaveStars which keeps the best).</summary>
        public static void SetStarsForTest(string stageName, int stars) => PlayerPrefs.SetInt(Key(stageName), stars);

        /// <summary>Keeps the best result only.</summary>
        public static void SaveStars(string stageName, int stars)
        {
            if (stars <= GetStars(stageName)) return;
            PlayerPrefs.SetInt(Key(stageName), stars);
            PlayerPrefs.Save();
        }

        public static bool IsUnlocked(StageCatalog catalog, int index)
        {
            if (catalog == null || index <= 0) return true;
            if (index >= catalog.stages.Count) return false;
            StageData previous = catalog.stages[index - 1];
            return previous != null && GetStars(previous.stageName) > 0;
        }
    }
}
