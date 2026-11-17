using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TinySwordsBattle
{
    /// <summary>
    /// Horizontal camera panning: drag with mouse/finger, or use A/D and arrow keys.
    /// The camera never shows past the field edges.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPan : MonoBehaviour
    {
        [Tooltip("Left edge of the battlefield (world X)")]
        public float fieldMinX = -12.3f;
        [Tooltip("Right edge of the battlefield (world X)")]
        public float fieldMaxX = 12.3f;
        public float keySpeed = 12f;
        [Tooltip("Start looking at the player's castle")]
        public bool startAtPlayerCastle = true;

        [Header("Auto follow")]
        [Tooltip("Follow our front-most soldier when it nears the screen edge")]
        public bool followFront = true;
        [Tooltip("Where our front soldier may go on screen before the camera follows (0 = left edge, 1 = right edge)")]
        [Range(0.5f, 0.95f)] public float frontScreenX = 0.75f;
        [Tooltip("...and from the left edge (when our line is pushed back)")]
        public float leftMargin = 3f;
        public float followSpeed = 6f;
        [Tooltip("Seconds auto follow stays off after the player pans by hand")]
        public float manualPause = 3f;

        float manualTimer;

        Camera cam;
        bool dragging;
        Vector2 lastScreenPos;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            SetX(startAtPlayerCastle ? float.MinValue : transform.position.x);
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer != null)
            {
                if (pointer.press.wasPressedThisFrame)
                {
                    bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                    dragging = !overUI;
                    lastScreenPos = pointer.position.ReadValue();
                }
                else if (pointer.press.wasReleasedThisFrame)
                {
                    dragging = false;
                }

                if (dragging && pointer.press.isPressed)
                {
                    Vector2 pos = pointer.position.ReadValue();
                    float worldPerPixel = cam.orthographicSize * 2f / Screen.height;
                    if (pos.x != lastScreenPos.x) manualTimer = manualPause;
                    SetX(transform.position.x - (pos.x - lastScreenPos.x) * worldPerPixel);
                    lastScreenPos = pos;
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                float dir = 0f;
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += 1f;
                if (dir != 0f)
                {
                    manualTimer = manualPause;
                    SetX(transform.position.x + dir * keySpeed * Time.unscaledDeltaTime);
                }
            }

            if (manualTimer > 0f) manualTimer -= Time.unscaledDeltaTime;
            else if (followFront && !dragging) FollowFront();
        }

        /// <summary>Jumps the camera to x (e.g. the boss) and holds it there for a moment before auto follow resumes.</summary>
        public void LookAt(float x, float hold)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            SetX(x - halfWidth * 0.3f);
            manualTimer = hold;
        }

        /// <summary>Pans so our front-most fighter (workers excluded) stays on screen.</summary>
        void FollowFront()
        {
            float front = float.MinValue;
            foreach (var u in Unit.All)
            {
                if (u == null || u.Team != Team.Player || u.Data == null || u.Data.IsWorker) continue;
                if (u.transform.position.x > front) front = u.transform.position.x;
            }
            if (front == float.MinValue) return;

            float halfWidth = cam.orthographicSize * cam.aspect;
            float x = transform.position.x;
            float target = x;
            float frontOffset = halfWidth * (2f * frontScreenX - 1f); // 0.75 -> half a half-screen right of centre
            if (front > x + frontOffset) target = front - frontOffset;
            else if (front < x - halfWidth + leftMargin) target = front + halfWidth - leftMargin;
            if (Mathf.Abs(target - x) < 0.01f) return;
            SetX(Mathf.MoveTowards(x, target, followSpeed * Time.deltaTime));
        }

        void SetX(float x)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            float min = fieldMinX + halfWidth;
            float max = fieldMaxX - halfWidth;
            if (min > max) min = max = (fieldMinX + fieldMaxX) * 0.5f;
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(x, min, max);
            transform.position = p;
        }
    }
}
