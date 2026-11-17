using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>
    /// Sprites and font for windows that are built in code at runtime (daily reward, achievements, codex, credits).
    /// The scene builder puts one on each scene's canvas.
    /// </summary>
    public class UISkin : MonoBehaviour
    {
        public static UISkin Instance { get; private set; }

        public Font font;
        public Sprite paper;
        public Sprite ribbon;
        public Sprite blueButton;
        public Sprite redButton;
        public Sprite badge;
        public Sprite banner;
        [Tooltip("Units shown in the codex, in order (player units first)")]
        public UnitData[] codexUnits;
        public StageCatalog catalog;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>Small helpers that build UGUI in code, in the same look as the scene builder.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color32(70, 45, 30, 255);
        public static readonly Color Green = new Color32(40, 110, 60, 255);

        static UISkin S => UISkin.Instance;

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(RectTransform rt, Sprite sprite, float ppu = 1.5f, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppu;
            }
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, TextAnchor align, Vector2 pos, Vector2 box, bool ink = false)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = S != null ? S.font : null;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (ink) t.color = Ink;
            else
            {
                t.color = Color.white;
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.85f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        /// <summary>Button with a label. Optional sound key plays on click (none by default).</summary>
        public static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, bool red, UnityAction onClick,
            int fontSize = 34, string sound = null)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), pos, size);
            var img = Img(rt, S != null ? (red ? S.redButton : S.blueButton) : null, 2.5f, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            b.colors = colors;
            Txt(rt, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0f, 4f), size);
            b.onClick.AddListener(() => { if (sound != null) Sfx.Play(sound); });
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        /// <summary>Full-screen dimmed window with a paper sheet, a ribbon title and a CLOSE button.</summary>
        public static GameObject Window(string name, string title, Vector2 paperSize, UnityAction onClose, out RectTransform paper)
        {
            Transform root = CanvasRoot();
            var dim = Stretch(name, root);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.6f);

            paper = Rect("Paper", dim, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), paperSize);
            Img(paper, S != null ? S.paper : null, 1.2f, true);

            var rib = Rect("Ribbon", dim, new Vector2(0.5f, 0.5f), new Vector2(0f, paperSize.y / 2f - 10f), new Vector2(Mathf.Min(760f, paperSize.x), 190f));
            Img(rib, S != null ? S.ribbon : null, 1f);
            Txt(rib, "Title", title, 56, TextAnchor.MiddleCenter, new Vector2(0f, 14f), new Vector2(640f, 110f));

            Btn(dim, "Close", Loc.T("CLOSE", "닫기"), new Vector2(0f, -paperSize.y / 2f - 20f), new Vector2(260f, 80f), true,
                onClose ?? (() => Object.Destroy(dim.gameObject)), 34, null);
            dim.SetAsLastSibling();
            return dim.gameObject;
        }

        /// <summary>Vertical scroll area inside a parent. Returns the content transform (children stack from the top).</summary>
        public static RectTransform ScrollArea(Transform parent, Vector2 pos, Vector2 size, out ScrollRect scroll)
        {
            var view = Rect("Scroll", parent, new Vector2(0.5f, 0.5f), pos, size);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f); // catches drags
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var content = Rect("Content", view, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size.x, 0f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            scroll.content = content;
            scroll.viewport = view;
            return content;
        }

        /// <summary>Sets the scroll content height after children are placed by hand.</summary>
        public static void SetContentHeight(RectTransform content, float height)
        {
            content.sizeDelta = new Vector2(0f, height);
        }

        public static Transform CanvasRoot()
        {
            if (S != null) return S.transform;
            var c = Object.FindAnyObjectByType<Canvas>();
            return c != null ? c.transform : null;
        }

        public static GameObject Badge(Transform parent)
        {
            var rt = Rect("Badge", parent, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(46f, 46f));
            Img(rt, S != null ? S.badge : null, 1f);
            Txt(rt, "Mark", "!", 30, TextAnchor.MiddleCenter, new Vector2(0f, 3f), new Vector2(46f, 46f));
            return rt.gameObject;
        }
    }

    /// <summary>Game name and version shown on the title screen.</summary>
    public static class GameInfo
    {
        public const string Version = "v1.0";
        public const string Name = "Lane Lords";
        public static string Subtitle => Loc.T("The Cursed Kingdom", "저주받은 왕국");
        public static string Title => Name + ": " + Subtitle;
    }
}
