using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// First-time tutorial in stage 1-1 (after the story dialogue):
    /// 1) tap the Worker, 2) tap the Warrior, 3) short tips. Enemies wait until step 2 is done.
    /// Runs once (PlayerPrefs "tutorial_done"); Settings > Reset progress shows it again.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        /// <summary>While true the enemy AI neither earns gold nor spawns.</summary>
        public static bool HoldEnemies { get; private set; }

        public BattleUI battleUI;
        public GameObject bubble;
        public Text bubbleText;
        public RectTransform hand;       // pointing hand, drawn above the target button
        public RectTransform highlight;  // corner frame around the target button

        public const string DoneKey = "tutorial_done";
        RectTransform target;
        float handBaseY;

        void Awake()
        {
            HoldEnemies = false;
            if (bubble != null) bubble.SetActive(false);
            SetPointer(null);
        }

        void OnDestroy()
        {
            HoldEnemies = false;
        }

        IEnumerator Start()
        {
            var bm = BattleManager.Instance;
            if (bm == null || bm.stage == null) yield break;
            if (bm.stage.stageName != "1-1" || PlayerPrefs.GetInt(DoneKey, 0) == 1)
            {
                yield return UnitTips();
                yield break;
            }

            HoldEnemies = true;
            yield return null;
            while (StoryDialogUI.Instance != null && StoryDialogUI.Instance.IsShowing) yield return null;

            // Step 1: worker
            SetPointer(ButtonFor(UnitType.Pawn));
            Say(Loc.T("Gold fills up by itself. Tap the WORKER (25 gold) - workers make gold faster!",
                      "골드는 저절로 차요. 일꾼(25골드)을 눌러 보세요. 일꾼이 많을수록 골드가 빨리 모여요!"));
            while (CountPlayer(UnitType.Pawn) < 1) yield return null;

            // Step 2: warrior
            SetPointer(ButtonFor(UnitType.Warrior));
            Say(Loc.T("Now tap the WARRIOR (30 gold) to stop the goblins!",
                      "이제 전사(30골드)를 눌러 고블린을 막으세요!"));
            while (CountPlayer(UnitType.Warrior) < 1) yield return null;

            // Step 3: tips (enemies start now)
            HoldEnemies = false;
            SetPointer(null);
            Say(Loc.T("Here they come! Keep training soldiers and destroy the enemy base to win.",
                      "적이 몰려와요! 골드가 모이면 병사를 계속 뽑고, 적 성을 무너뜨리면 승리예요."));
            yield return Wait(5f);
            Say(Loc.T("Tip: drag the screen left and right to look around the battlefield.",
                      "팁: 화면을 좌우로 끌면 전장을 둘러볼 수 있어요."));
            yield return Wait(4f);

            if (bubble != null) bubble.SetActive(false);
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>First battle with a newly unlocked unit: points at its button and explains its role (once per unit).</summary>
        IEnumerator UnitTips()
        {
            yield return null;
            while (StoryDialogUI.Instance != null && StoryDialogUI.Instance.IsShowing) yield return null;
            foreach (var t in UnitProgress.ShopUnits)
            {
                string key = "unit_tip_" + t;
                string tip = UnitTip(t);
                if (tip == null || !UnitProgress.IsUnlocked(t) || PlayerPrefs.GetInt(key, 0) == 1) continue;
                var button = ButtonFor(t);
                if (button == null) continue;
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                SetPointer(button);
                Say(tip);
                int before = CountPlayer(t);
                float time = 0f;
                while (time < 8f && CountPlayer(t) <= before && BattleManager.Instance != null && BattleManager.Instance.IsRunning)
                {
                    time += Time.deltaTime;
                    yield return null;
                }
                SetPointer(null);
                if (bubble != null) bubble.SetActive(false);
                yield return Wait(0.5f);
            }
        }

        static string UnitTip(UnitType t)
        {
            switch (t)
            {
                case UnitType.Archer: return Loc.T("New: ARCHER! Shoots from far away. Keep Warriors in front of them.",
                                                   "새 유닛 궁수! 멀리서 화살을 쏴요. 전사를 앞에 세우고 뒤에서 쏘게 하세요.");
                case UnitType.Lancer: return Loc.T("New: LANCER! Lots of HP - holds the front line while Archers shoot.",
                                                   "새 유닛 창병! 체력이 높아 앞에서 적을 막아요. 궁수와 함께 쓰면 좋아요.");
                case UnitType.Monk: return Loc.T("New: MONK! Casts a holy burst: enemies in it get hurt, allies get healed.",
                                                 "새 유닛 수도사! 빛의 기술로 범위 안의 적은 다치고 아군은 회복돼요.");
            }
            return null;
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (BattleManager.Instance == null || !BattleManager.Instance.IsRunning) yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        void Update()
        {
            if (target == null) return;
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 18f;
            if (hand != null) hand.anchoredPosition = new Vector2(0f, handBaseY + bob);
            if (highlight != null)
            {
                float s = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.08f;
                highlight.localScale = new Vector3(s, s, 1f);
            }
        }

        void Say(string text)
        {
            if (bubble == null) return;
            bubble.SetActive(true);
            if (bubbleText != null) bubbleText.text = text;
        }

        /// <summary>Moves the hand and the corner frame onto a unit button (null hides them).</summary>
        void SetPointer(RectTransform button)
        {
            target = button;
            bool show = button != null;
            if (hand != null)
            {
                hand.gameObject.SetActive(show);
                if (show)
                {
                    hand.SetParent(button, false);
                    handBaseY = button.rect.height * 0.5f + 70f;
                    hand.anchoredPosition = new Vector2(0f, handBaseY);
                }
            }
            if (highlight != null)
            {
                highlight.gameObject.SetActive(show);
                if (show)
                {
                    highlight.SetParent(button, false);
                    highlight.anchoredPosition = Vector2.zero;
                    highlight.sizeDelta = button.rect.size + new Vector2(40f, 40f);
                }
            }
        }

        RectTransform ButtonFor(UnitType type)
        {
            var sp = Spawner.Instance;
            if (sp == null || battleUI == null || battleUI.unitButtons == null) return null;
            for (int i = 0; i < sp.playerUnits.Length && i < battleUI.unitButtons.Length; i++)
                if (sp.playerUnits[i] != null && sp.playerUnits[i].type == type)
                    return (RectTransform)battleUI.unitButtons[i].transform;
            return null;
        }

        static int CountPlayer(UnitType type)
        {
            int n = 0;
            foreach (var u in Unit.All)
                if (u != null && u.Team == Team.Player && u.Data != null && u.Data.type == type) n++;
            return n;
        }
    }
}
