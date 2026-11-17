using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>Where the fill sits inside a Tiny Swords bar frame (in frame-sprite units, pivot = centre).</summary>
    public struct BarLayout
    {
        public float left;       // left edge of the inner slot
        public float width;      // width of the inner slot
        public float fillY;      // vertical offset of the fill
        public float fillScaleY; // vertical scale of the fill sprite

        // Measured from the pixels of BigBar_Base / SmallBar_Base (64 px per unit).
        public static readonly BarLayout Big = new BarLayout { left = -0.703f, width = 1.406f, fillY = 0f, fillScaleY = 1f };
        public static readonly BarLayout Small = new BarLayout { left = -0.578f, width = 1.156f, fillY = -2f / 64f, fillScaleY = 2f };
    }

    /// <summary>A frame + fill bar built from sprites. Falls back to plain rectangles if sprites are missing.</summary>
    public class FramedBar
    {
        Transform fill;
        BarLayout layout;
        bool fillPivotLeft;

        public static FramedBar Build(Transform root, Sprite frame, Sprite fillSprite, BarLayout layout, int order, Color fallbackColor)
        {
            var bar = new FramedBar { layout = layout };
            if (frame != null && fillSprite != null)
            {
                MakeSprite(root, "Frame", frame, Color.white, Vector3.zero, Vector3.one, order);
                bar.fill = MakeSprite(root, "Fill", fillSprite, Color.white, Vector3.zero, Vector3.one, order + 1);
                bar.fillPivotLeft = false;
            }
            else
            {
                // Plain fallback: dark background + coloured fill, same footprint as the slot.
                var plain = new BarLayout { left = layout.left, width = layout.width, fillY = 0f, fillScaleY = 0.25f };
                bar.layout = plain;
                MakeSprite(root, "Bg", HealthBar.WhiteSprite, new Color(0f, 0f, 0f, 0.6f),
                    new Vector3(plain.left - 0.05f, 0f, 0f), new Vector3(plain.width + 0.1f, plain.fillScaleY + 0.1f, 1f), order);
                bar.fill = MakeSprite(root, "Fill", HealthBar.WhiteSprite, fallbackColor, Vector3.zero, Vector3.one, order + 1);
                bar.fillPivotLeft = true;
            }
            bar.Set(1f);
            return bar;
        }

        static Transform MakeSprite(Transform parent, string name, Sprite sprite, Color color, Vector3 pos, Vector3 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        public void Set(float ratio)
        {
            float w = layout.width * Mathf.Clamp01(ratio);
            float x = fillPivotLeft ? layout.left : layout.left + w * 0.5f;
            fill.localPosition = new Vector3(x, layout.fillY, 0f);
            fill.localScale = new Vector3(w, layout.fillScaleY, 1f);
        }
    }

    /// <summary>Small HP bar shown above a unit once it takes damage.</summary>
    public class HealthBar : MonoBehaviour
    {
        static Sprite whiteSprite;

        /// <summary>1x1 world-unit white sprite with its pivot on the left edge.</summary>
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), tex.width);
                }
                return whiteSprite;
            }
        }

        const float Scale = 0.6f;
        FramedBar bar;

        public static HealthBar Create(Transform parent, Team team)
        {
            return Create(parent, team, 0.8f);
        }

        public static HealthBar Create(Transform parent, Team team, float height)
        {
            var bm = BattleManager.Instance;
            var root = new GameObject("HealthBar");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            root.transform.localScale = Vector3.one * Scale;

            var hb = root.AddComponent<HealthBar>();
            hb.bar = FramedBar.Build(root.transform,
                bm != null ? bm.smallBarFrame : null,
                TeamFill(team, bm != null ? bm.smallBarFill : null, "Bars/SmallBar_Fill_Blue"),
                BarLayout.Small, 1000,
                team == Team.Player ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.35f, 0.35f));
            root.SetActive(false);
            return hb;
        }

        /// <summary>Blue team uses a recoloured blue fill (Assets/_Game/Resources/Bars); red team uses the original.</summary>
        public static Sprite TeamFill(Team team, Sprite original, string blueResourcePath)
        {
            if (team == Team.Player)
            {
                var blue = Resources.Load<Sprite>(blueResourcePath);
                if (blue != null) return blue;
            }
            return original;
        }

        public void Set(float ratio)
        {
            gameObject.SetActive(true);
            bar.Set(ratio);
        }
    }
}
