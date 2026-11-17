using System;
using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Daily login reward on a 7-day cycle. Missing a day starts the cycle over.
    /// Uses the device date. Saved in PlayerPrefs.
    /// </summary>
    public static class DailyReward
    {
        public static readonly int[] Rewards = { 10, 15, 20, 25, 30, 40, 60 };
        const string LastKey = "daily_last";      // yyyy-MM-dd of the last claim
        const string StreakKey = "daily_streak";  // consecutive days claimed (1, 2, 3, ...)
        public const string BestStreakKey = "daily_best_streak";

        static DateTime Today => DateTime.Now.Date;

        static DateTime? LastClaim
        {
            get
            {
                string s = PlayerPrefs.GetString(LastKey, "");
                return DateTime.TryParseExact(s, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d) ? d : (DateTime?)null;
            }
        }

        public static bool CanClaim => LastClaim == null || LastClaim.Value < Today;

        /// <summary>Streak the next claim will make (1 if the chain was broken).</summary>
        static int NextStreak
        {
            get
            {
                var last = LastClaim;
                int streak = PlayerPrefs.GetInt(StreakKey, 0);
                if (last == null) return 1;
                if (last.Value == Today) return streak;
                return last.Value == Today.AddDays(-1) ? streak + 1 : 1;
            }
        }

        /// <summary>0..6: which box of the 7-day row is today's.</summary>
        public static int TodayIndex => (Mathf.Max(1, NextStreak) - 1) % Rewards.Length;

        public static int Claim()
        {
            if (!CanClaim) return 0;
            int streak = NextStreak;
            int gems = Rewards[(streak - 1) % Rewards.Length];
            PlayerPrefs.SetString(LastKey, Today.ToString("yyyy-MM-dd"));
            PlayerPrefs.SetInt(StreakKey, streak);
            PlayerPrefs.SetInt(BestStreakKey, Mathf.Max(streak, PlayerPrefs.GetInt(BestStreakKey, 0)));
            SaveManager.AddGems(gems);
            PlayerPrefs.Save();
            return gems;
        }

        // ---------------- Window ----------------

        static GameObject window;

        /// <summary>Opens the reward window if today's reward has not been taken yet.</summary>
        public static void ShowIfAvailable()
        {
            if (CanClaim) Open();
        }

        public static void Open()
        {
            if (window != null) return;
            window = UIKit.Window("Daily Reward", Loc.T("DAILY REWARD", "출석 보상"), new Vector2(1400f, 560f), Close, out RectTransform paper);
            int today = TodayIndex;
            bool canClaim = CanClaim;

            UIKit.Txt(paper, "Info", Loc.T("Come back every day! Missing a day starts over from Day 1.", "매일 들어와서 받아 가세요! 하루라도 빠지면 1일부터 다시 시작해요."),
                28, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(1250f, 50f), true);

            const float boxW = 160f, gap = 20f;
            float startX = -(Rewards.Length - 1) * (boxW + gap) / 2f;
            for (int i = 0; i < Rewards.Length; i++)
            {
                var box = UIKit.Rect("Day" + (i + 1), paper, new Vector2(0.5f, 0.5f), new Vector2(startX + i * (boxW + gap), 10f), new Vector2(boxW, 200f));
                var img = UIKit.Img(box, UISkin.Instance != null ? UISkin.Instance.banner : null, 3f);
                bool done = i < today || (i == today && !canClaim);
                bool isToday = i == today && canClaim;
                img.color = done ? new Color(0.7f, 0.7f, 0.7f, 1f) : isToday ? new Color(1f, 0.95f, 0.6f, 1f) : Color.white;
                UIKit.Txt(box, "Day", Loc.T($"Day {i + 1}", $"{i + 1}일"), 30, TextAnchor.MiddleCenter, new Vector2(0f, 50f), new Vector2(boxW, 50f), true);
                UIKit.Txt(box, "Gems", Loc.T($"{Rewards[i]}\nGems", $"보석\n{Rewards[i]}"), 32, TextAnchor.MiddleCenter, new Vector2(0f, -15f), new Vector2(boxW, 90f), true);
                if (done) UIKit.Txt(box, "Check", Loc.T("DONE", "완료"), 34, TextAnchor.MiddleCenter, new Vector2(0f, -70f), new Vector2(boxW, 50f)).color = new Color(0.4f, 1f, 0.5f);
            }

            // With a rewarded ad available, CLAIM sits on the left and "watch an ad for double" on the right.
            bool offerAd = canClaim && Ads.RewardedAvailable;
            var claim = UIKit.Btn(paper, "Claim", canClaim ? Loc.T("CLAIM", "받기") : Loc.T("Come back tomorrow", "내일 또 오세요"),
                new Vector2(offerAd ? -220f : 0f, -170f), new Vector2(canClaim ? 300f : 460f, 90f), false, null, 38, null);
            claim.interactable = canClaim;
            claim.onClick.AddListener(() => ClaimAndRefresh(false));

            if (offerAd)
            {
                var ad = UIKit.Btn(paper, "Claim x2", Loc.T("WATCH AD: x2", "광고 보고 2배"),
                    new Vector2(200f, -170f), new Vector2(400f, 90f), true, null, 36, null);
                ad.onClick.AddListener(() => Ads.Rewarded(() => ClaimAndRefresh(true)));
            }
        }

        static void ClaimAndRefresh(bool doubled)
        {
            int gems = Claim();
            if (gems <= 0) return;
            if (doubled) SaveManager.AddGems(gems); // the ad pays the same amount again
            Sfx.Play("gem");
            RefreshGems();
            Close();
            Open(); // reopen to show the check mark
        }

        public static void Close()
        {
            if (window != null) UnityEngine.Object.Destroy(window);
            window = null;
        }

        public static void RefreshGems()
        {
            // includes inactive shops; skip anything that isn't in a loaded scene
            foreach (var shop in Resources.FindObjectsOfTypeAll<UpgradeShopUI>())
                if (shop != null && shop.gameObject.scene.IsValid()) shop.Refresh();
        }
    }
}
