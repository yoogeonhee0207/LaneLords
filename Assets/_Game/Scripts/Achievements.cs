using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Lifetime counters used by achievements (saved in PlayerPrefs).</summary>
    public static class Stats
    {
        public const string Kills = "stat_kills";
        public const string Freed = "stat_freed";
        public const string Healed = "stat_healed";
        public const string Spawned = "stat_spawned";

        public static int Get(string key) => PlayerPrefs.GetInt(key, 0);

        public static void Add(string key, int amount = 1)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(key, Get(key) + amount);
            Achievements.MarkDirty();
        }
    }

    /// <summary>15 achievements with gem rewards. Progress is checked from saved data; a toast pops up when one is reached.</summary>
    public static class Achievements
    {
        public class Def
        {
            public string id, en, ko, descEn, descKo;
            public int target, gems;
            public Func<int> progress;
            public string Name => Loc.T(en, ko);
            public string Desc => Loc.T(descEn, descKo);
            public int Progress => Mathf.Min(target, progress());
            public bool Done => progress() >= target;
            public bool Claimed => PlayerPrefs.GetInt("ach_claimed_" + id, 0) == 1;
        }

        static readonly string[] AllStages = BuildStageNames();

        static string[] BuildStageNames()
        {
            var list = new List<string>();
            for (int c = 1; c <= 4; c++)
                for (int s = 1; s <= 6; s++) list.Add(c + "-" + s);
            return list.ToArray();
        }

        static int Cleared(string stage) => SaveManager.GetStars(stage) > 0 ? 1 : 0;

        static int ThreeStars()
        {
            int n = 0;
            foreach (var s in AllStages) if (SaveManager.GetStars(s) >= 3) n++;
            return n;
        }

        static int MaxUnitLevel()
        {
            int best = 0;
            foreach (var t in UnitProgress.ShopUnits) best = Mathf.Max(best, UnitProgress.Level(t));
            return best;
        }

        static int MaxUpgrade()
        {
            int best = 0;
            foreach (var t in Upgrades.All) best = Mathf.Max(best, Upgrades.Level(t));
            return best;
        }

        public static readonly Def[] All =
        {
            new Def { id = "first_win", en = "First Victory", ko = "첫 승리", descEn = "Clear stage 1-1.", descKo = "1-1 스테이지를 클리어하세요.", target = 1, gems = 10, progress = () => Cleared("1-1") },
            new Def { id = "boss1", en = "Forest Liberator", ko = "숲의 해방자", descEn = "Defeat the Chapter 1 boss.", descKo = "1장 보스를 물리치세요.", target = 1, gems = 30, progress = () => Cleared("1-6") },
            new Def { id = "boss2", en = "Peace on the Bamboo Hills", ko = "대나무 언덕의 평화", descEn = "Defeat the Chapter 2 boss.", descKo = "2장 보스를 물리치세요.", target = 1, gems = 40, progress = () => Cleared("2-6") },
            new Def { id = "boss3", en = "Victor of the Canyon", ko = "협곡의 승자", descEn = "Defeat the Chapter 3 boss.", descKo = "3장 보스를 물리치세요.", target = 1, gems = 50, progress = () => Cleared("3-6") },
            new Def { id = "boss4", en = "Crown Breaker", ko = "왕관을 부순 자", descEn = "Defeat the Troll.", descKo = "트롤을 물리치세요.", target = 1, gems = 100, progress = () => Cleared("4-6") },
            new Def { id = "stars10", en = "Perfectionist", ko = "완벽주의자", descEn = "Get 3 stars on 10 stages.", descKo = "스테이지 10개에서 별 3개를 받으세요.", target = 10, gems = 50, progress = ThreeStars },
            new Def { id = "stars_all", en = "Legendary Commander", ko = "전설의 지휘관", descEn = "Get 3 stars on every stage.", descKo = "모든 스테이지에서 별 3개를 받으세요.", target = 24, gems = 200, progress = ThreeStars },
            new Def { id = "kills500", en = "Hunter", ko = "사냥꾼", descEn = "Defeat 500 enemies.", descKo = "적을 500마리 물리치세요.", target = 500, gems = 30, progress = () => Stats.Get(Stats.Kills) },
            new Def { id = "kills3000", en = "Unstoppable", ko = "멈출 수 없는 군대", descEn = "Defeat 3000 enemies.", descKo = "적을 3000마리 물리치세요.", target = 3000, gems = 80, progress = () => Stats.Get(Stats.Kills) },
            new Def { id = "freed100", en = "Liberator", ko = "해방자", descEn = "Free 100 cursed animals.", descKo = "저주받은 동물 100마리를 풀어 주세요.", target = 100, gems = 40, progress = () => Stats.Get(Stats.Freed) },
            new Def { id = "heal5000", en = "Healing Touch", ko = "치유의 손길", descEn = "Heal 5000 HP with Monks.", descKo = "수도사로 체력을 5000 회복하세요.", target = 5000, gems = 40, progress = () => Stats.Get(Stats.Healed) },
            new Def { id = "spawn1000", en = "Grand Army", ko = "대군", descEn = "Summon 1000 units.", descKo = "유닛을 1000명 소환하세요.", target = 1000, gems = 40, progress = () => Stats.Get(Stats.Spawned) },
            new Def { id = "unit50", en = "Fully Trained", ko = "단련의 끝", descEn = "Train one unit to level 50.", descKo = "유닛 하나를 50레벨까지 강화하세요.", target = UnitProgress.MaxLevel, gems = 80, progress = MaxUnitLevel },
            new Def { id = "upgrade_max", en = "Fortress", ko = "철옹성", descEn = "Max out one castle upgrade.", descKo = "성 업그레이드 하나를 최대까지 올리세요.", target = Upgrades.MaxLevel, gems = 60, progress = MaxUpgrade },
            new Def { id = "daily7", en = "Loyal Commander", ko = "성실한 지휘관", descEn = "Claim the daily reward 7 days in a row.", descKo = "출석 보상을 7일 연속으로 받으세요.", target = 7, gems = 50, progress = () => PlayerPrefs.GetInt(DailyReward.BestStreakKey, 0) },
        };

        public static bool AnyClaimable
        {
            get
            {
                foreach (var a in All) if (a.Done && !a.Claimed) return true;
                return false;
            }
        }

        public static bool Claim(Def a)
        {
            if (!a.Done || a.Claimed) return false;
            PlayerPrefs.SetInt("ach_claimed_" + a.id, 1);
            SaveManager.AddGems(a.gems);
            PlayerPrefs.Save();
            return true;
        }

        // ---------------- Saving + toast watcher ----------------

        static bool dirty;
        public static void MarkDirty() { dirty = true; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Achievements");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<AchievementWatcher>();
        }

        public static void CheckNew(Queue<Def> toasts)
        {
            foreach (var a in All)
            {
                string key = "ach_notified_" + a.id;
                if (a.Done && PlayerPrefs.GetInt(key, 0) == 0)
                {
                    PlayerPrefs.SetInt(key, 1);
                    toasts.Enqueue(a);
                }
            }
        }

        public static void SaveIfDirty()
        {
            if (!dirty) return;
            dirty = false;
            PlayerPrefs.Save();
        }

        // ---------------- Window ----------------

        static GameObject window;

        public static void Open()
        {
            if (window != null) return;
            window = UIKit.Window("Achievements", Loc.T("ACHIEVEMENTS", "업적"), new Vector2(1300f, 800f), Close, out RectTransform paper);
            var content = UIKit.ScrollArea(paper, new Vector2(0f, -25f), new Vector2(1180f, 640f), out ScrollRect _);
            const float rowH = 120f;
            for (int i = 0; i < All.Length; i++)
            {
                var a = All[i];
                var row = UIKit.Rect("Row " + a.id, content, new Vector2(0.5f, 1f), new Vector2(0f, -rowH / 2f - i * rowH), new Vector2(1140f, rowH - 12f));
                var bg = UIKit.Img(row, UISkin.Instance != null ? UISkin.Instance.banner : null, 3f);
                bg.color = a.Claimed ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;
                UIKit.Txt(row, "Name", a.Name, 34, TextAnchor.MiddleLeft, new Vector2(-170f, 20f), new Vector2(720f, 50f), true);
                UIKit.Txt(row, "Desc", a.Desc, 26, TextAnchor.MiddleLeft, new Vector2(-170f, -22f), new Vector2(720f, 40f), true);
                UIKit.Txt(row, "Reward", Loc.T($"{a.gems} Gems", $"보석 {a.gems}"), 28, TextAnchor.MiddleCenter, new Vector2(270f, 0f), new Vector2(160f, 50f), true);

                if (a.Claimed)
                    UIKit.Txt(row, "State", Loc.T("DONE", "완료"), 32, TextAnchor.MiddleCenter, new Vector2(460f, 0f), new Vector2(200f, 60f), true).color = UIKit.Green;
                else if (a.Done)
                {
                    var def = a;
                    UIKit.Btn(row, "Claim", Loc.T("CLAIM", "받기"), new Vector2(460f, 0f), new Vector2(190f, 80f), false, () =>
                    {
                        if (!Claim(def)) return;
                        Sfx.Play("gem");
                        DailyReward.RefreshGems();
                        Close();
                        Open();
                    }, 32, null);
                }
                else
                    UIKit.Txt(row, "State", $"{a.Progress} / {a.target}", 28, TextAnchor.MiddleCenter, new Vector2(460f, 0f), new Vector2(220f, 60f), true);
            }
            UIKit.SetContentHeight(content, All.Length * rowH + 10f);
        }

        public static void Close()
        {
            if (window != null) UnityEngine.Object.Destroy(window);
            window = null;
        }
    }

    /// <summary>Checks achievements once a second, saves counters now and then, and shows "Achievement unlocked!" toasts.</summary>
    public class AchievementWatcher : MonoBehaviour
    {
        readonly Queue<Achievements.Def> toasts = new Queue<Achievements.Def>();
        float checkTimer, saveTimer, toastTimer;
        GameObject toast;
        CanvasGroup toastGroup;
        Text toastName;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            checkTimer -= dt;
            if (checkTimer <= 0f)
            {
                checkTimer = 1f;
                Achievements.CheckNew(toasts);
            }
            saveTimer -= dt;
            if (saveTimer <= 0f)
            {
                saveTimer = 10f;
                Achievements.SaveIfDirty();
            }

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                if (toastGroup != null)
                    toastGroup.alpha = toastTimer > 2.6f ? (3f - toastTimer) / 0.4f : toastTimer < 0.4f ? toastTimer / 0.4f : 1f;
                if (toastTimer <= 0f && toast != null) toast.SetActive(false);
            }
            else if (toasts.Count > 0) Show(toasts.Dequeue());
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveAll();
        }

        // On the web the tab can be closed at any time: save whenever the game loses focus.
        void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveAll();
        }

        void OnApplicationQuit()
        {
            SaveAll();
        }

        static void SaveAll()
        {
            Achievements.SaveIfDirty();
            PlayerPrefs.Save();
        }

        void Show(Achievements.Def a)
        {
            if (UISkin.Instance == null) return; // no skin in this scene: skip the popup (the reward can still be claimed)
            if (toast == null) Build();
            toastName.text = a.Name;
            toast.SetActive(true);
            toastTimer = 3f;
            Sfx.Play("ui_buy");
        }

        void Build()
        {
            toast = new GameObject("Achievement Toast", typeof(RectTransform));
            toast.transform.SetParent(transform, false);
            var canvas = toast.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = toast.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            toastGroup = toast.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            var box = UIKit.Rect("Box", toast.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(760f, 150f));
            UIKit.Img(box, UISkin.Instance.paper, 1.5f);
            UIKit.Txt(box, "Head", Loc.T("Achievement unlocked!", "업적 달성!"), 30, TextAnchor.MiddleCenter, new Vector2(0f, 28f), new Vector2(700f, 44f), true).color = UIKit.Green;
            toastName = UIKit.Txt(box, "Name", "", 40, TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(700f, 54f), true);
        }
    }
}
