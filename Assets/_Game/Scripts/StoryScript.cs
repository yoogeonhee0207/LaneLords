using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    public struct StoryLine
    {
        public string speaker;
        public string ko;
        public string en;
        public StoryLine(string speaker, string ko, string en) { this.speaker = speaker; this.ko = ko; this.en = en; }
    }

    /// <summary>
    /// All story text. Intro lines play before a stage (first visit only), outro lines after the first clear of a boss stage.
    /// Korean is shown when the device language is Korean, English otherwise.
    /// Speaker keys must match StoryDialogUI.speakerKeys (portraits are assigned by the scene builder).
    /// </summary>
    public static class StoryScript
    {
        public static bool Korean => Loc.Korean;

        /// <summary>Speakers drawn on the left (our side). Everyone else is on the right.</summary>
        public static bool IsAlly(string speaker) => speaker == "monk" || speaker == "panda_guide" || speaker == "commander"
            || speaker == "snake" || speaker == "bear" || speaker == "turtle" || speaker == "bat"; // freed animals

        public static string SpeakerName(string speaker)
        {
            bool ko = Korean;
            switch (speaker)
            {
                case "monk": return ko ? "수도사" : "Monk";
                case "commander": return ko ? "지휘관" : "Commander";
                case "panda": case "panda_guide": return ko ? "판다" : "Panda";
                case "shaman": return ko ? "해골 주술사" : "Hex Shaman";
                case "goblin": return ko ? "고블린" : "Goblin";
                case "gnome": return ko ? "노움" : "Gnome";
                case "shark": return ko ? "상어 해적" : "Shark Pirate";
                case "minotaur": return ko ? "미노타우로스" : "Minotaur";
                case "skull": return ko ? "해골 전사" : "Skeleton";
                case "troll": return ko ? "트롤 왕" : "Troll King";
                case "snake": return ko ? "뱀" : "Snake";
                case "bear": return ko ? "곰" : "Bear";
                case "turtle": return ko ? "거북이" : "Turtle";
                case "bat": return ko ? "큰 박쥐" : "Giant Bat";
            }
            return speaker;
        }

        static StoryLine L(string s, string ko, string en) => new StoryLine(s, ko, en);

        public static string ChapterName(int chapter)
        {
            switch (chapter)
            {
                case 1: return Loc.T("Dark Forest", "어둠의 숲");
                case 2: return Loc.T("Bamboo Hills", "대나무 언덕");
                case 3: return Loc.T("Shore Canyon", "물가와 협곡");
                case 4: return Loc.T("Troll Lands", "트롤의 땅");
            }
            return "";
        }

        /// <summary>"Chapter 2" / "제2장" for the title card.</summary>
        public static string ChapterLabel(int chapter) => Loc.Korean ? $"제{chapter}장" : $"Chapter {chapter}";

        // Intro: before a stage (first visit). 1-1 opens with the prologue.
        public static readonly Dictionary<string, StoryLine[]> Intro = new Dictionary<string, StoryLine[]>
        {
            // ---------- Chapter 1: Dark Forest ----------
            ["1-1"] = new[] {
                L("monk", "지휘관님, 큰일이에요! 왕국 곳곳에서 동물들이 갑자기 사나워졌어요.", "Commander, trouble! Animals all over the kingdom have gone wild."),
                L("monk", "얌전하던 다람쥐가 빵집을 털었다는 소문까지 돌아요.", "Rumor says a sweet little squirrel robbed a bakery."),
                L("monk", "게다가 국경 숲에서 고블린들까지 쳐들어오고 있어요!", "And now goblins are marching out of the border forest!"),
                L("goblin", "키히히! 숲은 이제 우리 거다! 빵도 우리 거다!", "Heehee! The forest is ours! The bread too!") },
            ["1-2"] = new[] {
                L("monk", "뱀들 눈이 보랏빛이에요. 누가 동물들에게 몹쓸 마법을 걸었나 봐요.", "The snakes' eyes glow purple. Someone put a nasty spell on the animals."),
                L("monk", "쓰러뜨리면 저주가 풀려요. 걱정 마세요, 동물들은 다치지 않아요.", "Defeating them breaks the curse. Don't worry, the animals won't be hurt.") },
            ["1-3"] = new[] {
                L("goblin", "거미들아, 기사들을 꽁꽁 묶어 버려! 선물 포장처럼!", "Spiders, wrap those knights up! Like presents!"),
                L("monk", "상점에서 궁수를 해금했다면 지금 써 보세요.", "If you unlocked Archers in the shop, now is the time.") },
            ["1-4"] = new[] {
                L("goblin", "주술사님이 그랬어. 왕관만 완성되면 숲 전체가 우리 편이래!", "The Shaman said once the crown is done, the whole forest is ours!"),
                L("monk", "왕관...? 무슨 왕관일까요. 불길한 예감이 들어요.", "A crown...? I have a bad feeling about this.") },
            ["1-5"] = new[] {
                L("monk", "숲 깊은 곳에서 수상한 보라색 연기가 피어올라요.", "Strange purple smoke is rising deep in the forest."),
                L("goblin", "주술사님 실험실은 출입 금지야! ...아차, 말해 버렸다.", "The Shaman's lab is off-limits! ...Oops, I said that out loud.") },
            ["1-6"] = new[] {
                L("monk", "주술사의 실험실이 코앞이에요. 숲의 저주를 끝내러 가요!", "The Shaman's lab is right ahead. Let's end the forest's curse!"),
                L("monk", "보스는 천천히 다가와요. 그동안 일꾼과 병사를 모아 두세요.", "Bosses walk slowly. Build up workers and soldiers while it comes.") },

            // ---------- Chapter 2: Bamboo Hills ----------
            ["2-1"] = new[] {
                L("monk", "대나무 언덕이에요. 공기가 맑아서 좋네요... 벌 떼만 아니면요.", "The Bamboo Hills. Lovely fresh air... minus the bees."),
                L("gnome", "어서 오세요, 손님! 통행료는 금화 100개! 없으면 벌침 100방!", "Welcome, customers! Toll is 100 gold! No gold? 100 bee stings!") },
            ["2-2"] = new[] {
                L("gnome", "트롤 왕이 돈을 두둑이 줬거든. 새총 부대, 영업 시작!", "The Troll King pays well. Slingshot squad, open for business!"),
                L("monk", "멀리서 쏘는 적은 빠른 병사로 먼저 처리하세요.", "Send fast soldiers after the ranged enemies first.") },
            ["2-3"] = new[] {
                L("monk", "곰까지 조종당하고 있어요... 꿀 대신 우리를 노리네요.", "Even the bears are cursed... and they want us instead of honey."),
                L("monk", "창병으로 막고 뒤에서 공격하세요.", "Block with Lancers and attack from behind.") },
            ["2-4"] = new[] {
                L("gnome", "판다님도 우리 편이 되셨지! 정확히는 왕관 편이지만!", "The Panda is on our side now! Well, the crown's side!"),
                L("monk", "언덕의 수호자 판다까지... 서둘러야겠어요.", "Even the Panda, guardian of the hills... We must hurry.") },
            ["2-5"] = new[] {
                L("monk", "언덕 꼭대기에서 엄청난 기운이 느껴져요.", "I can feel a huge presence at the top of the hill."),
                L("monk", "판다는 나쁜 존재가 아니에요. 저주만 풀어 주면 돼요.", "The Panda isn't evil. We just need to break the curse.") },
            ["2-6"] = new[] {
                L("monk", "판다가 있는 정상이에요. 대나무가 전부 쓰러져 있네요...", "We've reached the summit. All the bamboo is knocked down..."),
                L("monk", "판다는 힘이 세요. 창병으로 막고 궁수로 깎아요!", "The Panda is strong. Hold it with Lancers and wear it down with Archers!") },

            // ---------- Chapter 3: Shore Canyon ----------
            ["3-1"] = new[] {
                L("panda_guide", "바닷가 협곡이오. 짠내가 나는구려.", "The seaside canyon. Smells like salt."),
                L("shark", "육지 놈들이 바다까지 오셨네? 입장료는 너희 전부다!", "Landlubbers at the sea? The entry fee is... all of you!") },
            ["3-2"] = new[] {
                L("shark", "작살 부대, 던져라! 오늘 저녁은 기사 구이다!", "Harpoons, throw! Grilled knight for dinner!"),
                L("panda_guide", "상어들은 트롤에게 고용된 해적이오. 봐줄 필요 없소.", "The sharks are pirates hired by the Troll. No mercy needed.") },
            ["3-3"] = new[] {
                L("panda_guide", "저 거북이들은 저주에 걸려 방패로 쓰이고 있소. 불쌍하구려.", "Those turtles are cursed and used as shields. Poor things."),
                L("panda_guide", "아주 단단하니 시간을 들여 무너뜨리시오.", "They are very tough. Wear them down.") },
            ["3-4"] = new[] {
                L("shark", "폭탄 물고기 발사! 다 날려 버려!", "Bomb fish, launch! Blow 'em all up!"),
                L("panda_guide", "폭탄은 크게 터지오. 병사를 한곳에 몰지 마시오.", "Bombs hit hard. Don't bunch your soldiers together.") },
            ["3-5"] = new[] {
                L("panda_guide", "협곡의 관문이 보이오. 미노타우로스가 지키고 있소.", "I see the canyon gate. The Minotaur guards it."),
                L("panda_guide", "그자는 저주받지 않았소. 스스로 트롤 왕을 따르는 장군이오.", "He isn't cursed. He serves the Troll King by choice.") },
            ["3-6"] = new[] {
                L("panda_guide", "관문 앞이오. 미노타우로스는 한 방이 무섭소.", "We're at the gate. The Minotaur hits terribly hard."),
                L("panda_guide", "창병으로 버티고 궁수로 뒤에서 쏘시오!", "Hold with Lancers and shoot from behind with Archers!") },

            // ---------- Chapter 4: Troll Lands ----------
            ["4-1"] = new[] {
                L("panda_guide", "트롤의 땅이오. 풀 한 포기 없이 메말랐구려.", "The Troll Lands. Not a single blade of green."),
                L("skull", "살아 있는 자들이여... 돌아가라... 아니면 우리 친구가 되든지...", "Living ones... turn back... or stay and be our friends...") },
            ["4-2"] = new[] {
                L("panda_guide", "놀들은 뼈를 던지오. 거리를 좁혀 상대하시오.", "Gnolls throw bones. Close the distance."),
                L("skull", "그거 내 뼈야... 돌려줘...", "That's my bone... give it back...") },
            ["4-3"] = new[] {
                L("skull", "임프들이 깨어났다... 아주 시끄러운 녀석들이지...", "The imps are awake... noisy little things..."),
                L("panda_guide", "박쥐들도 저주에 걸렸소. 풀어 주어야 하오.", "The bats are cursed too. We must free them.") },
            ["4-4"] = new[] {
                L("panda_guide", "도적들이 어둠 속에서 노리고 있소. 지갑 조심하시오.", "Thieves lurk in the shadows. Watch your wallet.") },
            ["4-5"] = new[] {
                L("panda_guide", "들은 바로는 트롤 왕은 옛날 왕국에서 쫓겨났다고 하오.", "They say the Troll King was once banished from the kingdom."),
                L("panda_guide", "그 원한으로 왕관을 쓴 모양이오. 요새가 바로 앞이오.", "That grudge is why he took the crown. His fortress is just ahead.") },
            ["4-6"] = new[] {
                L("panda_guide", "마지막 싸움이오. 모든 동물의 운명이 여기 달렸소.", "The final battle. Every animal's fate rests on this."),
                L("monk", "지휘관님, 여기까지 오셨으니 꼭 이길 거예요!", "Commander, you've come this far. We will win!") },
        };

        // Boss entrance: when the boss walks out (game paused).
        public static readonly Dictionary<string, StoryLine[]> BossEntry = new Dictionary<string, StoryLine[]>
        {
            ["1-6"] = new[] {
                L("shaman", "누가 내 실험을 방해하느냐! 짐승의 왕관은 거의 완성됐단 말이다!", "Who dares disturb my work? The Beast Crown is nearly finished!"),
                L("shaman", "숲의 짐승들아, 저 깡통 기사들을 혼내 줘라!", "Beasts of the forest, teach these tin-can knights a lesson!"),
                L("monk", "짐승의 왕관... 그게 동물들을 조종하는 물건이군요!", "The Beast Crown... that's what controls the animals!") },
            ["2-6"] = new[] {
                L("panda", "크르르... 대나무... 아니... 침입자... 막는다...", "Grrr... bamboo... no... intruders... must stop..."),
                L("panda", "머리가... 왕관 소리가 머릿속에서 울리오...", "My head... the crown's voice keeps ringing..."),
                L("monk", "저주에 걸렸어요! 쓰러뜨려서 깨워 줘요!", "It's cursed! Knock it down to wake it up!") },
            ["3-6"] = new[] {
                L("minotaur", "멈춰라! 이 관문은 아무도 못 지나간다!", "Halt! No one passes this gate!"),
                L("minotaur", "나는 왕관 따위 필요 없다. 내 도끼면 충분하지!", "I don't need any crown. My axe is enough!"),
                L("panda_guide", "역시 고집불통이구려. 정면으로 부딪쳐 봅시다!", "Stubborn as ever. Let's meet him head-on!") },
            ["4-6"] = new[] {
                L("troll", "감히 내 요새까지 왔느냐! 왕국이 나를 쫓아낸 날을 잊지 않았다!", "You dare enter my fortress?! I never forgot the day the kingdom banished me!"),
                L("troll", "이 왕관으로 짐승도 몬스터도 모두 내 것이다! 이제 왕국 차례다!", "With this crown, every beast and monster is mine! The kingdom is next!"),
                L("panda_guide", "왕관을 쓴 트롤 왕이오! 저 녀석만 쓰러뜨리면 끝이오!", "There's the crowned Troll King! Beat him and it's over!") },
        };

        // Outro: after the first clear of a boss stage.
        public static readonly Dictionary<string, StoryLine[]> Outro = new Dictionary<string, StoryLine[]>
        {
            ["1-6"] = new[] {
                L("shaman", "크윽... 늦었다... 왕관은 이미 트롤 왕께 보냈지...", "Argh... too late... I already sent the crown to the Troll King..."),
                L("snake", "쉬익... 머리가 맑아졌어. 고마워, 기사들.", "Hsss... my head is clear. Thank you, knights."),
                L("monk", "트롤 왕이 왕관을 쓰고 있다면, 그를 막아야 해요.", "If the Troll King wears the crown, we have to stop him."),
                L("monk", "흔적이 대나무 언덕 쪽으로 이어져요. 출발해요!", "The trail leads to the Bamboo Hills. Let's go!") },
            ["2-6"] = new[] {
                L("panda", "...여기가 어디요? 내 대나무 밭이 왜 이 꼴이오?", "...Where am I? What happened to my bamboo?"),
                L("bear", "판다 형님, 정신 차리셨군요! 저도 방금 깼어요.", "Panda, you're back! I just woke up too."),
                L("panda_guide", "고맙소, 기사들. 왕관을 쓴 자를 쓰러뜨리면 모든 저주가 풀리오.", "Thank you, knights. Defeat whoever wears the crown and every curse breaks."),
                L("panda_guide", "트롤 왕에게 가려면 바닷가 협곡을 지나야 하오. 내가 길을 안내하겠소.", "To reach the Troll King we must cross the seaside canyon. I'll guide you.") },
            ["3-6"] = new[] {
                L("minotaur", "훌륭하다... 오랜만에 제대로 된 싸움이었다.", "Well fought... a real battle, at last."),
                L("minotaur", "하지만 왕께서는 나보다 훨씬 강하시다. 각오해라.", "But my king is far stronger than me. Be ready."),
                L("turtle", "등껍질이 가벼워졌어요! 이제 바다로 돌아갈래요.", "My shell feels light again! Back to the sea for me."),
                L("panda_guide", "관문이 열렸소. 이제 트롤의 땅이오.", "The gate is open. Next, the Troll Lands.") },
            ["4-6"] = new[] {
                L("troll", "말도 안 돼... 내 왕관이... 금이 가잖아...", "Impossible... my crown... it's cracking..."),
                L("bat", "끼익! 앞이 보인다! 아니, 원래 안 보였는데 아무튼 기분 좋다!", "Eeek! I can see! Well, I never could, but I feel great!"),
                L("panda_guide", "왕관이 부서졌소. 모든 동물의 저주가 풀렸구려.", "The crown is broken. Every curse is lifted.") },
        };

        // Ending: after the 4-6 outro (first clear only), then "THE END".
        public static readonly StoryLine[] Ending =
        {
            L("monk", "동물들이 하나둘 집으로 돌아가고 있어요.", "The animals are heading home, one by one."),
            L("monk", "빵집 다람쥐도 사과 편지를 보냈대요. 빵값도 갚겠다고요.", "Even the bakery squirrel sent an apology letter. It'll pay for the bread."),
            L("panda_guide", "나도 대나무 언덕으로 돌아가겠소. 대나무를 다시 심어야 하니.", "I'll return to the Bamboo Hills too. I have bamboo to replant."),
            L("panda_guide", "언젠가 또 도움이 필요하면 부르시오, 지휘관.", "If you ever need help again, call me, Commander."),
            L("monk", "지휘관님, 수고 많으셨어요. 왕국에 평화가 돌아왔어요!", "Great work, Commander. Peace has returned to the kingdom!"),
        };
    }
}
