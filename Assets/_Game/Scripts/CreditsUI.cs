using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Credits window (opened from Settings). Lists every third-party asset and its license.</summary>
    public static class CreditsUI
    {
        static string Body => Loc.Korean ? BodyKo : BodyEn;

        const string BodyKo =
@"<b>그래픽</b>
Tiny Swords / Tiny Swords Enemy Pack
Pixel Frog  (pixelfrog-assets.itch.io)

<b>음악</b>
Pixel Fantasy 30 Tracks Music Pack
alkakrab  (alkakrab.itch.io)

<b>효과음</b>
RPG Sounds - Kenney (kenney.nl), CC0
RPG Sound Pack - artisticdude (OpenGameArt), CC0
RPG Essentials SFX - Leohpaz (leohpaz.itch.io)
Fantasy Ambient Sound Effects Pack - kmontesdev (itch.io), CC0

<b>글꼴</b>
Galmuri11
Copyright © 2019–2025 Lee Minseo (quiple@quiple.dev)
SIL Open Font License, Version 1.1 에 따라 사용합니다.
https://openfontlicense.org

멋진 에셋을 만들어 주신 모든 분께 감사드립니다.";

        const string BodyEn =
@"<b>Graphics</b>
Tiny Swords / Tiny Swords Enemy Pack
Pixel Frog  (pixelfrog-assets.itch.io)

<b>Music</b>
Pixel Fantasy 30 Tracks Music Pack
alkakrab  (alkakrab.itch.io)

<b>Sound effects</b>
RPG Sounds - Kenney (kenney.nl), CC0
RPG Sound Pack - artisticdude (OpenGameArt), CC0
RPG Essentials SFX - Leohpaz (leohpaz.itch.io)
Fantasy Ambient Sound Effects Pack - kmontesdev (itch.io), CC0

<b>Font</b>
Galmuri11
Copyright © 2019–2025 Lee Minseo (quiple@quiple.dev)
Licensed under the SIL Open Font License, Version 1.1.
https://openfontlicense.org

Thank you to everyone who made these wonderful assets.";

        public static void Open()
        {
            UIKit.Window("Credits", Loc.T("CREDITS", "크레딧"), new Vector2(1100f, 760f), null, out RectTransform paper);
            var content = UIKit.ScrollArea(paper, new Vector2(0f, -20f), new Vector2(980f, 600f), out ScrollRect _);
            var t = UIKit.Txt(content, "Body", Body, 30, TextAnchor.UpperCenter, Vector2.zero, new Vector2(940f, 1200f), true);
            t.supportRichText = true;
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(940f, t.preferredHeight + 20f);
            UIKit.SetContentHeight(content, t.preferredHeight + 40f);
        }
    }
}
