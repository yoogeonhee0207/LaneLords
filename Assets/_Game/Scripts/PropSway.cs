using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>Gently rocks a prop around its bottom pivot (bamboo, dead trees) or bobs it up and down (things in the water).</summary>
    public class PropSway : MonoBehaviour
    {
        [Tooltip("Rocking angle in degrees")] public float angle = 1.5f;
        [Tooltip("Up-down bob in units")] public float bob = 0f;
        public float speed = 1f;

        Vector3 startPos;
        float phase;

        void Start()
        {
            startPos = transform.localPosition;
            phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            float s = Mathf.Sin(Time.time * speed + phase);
            if (angle != 0f) transform.localRotation = Quaternion.Euler(0f, 0f, s * angle);
            if (bob != 0f) transform.localPosition = startPos + new Vector3(0f, s * bob, 0f);
        }
    }
}
