using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>The chapter's sky picture follows the camera at a fraction of its speed, so it feels far away.</summary>
    public class SkyParallax : MonoBehaviour
    {
        [Tooltip("0 = fixed to the world, 1 = fixed to the screen")]
        [Range(0f, 1f)] public float factor = 0.5f;

        Transform cam;
        float baseX;

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            baseX = transform.position.x;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 p = transform.position;
            p.x = baseX + cam.position.x * factor;
            transform.position = p;
        }
    }
}
