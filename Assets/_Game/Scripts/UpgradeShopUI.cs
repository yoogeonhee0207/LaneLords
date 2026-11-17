using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Shop on the stage select screen.
    /// Left column: units (unlock once, then level up). Right column: general upgrades.
    /// </summary>
    public class UpgradeShopUI : MonoBehaviour
    {
        public Text gemsText;
        public GameObject panel;
        [Tooltip("Shows the result of the last purchase (success / failed)")]
        public Text feedbackText;

        [Header("Units (order of UnitProgress.ShopUnits)")]
        public Text[] unitNameTexts;
        public Text[] unitEffectTexts;
        public Text[] unitCostTexts;
        public Button[] unitButtons;

        [Header("General upgrades (order of Upgrades.All)")]
        public Text[] nameTexts;
        public Text[] effectTexts;
        public Text[] costTexts;
        public Button[] buyButtons;

        void Start()
        {
            Upgrades.MigrateOldUnitUpgrades();
            if (panel != null) panel.SetActive(false);
            Refresh();
        }

        void Update()
        {
            // In battle the shop belongs to the pause menu: if the game resumes (e.g. Esc), close it.
            var bm = BattleManager.Instance;
            if (bm != null && panel != null && panel.activeSelf && !bm.IsPaused) Close();
        }

        public void Open()
        {
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling(); // above the pause menu in battle
            }
            if (feedbackText != null) feedbackText.text = "";
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>Unit button: unlock or level up.</summary>
        public void BuyUnit(int index)
        {
            if (index < 0 || index >= UnitProgress.ShopUnits.Length) return;
            UnitType t = UnitProgress.ShopUnits[index];
            BuyResult result = UnitProgress.TryBuy(t);
            Sfx.Play(result == BuyResult.Unlocked || result == BuyResult.Success ? "ui_buy" : result == BuyResult.Failed ? "ui_fail" : "ui_denied");
            if (feedbackText != null)
            {
                string name = UnitProgress.Name(t);
                switch (result)
                {
                    case BuyResult.Unlocked:
                        feedbackText.text = Loc.T($"{name} unlocked!", $"{name} 해금!");
                        feedbackText.color = new Color32(40, 110, 60, 255);
                        break;
                    case BuyResult.Success:
                        feedbackText.text = Loc.T($"SUCCESS! {name} is now Lv.{UnitProgress.Level(t)}", $"성공! {name} Lv.{UnitProgress.Level(t)}");
                        feedbackText.color = new Color32(40, 110, 60, 255);
                        break;
                    case BuyResult.Failed:
                        feedbackText.text = Loc.T($"Failed... next try {Mathf.RoundToInt(UnitProgress.SuccessChance(t) * 100)}%", $"실패... 다음 확률 {Mathf.RoundToInt(UnitProgress.SuccessChance(t) * 100)}%");
                        feedbackText.color = new Color32(170, 50, 40, 255);
                        break;
                }
            }
            if (BattleManager.Instance != null) BattleManager.Instance.ApplyUpgradesLive();
            Refresh();
        }

        /// <summary>General upgrade button.</summary>
        public void Buy(int index)
        {
            if (index < 0 || index >= Upgrades.All.Length) return;
            Sfx.Play(Upgrades.TryBuy(Upgrades.All[index]) ? "ui_buy" : "ui_denied");
            if (BattleManager.Instance != null) BattleManager.Instance.ApplyUpgradesLive();
            Refresh();
        }

        public void Refresh()
        {
            int gems = SaveManager.Gems;
            if (gemsText != null) gemsText.text = Loc.T($"Gems {gems}", $"보석 {gems}");

            for (int i = 0; i < UnitProgress.ShopUnits.Length; i++)
            {
                UnitType t = UnitProgress.ShopUnits[i];
                bool unlocked = UnitProgress.IsUnlocked(t);
                bool maxed = unlocked && UnitProgress.IsMaxed(t);
                if (i < unitNameTexts.Length)
                {
                    if (!unlocked) unitNameTexts[i].text = $"{UnitProgress.Name(t)}  " + Loc.T("(locked)", "(잠김)");
                    else if (maxed) unitNameTexts[i].text = $"{UnitProgress.Name(t)}  Lv.{UnitProgress.Level(t)}";
                    else if (UnitProgress.AtCap(t)) unitNameTexts[i].text = $"{UnitProgress.Name(t)}  Lv.{UnitProgress.Level(t)}/{UnitProgress.LevelCap()}";
                    else unitNameTexts[i].text = $"{UnitProgress.Name(t)}  Lv.{UnitProgress.Level(t)}/{UnitProgress.LevelCap()}  ({Mathf.RoundToInt(UnitProgress.SuccessChance(t) * 100)}%)";
                }
                if (i < unitEffectTexts.Length)
                    unitEffectTexts[i].text = unlocked ? $"+{UnitProgress.Level(t) * 2}%" : "-";
                bool requirementMet = UnitProgress.RequirementMet(t);
                if (i < unitCostTexts.Length)
                {
                    if (maxed) unitCostTexts[i].text = Loc.T("MAX", "최대");
                    else if (unlocked && UnitProgress.AtCap(t))
                    {
                        int ch = UnitProgress.NextCapChapter();
                        unitCostTexts[i].text = Loc.T($"Ch.{ch} opens", $"{ch}장에서 해제");
                    }
                    else if (unlocked) unitCostTexts[i].text = Loc.T($"{UnitProgress.LevelCost(t)} Gems", $"보석 {UnitProgress.LevelCost(t)}");
                    else if (!requirementMet) unitCostTexts[i].text = Loc.T($"Clear {UnitProgress.RequiredStage(t)}", $"{UnitProgress.RequiredStage(t)} 클리어");
                    else unitCostTexts[i].text = Loc.T($"Unlock {UnitProgress.UnlockCost(t)}", $"해금 {UnitProgress.UnlockCost(t)}");
                }
                if (i < unitButtons.Length)
                    unitButtons[i].interactable = UnitProgress.CanBuyNow(t) && gems >= UnitProgress.NextCost(t);
            }

            for (int i = 0; i < Upgrades.All.Length; i++)
            {
                UpgradeType t = Upgrades.All[i];
                bool maxed = Upgrades.IsMaxed(t);
                if (i < nameTexts.Length) nameTexts[i].text = $"{Upgrades.Name(t)}  Lv.{Upgrades.Level(t)}";
                if (i < effectTexts.Length) effectTexts[i].text = Upgrades.Effect(t);
                if (i < costTexts.Length) costTexts[i].text = maxed ? Loc.T("MAX", "최대") : Loc.T($"{Upgrades.Cost(t)} Gems", $"보석 {Upgrades.Cost(t)}");
                if (i < buyButtons.Length) buyButtons[i].interactable = !maxed && gems >= Upgrades.Cost(t);
            }
        }
    }
}
