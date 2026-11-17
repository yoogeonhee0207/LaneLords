using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinySwordsBattle
{
    /// <summary>How one chapter looks: which prop set is on, and how the ground, water and props are coloured.</summary>
    [System.Serializable]
    public class ChapterLook
    {
        public GameObject set;
        [Tooltip("Multiplies the ground tiles")] public Color groundTint = Color.white;
        [Tooltip("Multiplies the water tiles (and the camera background)")] public Color waterTint = Color.white;
        [Tooltip("Multiplies the shared forest, bushes and rocks")] public Color propTint = Color.white;
        [Tooltip("Screen-edge darkening (alpha = strength)")] public Color vignette = new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Gives each chapter its own battlefield mood. Units and castles are never tinted, only the scenery,
    /// so the fight stays easy to read in the darker chapters.
    /// </summary>
    public class ChapterTheme : MonoBehaviour
    {
        public const float BuiltHalfWidth = 9.5f;

        public Tilemap ground;
        public Tilemap water;
        public Transform[] sharedProps;     // the workers' forest and the old bushes/rocks
        public SpriteRenderer vignette;
        public ChapterLook[] looks = new ChapterLook[4];

        static readonly Color WaterBase = new Color32(71, 171, 169, 255);

        public static int ChapterOf(StageData stage)
        {
            if (stage == null || string.IsNullOrEmpty(stage.stageName)) return 1;
            int c = stage.stageName[0] - '0';
            return Mathf.Clamp(c, 1, 4);
        }

        void Start()
        {
            var bm = BattleManager.Instance;
            StageData stage = bm != null ? bm.stage : null;
            float w = stage != null && stage.mapHalfWidth > 0f ? stage.mapHalfWidth : BuiltHalfWidth;
            Apply(ChapterOf(stage), w);
        }

        /// <summary>Turns on one chapter's look. halfWidth stretches the props to the chapter's map width.</summary>
        public void Apply(int chapter, float halfWidth = BuiltHalfWidth)
        {
            int idx = Mathf.Clamp(chapter, 1, looks.Length) - 1;
            for (int i = 0; i < looks.Length; i++)
                if (looks[i] != null && looks[i].set != null) looks[i].set.SetActive(i == idx);

            ChapterLook look = looks[idx];
            if (look == null) return;
            if (ground != null) ground.color = look.groundTint;
            if (water != null) water.color = look.waterTint;
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = WaterBase * look.waterTint;

            if (sharedProps != null)
                foreach (var root in sharedProps)
                    if (root != null)
                        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = look.propTint;

            if (vignette != null)
            {
                vignette.color = look.vignette;
                vignette.enabled = look.vignette.a > 0.01f;
            }

            if (look.set != null && Application.isPlaying && !Mathf.Approximately(halfWidth, BuiltHalfWidth))
            {
                foreach (Transform child in look.set.transform)
                {
                    Vector3 p = child.position;
                    p.x *= halfWidth / BuiltHalfWidth;
                    child.position = p;
                    // keep land props off the castles (a tier-4 castle is ~3 units wide each side)
                    if (p.y < 2.5f && Mathf.Abs(p.x) > halfWidth - 3.2f && child.GetComponent<AmbientMotes>() == null)
                        child.gameObject.SetActive(false);
                }
            }
        }
    }
}
