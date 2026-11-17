using UnityEngine;

namespace TinySwordsBattle
{
    public class Castle : MonoBehaviour
    {
        public Team team;
        [Tooltip("Distance from the building centre to its front edge (world units)")]
        public float halfWidth = 1.2f;

        public float MaxHp { get; private set; }
        public float Hp { get; private set; }
        public bool IsAlive => Hp > 0f;

        /// <summary>X position where attackers stop and hit the building.</summary>
        public float FrontX => transform.position.x + (team == Team.Player ? halfWidth : -halfWidth);

        [Header("HP bar above the building")]
        [Tooltip("Height of the bar above the building centre (world units)")]
        public float barY = 2.3f;
        [Tooltip("Size of the bar frame")]
        public float barScale = 1.4f;

        FramedBar bar;
        TextMesh barLabel;
        Transform barRoot;

        /// <summary>Moves the HP bar (used when the base changes its look).</summary>
        public void SetBarY(float y)
        {
            barY = y;
            if (barRoot != null)
            {
                float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
                barRoot.localPosition = new Vector3(0f, barY / parentScale, 0f);
            }
        }

        public void Init(float hp)
        {
            MaxHp = hp;
            Hp = hp;
            if (bar == null) CreateBar();
            UpdateBar();
        }

        /// <summary>Raises (or lowers) max HP mid-battle; the difference is added to current HP.</summary>
        public void SetMaxHp(float hp)
        {
            if (Mathf.Approximately(hp, MaxHp)) return;
            Hp = Mathf.Max(1f, Hp + (hp - MaxHp));
            MaxHp = hp;
            UpdateBar();
        }

        void CreateBar()
        {
            var bm = BattleManager.Instance;
            float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);

            var root = new GameObject("HP Bar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, barY / parentScale, 0f);
            root.transform.localScale = Vector3.one * (barScale / parentScale);
            barRoot = root.transform;

            bar = FramedBar.Build(root.transform,
                bm != null ? bm.bigBarFrame : null,
                HealthBar.TeamFill(team, bm != null ? bm.bigBarFill : null, "Bars/BigBar_Fill_Blue"),
                BarLayout.Big, 2000,
                team == Team.Player ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.35f, 0.35f));

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            barLabel = labelGo.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            barLabel.font = font;
            barLabel.fontSize = 48;
            barLabel.characterSize = 0.045f;
            barLabel.anchor = TextAnchor.MiddleCenter;
            barLabel.alignment = TextAlignment.Center;
            barLabel.color = Color.white;
            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (font != null) mr.sharedMaterial = font.material;
                mr.sortingOrder = 2005;
            }
        }

        void UpdateBar()
        {
            if (bar == null || MaxHp <= 0f) return;
            bar.Set(Hp / MaxHp);
            if (barLabel != null) barLabel.text = $"{Mathf.CeilToInt(Hp)} / {Mathf.CeilToInt(MaxHp)}";
        }

        public void TakeDamage(float amount)
        {
            var bm = BattleManager.Instance;
            if (!IsAlive || bm == null || !bm.IsRunning) return;
            Hp = Mathf.Max(0f, Hp - amount);
            UpdateBar();
            Sfx.PlayAt("castle_hit", transform.position + Vector3.up);
            if (Hp <= 0f) bm.OnCastleDestroyed(this);
        }
    }
}
