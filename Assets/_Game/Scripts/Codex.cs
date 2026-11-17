using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Codex: every unit and monster with a picture, stats and a short note.
    /// Monsters stay hidden ("???") until met in battle once; player units open when unlocked.
    /// </summary>
    public static class Codex
    {
        struct Entry { public string ko, en, noteKo, noteEn; }

        static Entry E(string ko, string en, string noteKo, string noteEn) => new Entry { ko = ko, en = en, noteKo = noteKo, noteEn = noteEn };

        static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>
        {
            // ---- Player ----
            ["Pawn"] = E("일꾼", "Pawn", "나무를 베어 골드를 벌어요. 싸움은 못 하지만 왕국 살림은 이 친구들 몫이에요.", "Chops wood to earn gold. Can't fight, but the kingdom runs on their hard work."),
            ["Warrior"] = E("전사", "Warrior", "칼과 방패를 든 기본 병사. 싸고 빨리 나와서 언제나 든든해요.", "The basic soldier with sword and shield. Cheap, quick to call, always reliable."),
            ["Archer"] = E("궁수", "Archer", "멀리서 화살을 쏴요. 앞줄이 버텨 주면 누구보다 많은 적을 쓰러뜨려요.", "Shoots from afar. With a strong front line, nobody takes down more enemies."),
            ["Lancer"] = E("창병", "Lancer", "긴 창과 두꺼운 갑옷. 맨 앞에서 적을 막아 주는 든든한 방패예요.", "Long spear, thick armor. The sturdy wall that holds the front line."),
            ["Monk"] = E("수도사", "Monk", "빛을 터뜨려 주변 적은 다치게 하고 아군은 치료해요. 지휘관의 오랜 친구예요.", "Bursts holy light that hurts nearby enemies and heals allies. The Commander's old friend."),

            // ---- Chapter 1 ----
            ["Torch Goblin"] = E("횃불 고블린", "Torch Goblin", "횃불을 휘두르는 장난꾸러기. 빵 냄새만 맡으면 눈이 반짝여요.", "A torch-swinging troublemaker whose eyes light up at the smell of bread."),
            ["Spear Goblin"] = E("창 고블린", "Spear Goblin", "창을 든 고블린. 키가 작아서 창도 짧게 잘라 썼대요.", "A goblin with a spear. Cut it shorter to match his height."),
            ["Snake"] = E("뱀", "Snake", "트롤의 저주로 조종당하는 숲의 뱀. 쓰러뜨리면 저주가 풀려 숲으로 돌아가요.", "A forest snake controlled by the Troll's curse. Defeat it and it slithers home, free."),
            ["Spider"] = E("거미", "Spider", "저주받은 거미. 원래는 이슬 맺힌 거미줄을 자랑하던 얌전한 친구예요.", "A cursed spider. Normally a quiet fellow proud of its dewy webs."),
            ["Hex Shaman"] = E("해골 주술사", "Hex Shaman", "1장 보스. 동물을 조종하는 '짐승의 왕관'을 만들던 주술사예요. 멀리서 저주를 날려요.", "Chapter 1 boss. The shaman who forged the Beast Crown. Hurls curses from afar."),

            // ---- Chapter 2 ----
            ["Gnome"] = E("노움", "Gnome", "돈만 주면 누구 편이든 드는 장사꾼 노움. 지금은 트롤 왕이 단골손님이에요.", "A gnome who fights for whoever pays. The Troll King is his best customer."),
            ["Slingshot Gnome"] = E("새총 노움", "Slingshot Gnome", "멀리서 돌멩이를 쏘는 노움. 빠른 병사로 먼저 잡는 게 좋아요.", "Pelts stones from afar. Send fast soldiers after it first."),
            ["Bumblebee"] = E("호박벌", "Bumblebee", "저주받은 호박벌. 빠르게 날아와 따끔하게 쏘아요.", "A cursed bumblebee. Zooms in fast and stings hard."),
            ["Bear"] = E("곰", "Bear", "저주받은 곰. 꿀 대신 기사들을 노려요. 맷집이 아주 좋아요.", "A cursed bear that wants knights instead of honey. Very tough."),
            ["Panda"] = E("판다 수도승", "Panda Monk", "2장 보스. 저주에 걸린 대나무 언덕의 수도승. 저주가 풀린 뒤에는 우리 길잡이가 되어 줘요.", "Chapter 2 boss. The cursed monk of the Bamboo Hills, who becomes our guide once freed."),

            // ---- Chapter 3 ----
            ["Paddle Shark"] = E("노 젓는 상어", "Paddle Shark", "노를 무기로 휘두르는 상어 해적. 배는 어디 두고 왔는지 아무도 몰라요.", "A shark pirate who swings his oar. Nobody knows where he left the boat."),
            ["Harpoon Shark"] = E("작살 상어", "Harpoon Shark", "작살을 던지는 상어 해적. 멀리서도 정확해요.", "A harpoon-throwing shark pirate. Deadly accurate from afar."),
            ["Bomb Fish"] = E("폭탄 물고기", "Bomb Fish", "폭탄을 던지는 물고기. 공격은 느리지만 한 방이 아파요.", "A fish that throws bombs. Slow, but each blast really hurts."),
            ["Lizard"] = E("도마뱀 전사", "Lizard", "빠르고 날쌘 도마뱀 전사. 협곡 바위 사이를 누비며 싸워요.", "A quick, nimble lizard warrior darting between canyon rocks."),
            ["Turtle"] = E("거북이", "Turtle", "저주받은 거북이. 등껍질이 단단해서 쉽게 쓰러지지 않아요.", "A cursed turtle. Its shell makes it very hard to knock down."),
            ["Minotaur"] = E("미노타우로스", "Minotaur", "3장 보스. 협곡의 관문을 지키는 소머리 전사. 돌망치 한 방이 정말 무서워요.", "Chapter 3 boss. The bull-headed guard of the canyon gate. Its stone hammer hits terribly hard."),

            // ---- Chapter 4 ----
            ["Skull"] = E("해골 전사", "Skeleton", "트롤의 땅을 지키는 해골 병사. 뼈가 덜그럭거리는 소리로 먼저 알 수 있어요.", "A skeleton soldier of the Troll Lands. You hear its rattling bones first."),
            ["Giant Bat"] = E("큰 박쥐", "Giant Bat", "저주받은 큰 박쥐. 아주 빠르게 날아들어요.", "A cursed giant bat that swoops in very fast."),
            ["Imp"] = E("임프", "Imp", "불꽃처럼 성질 급한 작은 악마. 공격이 매서워요.", "A hot-tempered little devil with fierce attacks."),
            ["Gnoll"] = E("놀", "Gnoll", "멀리서 뼈 창을 던지는 하이에나 전사.", "A hyena warrior that throws bone spears from afar."),
            ["Thief"] = E("도적", "Thief", "재빠른 도적. 한 번 찌르면 아프니 조심하세요.", "A quick thief whose stabs really hurt."),
            ["Troll"] = E("트롤 왕", "Troll King", "최종 보스. '짐승의 왕관'으로 동물들을 조종한 장본인. 거대한 몽둥이를 휘둘러요.", "Final boss. The one who used the Beast Crown to control the animals. Swings a giant club."),
        };

        public static void MarkSeen(UnitData d)
        {
            if (d == null || d.displayName == null) return;
            string key = "seen_" + d.displayName;
            if (PlayerPrefs.GetInt(key, 0) == 1) return;
            PlayerPrefs.SetInt(key, 1);
            Achievements.MarkDirty();
        }

        static readonly HashSet<string> PlayerNames = new HashSet<string> { "Pawn", "Warrior", "Archer", "Lancer", "Monk" };
        static bool IsPlayerUnit(UnitData d) => PlayerNames.Contains(d.displayName);

        public static bool IsOpen(UnitData d)
        {
            if (d == null) return false;
            if (IsPlayerUnit(d)) return d.type == UnitType.Pawn || UnitProgress.IsUnlocked(d.type);
            return PlayerPrefs.GetInt("seen_" + d.displayName, 0) == 1;
        }

        static string Name(UnitData d) => Entries.TryGetValue(d.displayName, out var e) ? Loc.T(e.en, e.ko) : d.displayName;
        static string Note(UnitData d) => Entries.TryGetValue(d.displayName, out var e) ? Loc.T(e.noteEn, e.noteKo) : "";

        /// <summary>First chapter where this monster shows up (0 = player unit / unknown).</summary>
        static int ChapterOf(UnitData d)
        {
            var skin = UISkin.Instance;
            if (skin == null || skin.catalog == null || IsPlayerUnit(d)) return 0;
            var stages = skin.catalog.stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                if (s == null) continue;
                if (s.boss == d) return i / 6 + 1;
                if (s.enemyUnits != null)
                    foreach (var c in s.enemyUnits) if (c.unit == d) return i / 6 + 1;
            }
            return 0;
        }

        // ---------------- Window ----------------

        static GameObject window;
        static Image bigIcon;
        static Text detailName, detailInfo, detailNote;

        public static void Open()
        {
            if (window != null) return;
            var skin = UISkin.Instance;
            UnitData[] units = skin != null && skin.codexUnits != null ? skin.codexUnits : new UnitData[0];
            int open = 0;
            foreach (var u in units) if (IsOpen(u)) open++;

            window = UIKit.Window("Codex", Loc.T("CODEX", "도감"), new Vector2(1560f, 820f), Close, out RectTransform paper);
            UIKit.Txt(paper, "Count", Loc.T($"Found {open} / {units.Length}", $"발견 {open} / {units.Length}"), 28, TextAnchor.MiddleLeft,
                new Vector2(-470f, 330f), new Vector2(500f, 44f), true);

            // Card grid on the left
            const int cols = 6;
            const float cw = 150f, ch = 170f, gap = 12f;
            var content = UIKit.ScrollArea(paper, new Vector2(-260f, -35f), new Vector2(cols * (cw + gap) + 10f, 640f), out ScrollRect _);
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (u == null) continue;
                int col = i % cols, row = i / cols;
                var card = UIKit.Rect("Card " + u.displayName, content, new Vector2(0.5f, 1f),
                    new Vector2((col - (cols - 1) / 2f) * (cw + gap), -ch / 2f - 6f - row * (ch + gap)), new Vector2(cw, ch));
                var bg = UIKit.Img(card, skin != null ? skin.banner : null, 3f, true);
                bool isOpen = IsOpen(u);
                if (u.isBoss) bg.color = new Color(1f, 0.85f, 0.75f, 1f);

                var pic = UIKit.Rect("Icon", card, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(110f, 110f));
                var picImg = UIKit.Img(pic, u.icon, 1f);
                picImg.type = Image.Type.Simple;
                picImg.preserveAspect = true;
                if (!isOpen) picImg.color = new Color(0f, 0f, 0f, 0.85f); // silhouette
                UIKit.Txt(card, "Name", isOpen ? Name(u) : "???", 22, TextAnchor.MiddleCenter, new Vector2(0f, -56f), new Vector2(cw - 10f, 40f), true);

                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                var unit = u;
                b.onClick.AddListener(() => ShowDetail(unit));
            }
            int rows = (units.Length + cols - 1) / cols;
            UIKit.SetContentHeight(content, rows * (ch + gap) + 20f);

            // Detail panel on the right
            var detail = UIKit.Rect("Detail", paper, new Vector2(0.5f, 0.5f), new Vector2(510f, -35f), new Vector2(480f, 640f));
            UIKit.Img(detail, skin != null ? skin.banner : null, 3f);
            var big = UIKit.Rect("Big Icon", detail, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(220f, 220f));
            bigIcon = UIKit.Img(big, null, 1f);
            bigIcon.type = Image.Type.Simple;
            bigIcon.preserveAspect = true;
            detailName = UIKit.Txt(detail, "Name", "", 40, TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(440f, 56f), true);
            detailInfo = UIKit.Txt(detail, "Info", "", 26, TextAnchor.MiddleCenter, new Vector2(0f, -45f), new Vector2(440f, 90f), true);
            detailNote = UIKit.Txt(detail, "Note", "", 26, TextAnchor.UpperCenter, new Vector2(0f, -190f), new Vector2(420f, 190f), true);

            UnitData first = null;
            foreach (var u in units) if (u != null && IsOpen(u)) { first = u; break; }
            ShowDetail(first);
        }

        static void ShowDetail(UnitData u)
        {
            if (bigIcon == null) return;
            if (u == null || !IsOpen(u))
            {
                bigIcon.sprite = u != null ? u.icon : null;
                bigIcon.color = new Color(0f, 0f, 0f, u != null ? 0.85f : 0f);
                detailName.text = "???";
                detailInfo.text = "";
                detailNote.text = Loc.T("Meet it in battle to fill in this page.", "전투에서 만나면 이 페이지가 채워져요.");
                return;
            }
            bigIcon.sprite = u.icon;
            bigIcon.color = Color.white;
            detailName.text = Name(u) + (u.isBoss ? Loc.T("  (BOSS)", "  (보스)") : "");
            int chapter = ChapterOf(u);
            string where = IsPlayerUnit(u) ? Loc.T("Our kingdom", "우리 왕국") : chapter > 0 ? StoryScript.ChapterLabel(chapter) + " " + StoryScript.ChapterName(chapter) : "";
            string range = u.attackRange >= 2f ? Loc.T("Ranged", "원거리") : Loc.T("Melee", "근접");
            detailInfo.text = where + "\n" + Loc.T($"HP {u.maxHp:0}   ATK {u.attack:0}   {range}", $"체력 {u.maxHp:0}   공격 {u.attack:0}   {range}");
            detailNote.text = Note(u);
        }

        public static void Close()
        {
            if (window != null) Object.Destroy(window);
            window = null;
            bigIcon = null;
        }
    }
}
