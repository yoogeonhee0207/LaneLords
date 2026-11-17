using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Per-unit progression bought in the shop: a one-time unlock, then levels that raise that unit's HP and attack.
    /// Pawn and Warrior are unlocked from the start.
    /// </summary>
    public enum BuyResult { NotAllowed, NotEnoughGems, Unlocked, Success, Failed }

    public static class UnitProgress
    {
        public const int MaxLevel = 50;

        /// <summary>
        /// Level cap by progress: chapter 1 = Lv.15, after clearing 1-6 = Lv.25, after 2-6 = Lv.35, after 3-6 = Lv.50.
        /// </summary>
        public static int LevelCap()
        {
            if (SaveManager.GetStars("3-6") > 0) return 50;
            if (SaveManager.GetStars("2-6") > 0) return 35;
            if (SaveManager.GetStars("1-6") > 0) return 25;
            return 15;
        }

        /// <summary>The chapter whose boss raises the cap next (2, 3 or 4), or 0 when the cap is already 50.</summary>
        public static int NextCapChapter()
        {
            int cap = LevelCap();
            return cap >= 50 ? 0 : cap >= 35 ? 4 : cap >= 25 ? 3 : 2;
        }

        public static bool AtCap(UnitType t) => Level(t) >= LevelCap() && !IsMaxed(t);

        /// <summary>Old saves had 10 levels: Lv.N becomes Lv.5N (same strength).</summary>
        public static void MigrateTo50Levels()
        {
            if (PlayerPrefs.GetInt("unit_levels_x5", 0) == 1) return;
            foreach (var t in ShopUnits)
                PlayerPrefs.SetInt(LevelKey(t), Mathf.Min(MaxLevel, PlayerPrefs.GetInt(LevelKey(t), 0) * 5));
            PlayerPrefs.SetInt("unit_levels_x5", 1);
            PlayerPrefs.Save();
        }

        /// <summary>Units shown in the shop, in order.</summary>
        public static readonly UnitType[] ShopUnits = { UnitType.Warrior, UnitType.Archer, UnitType.Lancer, UnitType.Monk };

        static string UnlockKey(UnitType t) => "unit_unlocked_" + t;
        static string LevelKey(UnitType t) => "unit_level_" + t;
        static string PityKey(UnitType t) => "unit_pity_" + t;

        public static bool IsUnlocked(UnitType t)
        {
            if (t == UnitType.Pawn || t == UnitType.Warrior) return true;
            return PlayerPrefs.GetInt(UnlockKey(t), 0) == 1;
        }

        public static int UnlockCost(UnitType t)
        {
            switch (t)
            {
                case UnitType.Archer: return 60;
                case UnitType.Lancer: return 120;
                case UnitType.Monk: return 200;
                default: return 0;
            }
        }

        /// <summary>Stage that must be cleared before the unit can be bought (null = no requirement).</summary>
        public static string RequiredStage(UnitType t)
        {
            switch (t)
            {
                case UnitType.Archer: return "1-2";
                case UnitType.Lancer: return "1-6";
                case UnitType.Monk: return "2-3";
                default: return null;
            }
        }

        /// <summary>Units that become buyable when this stage is cleared for the first time.</summary>
        public static System.Collections.Generic.List<UnitType> UnlockedBy(string stageName)
        {
            var list = new System.Collections.Generic.List<UnitType>();
            foreach (var t in ShopUnits)
                if (!IsUnlocked(t) && RequiredStage(t) == stageName) list.Add(t);
            return list;
        }

        /// <summary>True when a unit can be unlocked in the shop but has not been bought yet (drives the "!" badge).</summary>
        public static bool AnyNewUnlockAvailable()
        {
            foreach (var t in ShopUnits)
                if (!IsUnlocked(t) && RequirementMet(t)) return true;
            return false;
        }

        public static bool RequirementMet(UnitType t)
        {
            string stage = RequiredStage(t);
            return string.IsNullOrEmpty(stage) || SaveManager.GetStars(stage) > 0;
        }

        /// <summary>Can the shop button be pressed right now (ignoring gems)?</summary>
        public static bool CanBuyNow(UnitType t)
        {
            if (!IsUnlocked(t)) return RequirementMet(t);
            return !IsMaxed(t) && Level(t) < LevelCap();
        }

        public static int Level(UnitType t)
        {
            MigrateTo50Levels();
            return PlayerPrefs.GetInt(LevelKey(t), 0);
        }

        /// <summary>Test panel only: set unlock state and level directly.</summary>
        public static void SetForTest(UnitType t, bool unlocked, int level)
        {
            MigrateTo50Levels();
            PlayerPrefs.SetInt(UnlockKey(t), unlocked ? 1 : 0);
            PlayerPrefs.SetInt(LevelKey(t), Mathf.Clamp(level, 0, MaxLevel));
            PlayerPrefs.SetInt(PityKey(t), 0);
        }
        public static bool IsMaxed(UnitType t) => Level(t) >= MaxLevel;
        /// <summary>Pricier units cost more to level up (roughly in line with their gold price in battle).</summary>
        public static float CostMultiplier(UnitType t)
        {
            switch (t)
            {
                case UnitType.Archer: return 1.3f;  // 45 gold
                case UnitType.Lancer: return 1.5f;  // 50 gold
                case UnitType.Monk: return 2.0f;    // 70 gold
                default: return 1.0f;               // Warrior, 30 gold
            }
        }

        /// <summary>Small steps: Warrior 3 gems at Lv.0 up to 15 at Lv.49 (about 750 gems in total with failures).</summary>
        public static int LevelCost(UnitType t) => Mathf.Max(1, Mathf.RoundToInt((3f + 0.25f * Level(t)) * CostMultiplier(t)));

        /// <summary>Base success chance of going from the current level to the next.</summary>
        public static float BaseChance(int currentLevel)
        {
            if (currentLevel < 15) return 1.0f; // to Lv.1-15
            if (currentLevel < 25) return 0.8f; // to Lv.16-25
            if (currentLevel < 35) return 0.6f; // to Lv.26-35
            return 0.4f;                        // to Lv.36-50
        }

        /// <summary>Failures in a row at the current level; each adds +10% to the next try.</summary>
        public static int Pity(UnitType t) => PlayerPrefs.GetInt(PityKey(t), 0);

        /// <summary>Chance shown to the player and used for the next level-up (0..1).</summary>
        public static float SuccessChance(UnitType t) => Mathf.Min(1f, BaseChance(Level(t)) + 0.1f * Pity(t));

        /// <summary>What the shop button costs right now (unlock or next level).</summary>
        public static int NextCost(UnitType t) => IsUnlocked(t) ? LevelCost(t) : UnlockCost(t);

        /// <summary>Unlocks the unit, or tries to level it up (may fail; gems are spent either way, the level is kept).</summary>
        public static BuyResult TryBuy(UnitType t)
        {
            if (!IsUnlocked(t))
            {
                if (!RequirementMet(t)) return BuyResult.NotAllowed;
                if (!SaveManager.SpendGems(UnlockCost(t))) return BuyResult.NotEnoughGems;
                PlayerPrefs.SetInt(UnlockKey(t), 1);
                PlayerPrefs.Save();
                return BuyResult.Unlocked;
            }
            if (IsMaxed(t) || Level(t) >= LevelCap()) return BuyResult.NotAllowed;

            float chance = SuccessChance(t);
            if (!SaveManager.SpendGems(LevelCost(t))) return BuyResult.NotEnoughGems;

            if (Random.value < chance)
            {
                PlayerPrefs.SetInt(LevelKey(t), Level(t) + 1);
                PlayerPrefs.SetInt(PityKey(t), 0);
                PlayerPrefs.Save();
                return BuyResult.Success;
            }
            PlayerPrefs.SetInt(PityKey(t), Pity(t) + 1);
            PlayerPrefs.Save();
            return BuyResult.Failed;
        }

        /// <summary>+2% HP and attack (and heal) per level: Lv.50 = x2.</summary>
        public static float StatMult(UnitType t) => 1f + 0.02f * Level(t);

        public static string Name(UnitType t)
        {
            switch (t)
            {
                case UnitType.Warrior: return Loc.T("Warrior", "전사");
                case UnitType.Archer: return Loc.T("Archer", "궁수");
                case UnitType.Lancer: return Loc.T("Lancer", "창병");
                case UnitType.Monk: return Loc.T("Monk", "수도사");
                default: return Loc.T("Pawn", "일꾼");
            }
        }
    }
}
