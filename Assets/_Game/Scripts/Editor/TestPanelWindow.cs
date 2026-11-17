using UnityEditor;
using UnityEngine;

namespace TinySwordsBattle.EditorTools
{
    /// <summary>
    /// Editor-only test panel: pick unit levels, castle upgrade levels, gems and unlocked stages directly.
    /// Tools > Lane Lords > Test Panel. Changes are saved immediately; in a running battle,
    /// income / castle changes apply right away and unit levels apply to newly spawned units.
    /// </summary>
    public class TestPanelWindow : EditorWindow
    {
        const string CatalogPath = "Assets/_Game/Data/StageCatalog.asset";
        Vector2 scroll;
        int gemsInput = 1000;

        [MenuItem("Tools/Lane Lords/Test Panel")]
        public static void Open()
        {
            var w = GetWindow<TestPanelWindow>("Test Panel");
            w.minSize = new Vector2(340, 520);
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("테스트용 설정이에요. 에디터에서만 쓰이고 실제 빌드에는 영향이 없어요.", MessageType.Info);

            // ---------- Gems ----------
            EditorGUILayout.LabelField("보석", EditorStyles.boldLabel);
            bool inf = PlayerPrefs.GetInt("test_infinite_gems", 0) == 1;
            bool newInf = EditorGUILayout.Toggle("보석 무제한", inf);
            if (newInf != inf) { PlayerPrefs.SetInt("test_infinite_gems", newInf ? 1 : 0); Save(); }
            EditorGUILayout.LabelField("현재 보석", SaveManager.Gems.ToString());
            EditorGUILayout.BeginHorizontal();
            gemsInput = EditorGUILayout.IntField(gemsInput);
            if (GUILayout.Button("보석 설정", GUILayout.Width(90))) { PlayerPrefs.SetInt("gems", Mathf.Max(0, gemsInput)); Save(); }
            EditorGUILayout.EndHorizontal();

            // ---------- Units ----------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("유닛 강화 (레벨당 체력·공격력 +2%, Lv.50 = 2배)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("지금 상한", $"Lv.{UnitProgress.LevelCap()} (1장 15 / 1-6 클리어 25 / 2-6 클리어 35 / 3-6 클리어 50)");
            foreach (var t in UnitProgress.ShopUnits)
            {
                EditorGUILayout.BeginHorizontal();
                bool alwaysOpen = t == UnitType.Warrior;
                bool unlocked = UnitProgress.IsUnlocked(t);
                EditorGUI.BeginDisabledGroup(alwaysOpen);
                bool newUnlocked = EditorGUILayout.ToggleLeft(UnitProgress.Name(t), unlocked, GUILayout.Width(90));
                EditorGUI.EndDisabledGroup();
                int level = UnitProgress.Level(t);
                int newLevel = EditorGUILayout.IntSlider(level, 0, UnitProgress.MaxLevel);
                EditorGUILayout.EndHorizontal();
                if (newUnlocked != unlocked || newLevel != level)
                {
                    UnitProgress.SetForTest(t, alwaysOpen || newUnlocked, newLevel);
                    Save();
                }
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전부 해금 + Lv.50")) SetAllUnits(true, UnitProgress.MaxLevel);
            if (GUILayout.Button("처음 상태로")) SetAllUnits(false, 0);
            EditorGUILayout.EndHorizontal();

            // ---------- Castle upgrades ----------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("성 업그레이드", EditorStyles.boldLabel);
            foreach (var t in Upgrades.All)
            {
                int level = Upgrades.Level(t);
                int newLevel = EditorGUILayout.IntSlider($"{Upgrades.Name(t)} ({Upgrades.Effect(t)})", level, 0, Upgrades.MaxLevel);
                if (newLevel != level) { Upgrades.SetLevelForTest(t, newLevel); Save(); }
            }
            int castleLv = Upgrades.Level(UpgradeType.CastleHp);
            EditorGUILayout.LabelField("성 체력 / 모습", $"{Mathf.RoundToInt(700 * Upgrades.CastleHpMult)}  /  {CastleTiers.TierForUpgrade(castleLv) + 1}단계");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전부 Lv.10")) SetAllUpgrades(Upgrades.MaxLevel);
            if (GUILayout.Button("전부 0")) SetAllUpgrades(0);
            EditorGUILayout.EndHorizontal();

            // ---------- Stages ----------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스테이지", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모든 스테이지 열기")) SetAllStages(1);
            if (GUILayout.Button("스테이지 기록 지우기")) SetAllStages(0);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        void SetAllUnits(bool unlocked, int level)
        {
            foreach (var t in UnitProgress.ShopUnits)
                UnitProgress.SetForTest(t, unlocked || t == UnitType.Warrior, level);
            Save();
        }

        void SetAllUpgrades(int level)
        {
            foreach (var t in Upgrades.All) Upgrades.SetLevelForTest(t, level);
            Save();
        }

        void SetAllStages(int stars)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            if (catalog == null) return;
            foreach (var s in catalog.stages)
                if (s != null && (stars == 0 || SaveManager.GetStars(s.stageName) == 0))
                    SaveManager.SetStarsForTest(s.stageName, stars);
            Save();
        }

        static void Save()
        {
            PlayerPrefs.Save();
            // In a running battle, apply income / castle changes now.
            if (Application.isPlaying && BattleManager.Instance != null) BattleManager.Instance.ApplyUpgradesLive();
        }
    }
}
