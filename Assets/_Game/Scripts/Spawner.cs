using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    public class Spawner : MonoBehaviour
    {
        public static Spawner Instance { get; private set; }

        [Tooltip("Order = order of the buttons at the bottom of the screen")]
        public UnitData[] playerUnits;
        public float laneY = -2f;

        // Vertical slots, cycled so units spawned one after another don't stack on the same line.
        static readonly float[] LaneOffsets = { 0f, 0.6f, -0.6f, 0.3f, -0.3f, 0.9f, -0.9f };
        readonly int[] slotIndex = new int[2];

        float[] cooldowns;

        void Awake()
        {
            Instance = this;
            cooldowns = new float[playerUnits != null ? playerUnits.Length : 0];
        }

        void Update()
        {
            for (int i = 0; i < cooldowns.Length; i++)
                if (cooldowns[i] > 0f) cooldowns[i] -= Time.deltaTime;
        }

        public float CooldownRatio(int index)
        {
            float cd = playerUnits[index].spawnCooldown;
            return cd <= 0f ? 0f : Mathf.Clamp01(cooldowns[index] / cd);
        }

        /// <summary>Price of each worker in order: the more you already have, the more the next one costs.</summary>
        public static readonly int[] WorkerCosts = { 25, 40, 60, 85, 115 };

        /// <summary>Current price of the unit on this button (workers get pricier as you hire more).</summary>
        public int CostOf(int index)
        {
            UnitData d = playerUnits[index];
            if (!d.IsWorker) return d.cost;
            int hired = GoldManager.Instance != null ? GoldManager.Instance.Workers : 0;
            return WorkerCosts[Mathf.Clamp(hired, 0, WorkerCosts.Length - 1)];
        }

        public bool CanSpawn(int index)
        {
            var bm = BattleManager.Instance;
            var gm = GoldManager.Instance;
            if (bm == null || gm == null || !bm.IsRunning || bm.IsPaused) return false;
            if (index < 0 || index >= playerUnits.Length || cooldowns[index] > 0f) return false;
            if (!IsAvailable(index)) return false;
            UnitData d = playerUnits[index];
            if (d.IsWorker && gm.Workers >= gm.maxWorkers) return false;
            return gm.CanAfford(CostOf(index));
        }

        /// <summary>Is this unit unlocked in the current stage?</summary>
        public bool IsAvailable(int index)
        {
            if (index < 0 || index >= playerUnits.Length) return false;
            // Units are unlocked in the shop (Pawn and Warrior from the start).
            return UnitProgress.IsUnlocked(playerUnits[index].type);
        }

        /// <summary>Called by the unit buttons.</summary>
        public void SpawnByIndex(int index)
        {
            if (!CanSpawn(index))
            {
                var bm = BattleManager.Instance;
                if (bm != null && bm.IsRunning && !bm.IsPaused) Sfx.Play("ui_denied");
                return;
            }
            UnitData d = playerUnits[index];
            Sfx.Play(d.IsWorker ? "spawn_worker" : "spawn");
            Stats.Add(Stats.Spawned);
            GoldManager.Instance.TrySpend(CostOf(index));
            cooldowns[index] = d.spawnCooldown;
            float mult = UnitProgress.StatMult(d.type);
            SpawnUnit(d, Team.Player, mult, mult);
        }

        public Unit SpawnUnit(UnitData data, Team team, float statMultiplier)
        {
            return SpawnUnit(data, team, statMultiplier, statMultiplier);
        }

        public Unit SpawnUnit(UnitData data, Team team, float hpMultiplier, float attackMultiplier)
        {
            Castle castle = BattleManager.Instance.GetCastle(team);
            float x = castle.FrontX + (team == Team.Player ? 0.3f : -0.3f);
            var go = new GameObject($"{data.displayName}_{team}");
            int t = (int)team;
            float offset = LaneOffsets[slotIndex[t] % LaneOffsets.Length] + Random.Range(-0.08f, 0.08f);
            slotIndex[t]++;
            go.transform.position = new Vector3(x, laneY + offset, 0f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Animator>();
            var unit = go.AddComponent<Unit>();
            unit.Init(data, team, hpMultiplier, attackMultiplier);
            if (team == Team.Enemy) Codex.MarkSeen(data);
            return unit;
        }
    }
}
