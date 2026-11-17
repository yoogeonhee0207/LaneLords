using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    [CreateAssetMenu(menuName = "Lane Lords/Unit Data", fileName = "UnitData")]
    public class UnitData : ScriptableObject
    {
        public UnitType type;
        public string displayName = "Unit";

        [Header("Stats")]
        public int cost = 30;
        public float maxHp = 100f;
        public float attack = 10f;
        [Tooltip("World units. Melee ~0.9, ranged ~5")]
        public float attackRange = 0.9f;
        public float attackInterval = 1f;
        public float moveSpeed = 1.2f;
        public float spawnCooldown = 2f;

        [Header("Counter (rock-paper-scissors)")]
        public bool hasCounter;
        public UnitType strongAgainst;
        public float counterMultiplier = 1.5f;

        [Header("Special")]
        public bool isRanged;
        [Tooltip("Monk: radius of the holy burst (enemies hurt, allies healed)")]
        public float skillRadius = 1.8f;
        [Tooltip("Monk only: HP healed per burst")]
        public float healAmount;

        [Header("Size")]
        [Tooltip("Sprite scale. Bosses are drawn bigger (e.g. 1.6)")]
        public float scale = 1f;
        public bool isBoss;
        [Tooltip("Animal controlled by the curse: sparkles away (freed) instead of turning to dust")]
        public bool isCursedAnimal;
        [Tooltip("Height of the HP bar above the unit (before scale)")]
        public float barHeight = 0.8f;

        [Header("Ranged")]
        [Tooltip("Projectile sprite for ranged units. Empty = arrow")]
        public Sprite projectileSprite;

        [Header("Visuals (auto-filled by the scene builder)")]
        public RuntimeAnimatorController playerController;
        public RuntimeAnimatorController enemyController;
        public Sprite icon;

        public bool IsWorker => type == UnitType.Pawn;
        public bool IsHealer => type == UnitType.Monk;
    }
}
