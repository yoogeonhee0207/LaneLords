using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Light ambient effects made of a few soft sprites (built by the scene builder as children):
    /// fireflies, rising embers, drifting fog or cloud shadows, and blinking eyes in the bushes.
    /// </summary>
    public class AmbientMotes : MonoBehaviour
    {
        public enum Kind { Firefly, Ember, Drift, Eyes }

        public Kind kind;
        public Vector2 areaMin = new Vector2(-10f, -4f);
        public Vector2 areaMax = new Vector2(10f, 1f);
        [Tooltip("Drift and Ember: movement per second")] public Vector2 velocity = new Vector2(0.2f, 0f);
        [Tooltip("Highest alpha of each mote")] public float maxAlpha = 1f;

        SpriteRenderer[] motes;
        Vector3[] home;
        float[] phase;
        float[] speed;
        Color baseColor;

        void Start()
        {
            motes = GetComponentsInChildren<SpriteRenderer>(true);
            home = new Vector3[motes.Length];
            phase = new float[motes.Length];
            speed = new float[motes.Length];
            if (motes.Length > 0) baseColor = motes[0].color;
            for (int i = 0; i < motes.Length; i++)
            {
                home[i] = motes[i].transform.position;
                phase[i] = Random.Range(0f, 100f);
                speed[i] = Random.Range(0.7f, 1.3f);
                // eyes come in pairs that blink together
                if (kind == Kind.Eyes && i % 2 == 1) { phase[i] = phase[i - 1]; speed[i] = speed[i - 1]; }
            }
        }

        void Update()
        {
            if (motes == null) return;
            float t = Time.time;
            float dt = Time.deltaTime;
            float width = areaMax.x - areaMin.x;
            for (int i = 0; i < motes.Length; i++)
            {
                var m = motes[i];
                if (m == null) continue;
                Transform tr = m.transform;
                float ph = phase[i];
                float sp = speed[i];
                Color c = baseColor;
                switch (kind)
                {
                    case Kind.Firefly:
                        tr.position = home[i] + new Vector3(Mathf.Sin(t * 0.5f * sp + ph) * 0.9f, Mathf.Sin(t * 0.8f * sp + ph * 1.7f) * 0.4f, 0f);
                        c.a = maxAlpha * Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(t * 2.2f * sp + ph));
                        break;
                    case Kind.Ember:
                    {
                        Vector3 p = tr.position;
                        p.y += velocity.y * sp * dt;
                        p.x = home[i].x + Mathf.Sin(t * 1.3f * sp + ph) * 0.35f;
                        float life = Mathf.InverseLerp(areaMin.y, areaMax.y, p.y);
                        if (p.y > areaMax.y)
                        {
                            p.y = areaMin.y;
                            home[i].x = Random.Range(areaMin.x, areaMax.x);
                        }
                        tr.position = p;
                        c.a = maxAlpha * Mathf.Sin(life * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(t * 9f + ph));
                        break;
                    }
                    case Kind.Drift:
                    {
                        Vector3 p = tr.position;
                        p.x += velocity.x * sp * dt;
                        if (p.x > areaMax.x) p.x -= width;
                        else if (p.x < areaMin.x) p.x += width;
                        p.y = home[i].y + Mathf.Sin(t * 0.2f + ph) * 0.15f;
                        tr.position = p;
                        // fade near the area ends so wrapping is invisible
                        float edge = Mathf.Min(p.x - areaMin.x, areaMax.x - p.x) / 2f;
                        c.a = maxAlpha * Mathf.Clamp01(edge) * (0.8f + 0.2f * Mathf.Sin(t * 0.3f + ph));
                        break;
                    }
                    case Kind.Eyes:
                    {
                        // open most of the time, blink now and then, sometimes close for a while
                        float cycle = Mathf.Repeat(t * 0.25f * sp + ph, 1f);
                        bool open = cycle < 0.7f && Mathf.Repeat(t * 1.1f + ph, 3.3f) > 0.15f;
                        c.a = open ? maxAlpha : 0f;
                        break;
                    }
                }
                m.color = c;
            }
        }
    }
}
