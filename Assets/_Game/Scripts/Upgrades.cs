using UnityEngine;

namespace TinySwordsBattle
{
    public enum UpgradeType { UnitHp, UnitAttack, Income, CastleHp, StartGold, WorkerIncome }

    /// <summary>Permanent upgrades bought with gems. Levels are saved in PlayerPrefs.</summary>
    public static class Upgrades
    {
        public const int MaxLevel = 10;
        /// <summary>General upgrades shown in the shop. (Unit HP/Attack were replaced by per-unit levels in UnitProgress.)</summary>
        public static readonly UpgradeType[] All =
        {
            UpgradeType.Income, UpgradeType.CastleHp, UpgradeType.StartGold, UpgradeType.WorkerIncome
        };

        /// <summary>
        /// One-time refund for the old global "Unit HP" / "Unit Attack" upgrades, which no longer exist.
        /// Gives back every gem spent on them and resets their levels.
        /// </summary>
        public static void MigrateOldUnitUpgrades()
        {
            if (PlayerPrefs.GetInt("migrated_unit_upgrades", 0) == 1) return;
            int refund = 0;
            foreach (var t in new[] { UpgradeType.UnitHp, UpgradeType.UnitAttack })
            {
                int level = Level(t);
                for (int l = 0; l < level; l++) refund += 20 + 15 * l;
                PlayerPrefs.DeleteKey(Key(t));
            }
            PlayerPrefs.SetInt("migrated_unit_upgrades", 1);
            PlayerPrefs.Save();
            if (refund > 0) SaveManager.AddGems(refund);
        }

        static string Key(UpgradeType t) => "upgrade_" + t;

        public static int Level(UpgradeType t) => PlayerPrefs.GetInt(Key(t), 0);

        /// <summary>Test panel only.</summary>
        public static void SetLevelForTest(UpgradeType t, int level) => PlayerPrefs.SetInt(Key(t), Mathf.Clamp(level, 0, MaxLevel));

        /// <summary>Gem cost of the next level.</summary>
        public static int Cost(UpgradeType t) => 20 + 15 * Level(t);

        public static bool IsMaxed(UpgradeType t) => Level(t) >= MaxLevel;

        public static bool TryBuy(UpgradeType t)
        {
            if (IsMaxed(t)) return false;
            if (!SaveManager.SpendGems(Cost(t))) return false;
            PlayerPrefs.SetInt(Key(t), Level(t) + 1);
            PlayerPrefs.Save();
            return true;
        }

        // ---- Effects (used in battle) ----
        public static float IncomeBonus => 0.5f * Level(UpgradeType.Income);
        /// <summary>Base 700 HP -> 5000 HP at Lv.10 (+430 HP per level).</summary>
        public static float CastleHpMult => 1f + (4300f / 700f / MaxLevel) * Level(UpgradeType.CastleHp);
        public static int StartGoldBonus => 15 * Level(UpgradeType.StartGold);
        public static float WorkerIncomeBonus => 0.2f * Level(UpgradeType.WorkerIncome);

        public static string Name(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.UnitHp: return Loc.T("Unit HP", "유닛 체력");
                case UpgradeType.UnitAttack: return Loc.T("Unit Attack", "유닛 공격력");
                case UpgradeType.Income: return Loc.T("Gold Income", "골드 수입");
                case UpgradeType.CastleHp: return Loc.T("Base HP", "성 체력");
                case UpgradeType.StartGold: return Loc.T("Start Gold", "시작 골드");
                default: return Loc.T("Worker Income", "일꾼 수입");
            }
        }

        /// <summary>Current effect text, e.g. "+20%".</summary>
        public static string Effect(UpgradeType t)
        {
            int l = Level(t);
            switch (t)
            {
                case UpgradeType.UnitHp: return $"+{l * 10}%";
                case UpgradeType.UnitAttack: return $"+{l * 10}%";
                case UpgradeType.Income: return $"+{l * 0.5f:0.#}" + Loc.T("/s", "/초");
                case UpgradeType.CastleHp: return $"+{l * 430}";
                case UpgradeType.StartGold: return $"+{l * 15}";
                default: return $"+{l * 0.2f:0.#}" + Loc.T("/s", "/초");
            }
        }
    }
}
