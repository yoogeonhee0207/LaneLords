using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Korean / English text. Auto = follow the device language. Changed in Settings > Language.
    /// Static labels made by the scene builder carry a LocalizedText; scripts call Loc.T(english, korean).
    /// </summary>
    public static class Loc
    {
        /// <summary>0 = auto (device language), 1 = English, 2 = Korean.</summary>
        public static int Setting
        {
            get => PlayerPrefs.GetInt("setting_lang", 0);
            set { PlayerPrefs.SetInt("setting_lang", value); PlayerPrefs.Save(); }
        }

        public static bool Korean => Setting == 2 || (Setting == 0 && Application.systemLanguage == SystemLanguage.Korean);

        public static string T(string en, string ko) => Korean ? ko : en;

        public static string SettingLabel()
        {
            switch (Setting)
            {
                case 1: return T("Language: English", "언어: English");
                case 2: return T("Language: Korean", "언어: 한국어");
                default: return T("Language: Auto", "언어: 자동");
            }
        }

        /// <summary>Korean for the fixed labels the scene builder creates.</summary>
        static readonly Dictionary<string, string> Ko = new Dictionary<string, string>
        {
            ["MENU"] = "메뉴",
            ["CLOSE"] = "닫기",
            ["SHOP"] = "상점",
            ["SETTINGS"] = "설정",
            ["PAUSED"] = "일시정지",
            ["RESUME"] = "계속하기",
            ["RESTART"] = "다시 시작",
            ["RETRY"] = "다시 하기",
            ["STAGES"] = "스테이지",
            ["NEXT"] = "다음",
            ["UNIT TRAINING"] = "유닛 강화",
            ["CASTLE UPGRADES"] = "성 업그레이드",
            ["SKIP"] = "건너뛰기",
            ["DAILY REWARD"] = "출석 보상",
            ["CODEX"] = "도감",
            ["ACHIEVEMENTS"] = "업적",
            ["CREDITS"] = "크레딧",
            ["Unit level-up odds: Lv.1-15 100% / Lv.16-25 80% / Lv.26-35 60% / Lv.36-50 40%. +10% after each fail. Fail keeps your level."]
                = "유닛 강화 확률: Lv.1~15 100% / Lv.16~25 80% / Lv.26~35 60% / Lv.36~50 40%. 실패할 때마다 +10%, 실패해도 레벨은 그대로예요.",
        };

        public static bool HasStatic(string en) => !string.IsNullOrEmpty(en) && Ko.ContainsKey(en);

        public static string Static(string en)
        {
            string ko;
            return Korean && en != null && Ko.TryGetValue(en, out ko) ? ko : en;
        }
    }
}
