using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    [System.Serializable]
    public class EnemyUnitChoice
    {
        public UnitData unit;
        [Tooltip("Higher = picked more often")]
        public float weight = 1f;
    }

    [CreateAssetMenu(menuName = "Lane Lords/Stage Data", fileName = "StageData")]
    public class StageData : ScriptableObject
    {
        public string stageName = "1-1";
        [Tooltip("Ground colour 1-5 (Tilemap_color1..5). Chapter 1 = 1 (light green), 2 = 3 (deep green), 3 = 5 (teal), 4 = 4 (dry olive)")]
        [Range(1, 5)] public int terrainColor = 1;
        [Tooltip("Distance from the map centre to each castle. Chapter 1 = 9.5, wider in later chapters")]
        public float mapHalfWidth = 9.5f;

        [Header("Player")]
        public int startGold = 0;
        [Tooltip("Gold per second without workers")]
        public float baseIncome = 5f;
        [Tooltip("Extra gold per second for each worker")]
        public float incomePerWorker = 1f;
        public int maxWorkers = 5;
        public float playerCastleHp = 700f;

        [Header("Enemy")]
        public float enemyCastleHp = 600f;
        [Tooltip("Enemy gold per second. Main difficulty knob.")]
        public float enemyIncome = 5f;
        public int enemyStartGold = 0;
        [Tooltip("Seconds before the enemy starts spending")]
        public float enemyFirstSpawnDelay = 5f;
        [Tooltip("Multiplies enemy HP and attack")]
        public float enemyStatMultiplier = 1f;
        [Header("Enemy pacing over time")]
        [Tooltip("Enemy income multiplier at the start of the battle")]
        public float enemyIncomeStart = 0.8f;
        [Tooltip("Seconds until enemy income reaches 100%")]
        public float enemyIncomeFullTime = 20f;
        [Tooltip("Highest enemy income multiplier, reached at enemyIncomeMaxTime")]
        public float enemyIncomeMax = 1.8f;
        public float enemyIncomeMaxTime = 90f;
        [Tooltip("How far unit weights shift from cheap to expensive units by enemyIncomeMaxTime (0 = never)")]
        [Range(0f, 0.9f)] public float enemyLateShift = 0.6f;

        [Tooltip("Units the enemy can buy, picked at random by weight")]
        public List<EnemyUnitChoice> enemyUnits = new List<EnemyUnitChoice>();

        [Header("Boss (chapter finale)")]
        [Tooltip("Spawned once, for free. Empty = no boss")]
        public UnitData boss;
        [Tooltip("The boss comes out when the enemy castle drops to this share of its HP (0 = off)...")]
        [Range(0f, 1f)] public float bossAtCastleHp = 0.6f;
        [Tooltip("...or after this many seconds, whichever comes first")]
        public float bossDelay = 90f;
        [Tooltip("Multiplies the boss's HP and attack (0 = use enemyStatMultiplier)")]
        public float bossStatMultiplier = 0f;
    }
}
