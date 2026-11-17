using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>World-space number that floats up and fades out (e.g. "+14" over a healed unit).</summary>
    public class FloatingText : MonoBehaviour
    {
        const float Life = 0.9f;
        const float Rise = 0.7f;

        TextMesh main;
        TextMesh shadow;
        Color color;
        Vector3 start;
        float t;

        public static void Spawn(Font font, string text, Vector3 position, Color color)
        {
            var go = new GameObject("PopText");
            go.transform.position = position;
            var ft = go.AddComponent<FloatingText>();
            ft.color = color;
            ft.start = position;
            ft.shadow = MakeText(go.transform, font, text, new Color(0f, 0f, 0f, 0.8f), new Vector3(0.03f, -0.03f, 0f), 1000);
            ft.main = MakeText(go.transform, font, text, color, Vector3.zero, 1001);
        }

        static TextMesh MakeText(Transform parent, Font font, string text, Color color, Vector3 offset, int order)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var tm = go.AddComponent<TextMesh>();
            if (font != null) tm.font = font;
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.position = start + Vector3.up * Rise * (1f - (1f - k) * (1f - k));
            float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            main.color = new Color(color.r, color.g, color.b, a);
            shadow.color = new Color(0f, 0f, 0f, 0.8f * a);
        }
    }
}
