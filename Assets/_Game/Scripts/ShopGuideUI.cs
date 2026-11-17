using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Stage select screen: the first time a new unit can be unlocked, walks the player through
    /// MENU -> SHOP -> the unit's unlock button. Runs once (PlayerPrefs "guide_shop").
    /// </summary>
    public class ShopGuideUI : MonoBehaviour
    {
        public const string DoneKey = "guide_shop";

        public RectTransform menuButton;
        public RectTransform shopEntry;
        public GameObject menuPanel;
        public UpgradeShopUI shop;
        public GameObject bubble;
        public Text bubbleText;
        public RectTransform hand;

        enum Step { Off, Menu, ShopEntry, Unlock, Done }
        Step step = Step.Off;
        UnitType unit;
        float doneTimer;
        RectTransform target;
        float handBaseY;
        float bobSign = 1f;

        void Start()
        {
            Hide();
            if (PlayerPrefs.GetInt(DoneKey, 0) == 1) return;
            foreach (var t in UnitProgress.ShopUnits)
            {
                if (!UnitProgress.IsUnlocked(t) && UnitProgress.RequirementMet(t))
                {
                    unit = t;
                    step = Step.Menu;
                    return;
                }
            }
        }

        void Update()
        {
            if (step == Step.Off) return;

            bool shopOpen = shop != null && shop.panel != null && shop.panel.activeSelf;
            bool menuOpen = menuPanel != null && menuPanel.activeSelf;

            if (step == Step.Done)
            {
                doneTimer -= Time.unscaledDeltaTime;
                if (doneTimer <= 0f || !shopOpen)
                {
                    Hide();
                    step = Step.Off;
                }
                Bob();
                return;
            }

            if (UnitProgress.IsUnlocked(unit))
            {
                PlayerPrefs.SetInt(DoneKey, 1);
                PlayerPrefs.Save();
                step = Step.Done;
                doneTimer = 4f;
                Point(null);
                Say(Loc.T($"Unlocked! The {UnitProgress.Name(unit)} now joins your battles. Spend more gems to level units up.",
                          $"해금 완료! 이제 전투에서 {UnitProgress.Name(unit)}를 뽑을 수 있어요. 보석으로 유닛을 강화할 수도 있어요."));
                return;
            }

            if (shopOpen)
            {
                step = Step.Unlock;
                int cost = UnitProgress.UnlockCost(unit);
                Point(UnlockButton());
                if (SaveManager.Gems >= cost)
                    Say(Loc.T($"Tap here to unlock the {UnitProgress.Name(unit)} for {cost} gems!",
                              $"여기를 눌러 보석 {cost}개로 {UnitProgress.Name(unit)}를 해금하세요!"));
                else
                    Say(Loc.T($"You need {cost} gems. Replay cleared stages to earn more, then come back!",
                              $"보석 {cost}개가 필요해요. 깬 스테이지를 다시 하면 보석을 모을 수 있어요!"));
            }
            else if (menuOpen)
            {
                step = Step.ShopEntry;
                Point(shopEntry);
                Say(Loc.T("Open the SHOP.", "상점을 눌러요."));
            }
            else
            {
                step = Step.Menu;
                Point(menuButton);
                Say(Loc.T($"A new unit is ready: {UnitProgress.Name(unit)}! Tap MENU.",
                          $"새 유닛 {UnitProgress.Name(unit)}를 해금할 수 있어요! 메뉴를 눌러 보세요."));
            }
            Bob();
        }

        RectTransform UnlockButton()
        {
            if (shop == null || shop.unitButtons == null) return null;
            int i = System.Array.IndexOf(UnitProgress.ShopUnits, unit);
            return i >= 0 && i < shop.unitButtons.Length && shop.unitButtons[i] != null ? (RectTransform)shop.unitButtons[i].transform : null;
        }

        void Say(string text)
        {
            if (bubble == null) return;
            if (!bubble.activeSelf) bubble.SetActive(true);
            bubble.transform.SetAsLastSibling();
            if (bubbleText != null && bubbleText.text != text) bubbleText.text = text;
        }

        void Point(RectTransform t)
        {
            if (hand == null || t == target) return;
            target = t;
            hand.gameObject.SetActive(t != null);
            if (t == null) return;
            hand.SetParent(t, false);
            hand.SetAsLastSibling();
            // Buttons near the top of the screen get the hand below them, pointing up.
            bool below = t.position.y > Screen.height * 0.6f;
            bobSign = below ? -1f : 1f;
            hand.localRotation = Quaternion.Euler(0f, 0f, below ? 0f : 180f);
            handBaseY = bobSign * (t.rect.height * 0.5f + 60f);
            hand.anchoredPosition = new Vector2(0f, handBaseY);
        }

        void Bob()
        {
            if (hand != null && target != null)
                hand.anchoredPosition = new Vector2(0f, handBaseY + bobSign * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 16f);
        }

        void Hide()
        {
            if (bubble != null) bubble.SetActive(false);
            target = null;
            if (hand != null) hand.gameObject.SetActive(false);
        }
    }
}
