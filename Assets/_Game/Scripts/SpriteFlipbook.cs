using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>Plays a sprite strip on a loop (with an optional random pause between loops), starting at a random frame.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 10f;
        [Tooltip("Random wait between loops (0 = loop straight away). The renderer hides while waiting if hideWhileWaiting is on.")]
        public Vector2 pause = Vector2.zero;
        public bool hideWhileWaiting;

        SpriteRenderer sr;
        float timer;
        float wait;
        int frame;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0) { enabled = false; return; }
            frame = Random.Range(0, frames.Length);
            wait = pause.y > 0f ? Random.Range(0f, pause.y) : 0f;
            sr.sprite = frames[frame];
        }

        void Update()
        {
            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                if (hideWhileWaiting) sr.enabled = false;
                return;
            }
            sr.enabled = true;
            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, fps);
            while (timer >= step)
            {
                timer -= step;
                frame++;
                if (frame >= frames.Length)
                {
                    frame = 0;
                    if (pause.y > 0f) wait = Random.Range(pause.x, pause.y);
                }
            }
            sr.sprite = frames[frame];
        }
    }
}
