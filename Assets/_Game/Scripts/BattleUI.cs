using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    public class BattleUI : MonoBehaviour
    {
        public Text goldText;
        public Image playerHpFill;
        public Image enemyHpFill;
        public Text playerHpText;
        public Text enemyHpText;
        public Button[] unitButtons;
        public Image[] cooldownOverlays;
        public Text speedText;
        public Text stageText;

        Text[] costTexts;
        const float ButtonSize = 170f;
        const float ButtonGap = 20f;

        void Start()
        {
            var bm0 = BattleManager.Instance;
            if (stageText != null && bm0 != null && bm0.stage != null) stageText.text = Loc.T("STAGE ", "스테이지 ") + bm0.stage.stageName;

            // Castle HP is now shown above each castle, so hide the old top-bar HP.
            if (playerHpFill != null) playerHpFill.transform.parent.gameObject.SetActive(false);
            if (enemyHpFill != null) enemyHpFill.transform.parent.gameObject.SetActive(false);

            LayoutButtons();
        }

        /// <summary>Hides locked units and re-centres the remaining buttons (called again after an in-battle unlock).</summary>
        public void LayoutButtons()
        {
            var sp = Spawner.Instance;
            if (sp == null) return;
            var visible = new List<Button>();
            costTexts = new Text[unitButtons.Length];
            for (int i = 0; i < unitButtons.Length; i++)
            {
                Transform nameTf = unitButtons[i].transform.Find("Name");
                if (nameTf != null && i < sp.playerUnits.Length && sp.playerUnits[i] != null)
                    nameTf.GetComponent<Text>().text = UnitProgress.Name(sp.playerUnits[i].type);
                Transform costTf = unitButtons[i].transform.Find("Cost");
                costTexts[i] = costTf != null ? costTf.GetComponent<Text>() : null;

                bool unlocked = sp.IsAvailable(i);
                unitButtons[i].gameObject.SetActive(unlocked);
                if (unlocked) visible.Add(unitButtons[i]);
            }
            float startX = -(visible.Count - 1) * (ButtonSize + ButtonGap) / 2f;
            for (int j = 0; j < visible.Count; j++)
            {
                var rt = (RectTransform)visible[j].transform;
                rt.anchoredPosition = new Vector2(startX + j * (ButtonSize + ButtonGap), rt.anchoredPosition.y);
            }
        }

        void Update()
        {
            var gm = GoldManager.Instance;
            var bm = BattleManager.Instance;
            var sp = Spawner.Instance;
            if (gm == null || bm == null || sp == null) return;

            goldText.text = Loc.Korean
                ? $"골드 {Mathf.FloorToInt(gm.Gold)}   (+{gm.Income:0.#}/초)   일꾼 {gm.Workers}/{gm.maxWorkers}"
                : $"Gold {Mathf.FloorToInt(gm.Gold)}   (+{gm.Income:0.#}/s)   Workers {gm.Workers}/{gm.maxWorkers}";


            for (int i = 0; i < unitButtons.Length; i++)
            {
                bool ready = sp.CanSpawn(i);
                unitButtons[i].interactable = ready;
                // Ready = solid card; not enough gold or recharging = faded and slightly smaller
                var group = Group(i);
                if (group != null) group.alpha = ready ? 1f : 0.35f;
                float target = ready ? 1f : 0.93f;
                var tr = unitButtons[i].transform;
                float sc = Mathf.MoveTowards(tr.localScale.x, target, Time.unscaledDeltaTime * 2f);
                tr.localScale = new Vector3(sc, sc, 1f);
                // Show the live price (workers get more expensive as you hire more)
                if (costTexts != null && i < costTexts.Length && costTexts[i] != null)
                {
                    bool fullOfWorkers = sp.playerUnits[i].IsWorker && gm.Workers >= gm.maxWorkers;
                    costTexts[i].text = fullOfWorkers ? Loc.T("MAX", "최대") : sp.CostOf(i).ToString();
                }
                if (i < cooldownOverlays.Length) cooldownOverlays[i].fillAmount = sp.CooldownRatio(i);
            }

            if (speedText != null) speedText.text = "x" + Mathf.RoundToInt(Time.timeScale);
        }

        CanvasGroup[] groups;

        CanvasGroup Group(int i)
        {
            if (groups == null || groups.Length != unitButtons.Length) groups = new CanvasGroup[unitButtons.Length];
            if (groups[i] == null) groups[i] = unitButtons[i].GetComponent<CanvasGroup>();
            return groups[i];
        }

        static void UpdateHp(Castle castle, Image fill, Text label)
        {
            if (castle == null || castle.MaxHp <= 0f) return;
            fill.fillAmount = castle.Hp / castle.MaxHp;
            label.text = $"{Mathf.CeilToInt(castle.Hp)} / {Mathf.CeilToInt(castle.MaxHp)}";
        }
    }
}
