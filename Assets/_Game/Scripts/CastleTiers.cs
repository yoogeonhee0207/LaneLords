using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Four looks for a base, built from Tiny Swords buildings by the scene builder:
    /// 1 House, 2 Monastery, 3 Castle with the Monastery behind, 4 Castle + Monastery + two Towers.
    /// Our base grows with the "Base HP" castle upgrade; the enemy base grows with the chapter.
    /// </summary>
    public class CastleTiers : MonoBehaviour
    {
        public GameObject[] tiers = new GameObject[4];
        [Tooltip("Castle.halfWidth for each tier (where attackers stop)")]
        public float[] halfWidths = { 1f, 1.2f, 1.85f, 2.3f };
        [Tooltip("Castle.barY for each tier (HP bar height above the ground)")]
        public float[] barYs = { 3.8f, 4.5f, 5.2f, 5.1f };

        public int Current { get; private set; } = -1;

        /// <summary>Castle upgrade level 0-2 / 3-5 / 6-8 / 9-10 -> tier 1 / 2 / 3 / 4.</summary>
        public static int TierForUpgrade(int level) => Mathf.Clamp(level / 3, 0, 3);

        /// <summary>Stage "3-4" -> chapter 3 -> tier 3.</summary>
        public static int TierForStage(StageData stage)
        {
            if (stage == null || string.IsNullOrEmpty(stage.stageName)) return 0;
            int chapter;
            int dash = stage.stageName.IndexOf('-');
            string head = dash > 0 ? stage.stageName.Substring(0, dash) : stage.stageName;
            return int.TryParse(head, out chapter) ? Mathf.Clamp(chapter - 1, 0, 3) : 0;
        }

        /// <summary>Shows the right tier and sizes the Castle to match. Called by BattleManager before the castle is set up.</summary>
        public void Apply(StageData stage)
        {
            var castle = GetComponent<Castle>();
            int tier = castle != null && castle.team == Team.Player
                ? TierForUpgrade(Upgrades.Level(UpgradeType.CastleHp))
                : TierForStage(stage);
            Show(tier);
        }

        public void Show(int tier)
        {
            tier = Mathf.Clamp(tier, 0, tiers.Length - 1);
            Current = tier;
            for (int i = 0; i < tiers.Length; i++)
                if (tiers[i] != null) tiers[i].SetActive(i == tier);

            var castle = GetComponent<Castle>();
            if (castle == null) return;
            if (tier < halfWidths.Length) castle.halfWidth = halfWidths[tier];
            if (tier < barYs.Length) castle.SetBarY(barYs[tier]);
        }
    }
}
