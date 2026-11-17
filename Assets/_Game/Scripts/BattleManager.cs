using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Tooltip("Used when the battle scene is played directly (no stage chosen on the stage select screen)")]
        public StageData stage;
        public StageCatalog catalog;
        public string stageSelectSceneName = "StageSelect";
        public Castle playerCastle;
        public Castle enemyCastle;
        [Tooltip("Parent of the trees workers chop (each tree has a child named Base at its trunk)")]
        public Transform goldMine;
        public GoldManager gold;
        public EnemyWaveController waves;

        [Header("Effects")]
        public RuntimeAnimatorController dustEffect;
        public RuntimeAnimatorController explosionEffect;
        public RuntimeAnimatorController fireEffect;
        public RuntimeAnimatorController healEffect;
        [Tooltip("Font for floating numbers (+heal)")]
        public Font popupFont;
        public Sprite arrowSprite;

        [Header("HP bar sprites")]
        public Sprite bigBarFrame;
        public Sprite bigBarFill;
        public Sprite smallBarFrame;
        public Sprite smallBarFill;

        [Header("Result UI")]
        public GameObject resultPanel;
        [Tooltip("Details on the paper: stars and time")]
        public Text resultText;
        public Image resultRibbon;
        public Text resultTitle;
        public Sprite victoryRibbon;
        public Sprite defeatRibbon;
        [Tooltip("Shown only after a win when there is a next stage")]
        public GameObject nextStageButton;
        [Tooltip("Yellow line under the result: new unit unlockable / shop hint")]
        public Text resultNotice;
        [Tooltip("Win screen: watch a rewarded ad for double gems")]
        public GameObject doubleGemsButton;
        public Text doubleGemsLabel;

        int lastRewardGems;
        bool resultShown;
        bool watchedRewarded;

        public bool IsRunning { get; private set; }
        /// <summary>True while the pause menu is open.</summary>
        public bool IsPaused { get; set; }
        public float ElapsedTime { get; private set; }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Unit.All.Clear();
            GameSettings.Apply();

            int idx = GameSession.SelectedStageIndex;
            if (catalog != null && idx >= 0 && idx < catalog.stages.Count && catalog.stages[idx] != null)
                stage = catalog.stages[idx];
            ApplyMapWidth();
        }

        void Start()
        {
            MusicPlayer.Play(MusicPlayer.ChapterTrack(CastleTiers.TierForStage(stage) + 1));
            foreach (var c in new[] { playerCastle, enemyCastle })
            {
                var tiers = c != null ? c.GetComponent<CastleTiers>() : null;
                if (tiers != null) tiers.Apply(stage);
            }
            playerCastle.Init(stage.playerCastleHp * Upgrades.CastleHpMult);
            enemyCastle.Init(stage.enemyCastleHp);
            gold.Init(stage);
            gold.baseIncome += Upgrades.IncomeBonus;
            gold.incomePerWorker += Upgrades.WorkerIncomeBonus;
            appliedIncome = Upgrades.IncomeBonus;
            appliedWorkerIncome = Upgrades.WorkerIncomeBonus;
            gold.AddGold(Upgrades.StartGoldBonus);
            waves.Init(stage);
            if (resultPanel != null) resultPanel.SetActive(false);
            IsRunning = true;
            Ads.GameplayStart();
        }

        /// <summary>The scene is built for chapter 1 (castles at x = +-9.5). Later chapters push the castles apart.</summary>
        const float BuiltHalfWidth = 9.5f;

        void ApplyMapWidth()
        {
            float w = stage != null && stage.mapHalfWidth > 0f ? stage.mapHalfWidth : BuiltHalfWidth;
            float shift = w - BuiltHalfWidth;
            if (playerCastle != null) SetX(playerCastle.transform, -w);
            if (enemyCastle != null) SetX(enemyCastle.transform, w);
            // the forest stays next to our castle, just clear of a full-size (tier 4) castle
            if (goldMine != null) SetX(goldMine, -w + 4.7f);

            var deco = GameObject.Find("Decorations");
            if (deco != null)
            {
                foreach (Transform child in deco.transform)
                {
                    SetX(child, child.position.x * w / BuiltHalfWidth);
                    // keep bushes and rocks off the castles (a tier-4 castle is ~3 units wide each side)
                    bool onCastle = Mathf.Abs(child.position.x) > w - 3.2f && child.position.y < 2.5f;
                    child.gameObject.SetActive(!onCastle);
                }
            }

            var pan = Camera.main != null ? Camera.main.GetComponent<CameraPan>() : null;
            if (pan != null)
            {
                pan.fieldMinX = -(w + 3.5f); // room to see a whole tier-4 castle
                pan.fieldMaxX = w + 3.5f;
            }
        }

        static void SetX(Transform t, float x)
        {
            Vector3 p = t.position;
            p.x = x;
            t.position = p;
        }

        float appliedIncome, appliedWorkerIncome;

        /// <summary>
        /// Called after a purchase in the in-battle shop: income and castle HP upgrades take effect now,
        /// unit levels apply to newly spawned units, newly unlocked units get their button.
        /// (Start gold only matters at the start of the next battle.)
        /// </summary>
        public void ApplyUpgradesLive()
        {
            gold.baseIncome += Upgrades.IncomeBonus - appliedIncome;
            gold.incomePerWorker += Upgrades.WorkerIncomeBonus - appliedWorkerIncome;
            appliedIncome = Upgrades.IncomeBonus;
            appliedWorkerIncome = Upgrades.WorkerIncomeBonus;
            playerCastle.SetMaxHp(stage.playerCastleHp * Upgrades.CastleHpMult);
            var playerTiers = playerCastle.GetComponent<CastleTiers>();
            if (playerTiers != null) playerTiers.Apply(stage); // the base grows right away
            var ui = FindAnyObjectByType<BattleUI>();
            if (ui != null) ui.LayoutButtons();
        }

        void Update()
        {
            if (IsRunning) ElapsedTime += Time.deltaTime;
            if (IsRunning && !IsPaused) HandleTestSpeedKeys();
        }

        public Castle GetCastle(Team team) => team == Team.Player ? playerCastle : enemyCastle;

        // Work spots: one on each side of every tree trunk. Each worker gets its own spot.
        readonly List<Vector3> workSpots = new List<Vector3>();
        readonly List<float> workSpotFaceX = new List<float>();
        readonly List<bool> workSpotTaken = new List<bool>();

        void BuildWorkSpots()
        {
            workSpots.Clear();
            workSpotFaceX.Clear();
            workSpotTaken.Clear();
            if (goldMine == null) return;

            var trunks = new List<Vector3>();
            for (int i = 0; i < goldMine.childCount; i++)
            {
                Transform tree = goldMine.GetChild(i);
                Transform baseTf = tree.Find("Base");
                trunks.Add(baseTf != null ? baseTf.position : tree.position);
            }

            foreach (Vector3 trunk in trunks)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 spot = trunk + new Vector3(side * 0.5f, 0.35f, 0f);

                    // Skip a spot that would stand on the trunk of a tree in front of this one.
                    bool blocked = false;
                    foreach (Vector3 other in trunks)
                        if (other.y < trunk.y - 0.05f && Mathf.Abs(spot.x - other.x) < 0.45f) blocked = true;
                    // Skip a spot that sits right on top of a spot we already have.
                    foreach (Vector3 existing in workSpots)
                        if (Mathf.Abs(spot.x - existing.x) < 0.35f && Mathf.Abs(spot.y - existing.y) < 0.4f) blocked = true;
                    if (blocked) continue;

                    workSpots.Add(spot);
                    workSpotFaceX.Add(trunk.x);
                    workSpotTaken.Add(false);
                }
            }
        }

        /// <summary>
        /// Gives a worker a free spot, as far left/right as possible from the other workers.
        /// Returns the spot index (pass it back to ReleaseWorkSpot), or -1 if there is none.
        /// </summary>
        public int ClaimWorkSpot(out Vector3 position, out float faceX)
        {
            if (workSpots.Count == 0) BuildWorkSpots();
            int best = -1;
            float bestGap = -1f;
            for (int i = 0; i < workSpots.Count; i++)
            {
                if (workSpotTaken[i]) continue;
                float gap = float.MaxValue;
                for (int j = 0; j < workSpots.Count; j++)
                    if (workSpotTaken[j]) gap = Mathf.Min(gap, Mathf.Abs(workSpots[i].x - workSpots[j].x));
                if (gap == float.MaxValue) gap = 1000f - Mathf.Abs(workSpots[i].x - goldMine.position.x); // first worker: near the middle
                if (gap > bestGap) { bestGap = gap; best = i; }
            }
            if (best < 0)
            {
                position = goldMine != null ? goldMine.position : Vector3.zero;
                faceX = position.x;
                return -1;
            }
            workSpotTaken[best] = true;
            position = workSpots[best];
            faceX = workSpotFaceX[best];
            return best;
        }

        public void ReleaseWorkSpot(int index)
        {
            if (index >= 0 && index < workSpotTaken.Count) workSpotTaken[index] = false;
        }

        public void OnCastleDestroyed(Castle castle)
        {
            if (!IsRunning) return;
            IsRunning = false;
            Sfx.Play("castle_break");

            Vector3 p = castle.transform.position + new Vector3(0f, 1.5f, 0f); // the base's pivot is on the ground
            SpawnEffect(explosionEffect, p, 1.2f, 2.5f);
            SpawnEffect(fireEffect, p + new Vector3(-1f, 0.6f, 0f), 0f, 1.5f);
            SpawnEffect(fireEffect, p + new Vector3(1f, 0.2f, 0f), 0f, 1.5f);

            StartCoroutine(ShowResult(castle.team == Team.Enemy));
        }

        IEnumerator ShowResult(bool win)
        {
            Ads.GameplayStop();
            lastRewardGems = 0;
            MusicPlayer.PlayJingle(win ? "jingle_win" : "jingle_lose");
            yield return new WaitForSeconds(1.5f);

            // Boss stages: story outro after the first clear.
            var story = StoryDialogUI.Instance;
            if (win && story != null && story.PlayOutro(stage.stageName))
            {
                while (story.IsShowing) yield return null;
                // Final boss: ending and "THE END" (first clear only)
                if (stage.stageName == "4-6" && story.PlayEnding())
                {
                    while (story.IsShowing) yield return null;
                    yield return story.ShowBanner("THE END", Loc.T("Thanks for playing!", "플레이해 주셔서 고마워요!"), 3.5f, true);
                    story.HideBackdrop();
                }
            }

            if (resultRibbon != null)
            {
                Sprite ribbon = win ? victoryRibbon : defeatRibbon;
                if (ribbon != null) resultRibbon.sprite = ribbon;
            }
            if (resultTitle != null) resultTitle.text = win ? Loc.T("VICTORY", "승리") : Loc.T("DEFEAT", "패배");

            if (win)
            {
                int stars = 1;
                if (playerCastle.Hp / playerCastle.MaxHp >= 0.5f) stars++;
                if (ElapsedTime <= 120f) stars++;
                bool firstClear = SaveManager.GetStars(stage.stageName) == 0;
                var newUnits = UnitProgress.UnlockedBy(stage.stageName);
                int gems = stars * 10 + (firstClear ? 20 : 0); // first clear bonus
                SaveManager.AddGems(gems);
                lastRewardGems = gems;
                if (gems > 0) Sfx.Play("gem");
                SaveManager.SaveStars(stage.stageName, stars);
                resultText.text = Loc.T($"Stars {stars} / 3   Time {ElapsedTime:0}s\n+{gems} Gems" + (firstClear ? " (first clear!)" : ""),
                                         $"별 {stars} / 3   시간 {ElapsedTime:0}초\n보석 +{gems}" + (firstClear ? " (첫 클리어!)" : ""));
                if (resultNotice != null)
                {
                    string notice = "";
                    if (newUnits.Count > 0)
                    {
                        var names = new List<string>();
                        foreach (var t in newUnits) names.Add(UnitProgress.Name(t));
                        notice = Loc.T("NEW UNIT: " + string.Join(", ", names) + "  - unlock it in the SHOP!",
                                       "새 유닛: " + string.Join(", ", names) + " - 상점에서 해금하세요!");
                    }
                    else if (UnitProgress.AnyNewUnlockAvailable()) notice = Loc.T("A new unit is waiting in the SHOP!", "상점에서 새 유닛이 기다리고 있어요!");
                    resultNotice.text = notice;
                }
            }
            else
            {
                SaveManager.AddGems(5);
                resultText.text = Loc.T($"Time {ElapsedTime:0}s   +5 Gems\nTry again!", $"시간 {ElapsedTime:0}초   보석 +5\n다시 도전해 보세요!");
                if (resultNotice != null) resultNotice.text = Loc.T("Train units and upgrade your castle in the SHOP.", "상점에서 유닛 강화와 성 업그레이드를 할 수 있어요.");
            }
            if (nextStageButton != null) nextStageButton.SetActive(win && NextStageIndex() >= 0);
            if (doubleGemsButton != null)
            {
                bool offer = win && lastRewardGems > 0 && Ads.RewardedAvailable;
                doubleGemsButton.SetActive(offer);
                if (offer && doubleGemsLabel != null)
                    doubleGemsLabel.text = Loc.T($"WATCH AD: +{lastRewardGems} GEMS", $"광고 보고 보석 +{lastRewardGems}");
            }
            if (resultPanel != null) resultPanel.SetActive(true);
            resultShown = true;
        }

        /// <summary>Win screen button: watch a rewarded ad to get the stage's gems a second time.</summary>
        public void WatchDoubleGems()
        {
            if (watchedRewarded || lastRewardGems <= 0) return;
            Ads.Rewarded(() =>
            {
                watchedRewarded = true;
                SaveManager.AddGems(lastRewardGems);
                Sfx.Play("gem");
                if (doubleGemsButton != null) doubleGemsButton.SetActive(false);
                if (resultNotice != null)
                    resultNotice.text = Loc.T($"Bonus! +{lastRewardGems} gems (x2)", $"보너스! 보석 +{lastRewardGems} (2배)");
            });
        }

        /// <summary>
        /// Leaving the result screen is a natural break, so a midgame ad may show first
        /// (CrazyGames paces them itself). Skipped if the player just watched a rewarded ad.
        /// </summary>
        void Leave(System.Action go)
        {
            Ads.GameplayStop();
            if (resultShown && !watchedRewarded) Ads.Midgame(go);
            else go();
        }

        /// <summary>Result screen SHOP button: back to the stage select screen with the shop open.</summary>
        public void GoToShop()
        {
            GameSession.OpenShopOnLoad = true;
            int i = CurrentStageIndex();
            if (i >= 0) GameSession.SelectedStageIndex = i;
            BackToStageSelect();
        }

        public void Restart() { Leave(DoRestart); }

        void DoRestart()
        {
            Time.timeScale = 1f;
            int i = CurrentStageIndex();
            if (i >= 0) GameSession.SelectedStageIndex = i;
            SceneFader.Reload();
        }

        int CurrentStageIndex() => catalog != null ? catalog.stages.IndexOf(stage) : -1;

        int NextStageIndex()
        {
            int i = CurrentStageIndex();
            return (catalog != null && i >= 0 && i + 1 < catalog.stages.Count) ? i + 1 : -1;
        }

        public void NextStage() { Leave(DoNextStage); }

        void DoNextStage()
        {
            int next = NextStageIndex();
            if (next < 0) { DoBackToStageSelect(); return; }
            GameSession.SelectedStageIndex = next;
            Time.timeScale = 1f;
            SceneFader.Reload();
        }

        public void BackToStageSelect() { Leave(DoBackToStageSelect); }

        void DoBackToStageSelect()
        {
            Time.timeScale = 1f;
            SceneFader.Load(stageSelectSceneName);
        }

        // Players get x1/x2. In the Unity editor (testing) the button also cycles x4 and x8,
        // and the number keys 1-4 jump straight to x1 / x2 / x4 / x8.
        static readonly float[] PlayerSpeeds = { 1f, 2f };
        static readonly float[] TestSpeeds = { 1f, 2f, 4f, 8f };
        static float[] Speeds => Application.isEditor ? TestSpeeds : PlayerSpeeds;

        public void ToggleSpeed()
        {
            if (IsPaused) return;
            float[] speeds = Speeds;
            int current = 0;
            for (int i = 0; i < speeds.Length; i++)
                if (Mathf.Approximately(Time.timeScale, speeds[i])) current = i;
            Time.timeScale = speeds[(current + 1) % speeds.Length];
        }

        void HandleTestSpeedKeys()
        {
            if (!Application.isEditor) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) Time.timeScale = 1f;
            if (kb.digit2Key.wasPressedThisFrame) Time.timeScale = 2f;
            if (kb.digit3Key.wasPressedThisFrame) Time.timeScale = 4f;
            if (kb.digit4Key.wasPressedThisFrame) Time.timeScale = 8f;
        }

        /// <summary>Floating text that rises and fades (heal numbers).</summary>
        public void PopText(string text, Vector3 position, Color color)
        {
            FloatingText.Spawn(popupFont, text, position, color);
        }

        public GameObject SpawnEffect(RuntimeAnimatorController controller, Vector3 position, float lifetime, float scale)
        {
            if (controller == null) return null;
            var go = new GameObject("FX");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 950;
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if (lifetime > 0f) Destroy(go, lifetime);
            return go;
        }
    }
}
