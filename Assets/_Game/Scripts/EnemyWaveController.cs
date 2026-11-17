using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>
    /// Enemy AI: earns gold at a fixed rate (StageData.enemyIncome) and buys units with it,
    /// following the same cost and cooldown rules as the player.
    /// </summary>
    public class EnemyWaveController : MonoBehaviour
    {
        public float Gold { get; private set; }

        StageData stage;
        UnitData next;
        float startDelay;
        float bossTimer;
        float elapsed;
        bool bossSpawned;
        readonly Dictionary<UnitData, float> cooldowns = new Dictionary<UnitData, float>();
        readonly List<UnitData> cooldownKeys = new List<UnitData>();

        public void Init(StageData data)
        {
            stage = data;
            Gold = data.enemyStartGold;
            startDelay = data.enemyFirstSpawnDelay;
            bossTimer = data.bossDelay;
            bossSpawned = data.boss == null;
            elapsed = 0f;
            cooldowns.Clear();
            next = PickNext();
        }

        void Update()
        {
            var bm = BattleManager.Instance;
            if (stage == null || bm == null || !bm.IsRunning) return;
            if (TutorialUI.HoldEnemies) return; // first-time tutorial in 1-1

            elapsed += Time.deltaTime;
            Gold += stage.enemyIncome * IncomeMultiplier() * Time.deltaTime;

            if (!bossSpawned)
            {
                bossTimer -= Time.deltaTime;
                Castle ec = bm.enemyCastle;
                bool castleLow = ec != null && ec.MaxHp > 0f && ec.Hp / ec.MaxHp <= stage.bossAtCastleHp;
                if (bossTimer <= 0f || castleLow)
                {
                    bossSpawned = true;
                    float bossMult = stage.bossStatMultiplier > 0f ? stage.bossStatMultiplier : stage.enemyStatMultiplier;
                    Unit boss = Spawner.Instance.SpawnUnit(stage.boss, Team.Enemy, bossMult);
                    if (StoryDialogUI.Instance != null) StoryDialogUI.Instance.BossAppeared(stage.stageName, boss);
                }
            }

            cooldownKeys.Clear();
            cooldownKeys.AddRange(cooldowns.Keys);
            foreach (var key in cooldownKeys) cooldowns[key] -= Time.deltaTime;

            if (startDelay > 0f)
            {
                startDelay -= Time.deltaTime;
                return;
            }

            if (next == null) next = PickNext();
            if (next == null || Gold < next.cost) return;
            if (cooldowns.TryGetValue(next, out float cd) && cd > 0f) return;

            Gold -= next.cost;
            cooldowns[next] = next.spawnCooldown;
            Spawner.Instance.SpawnUnit(next, Team.Enemy, stage.enemyStatMultiplier);
            next = PickNext();
        }

        /// <summary>Enemy income ramps: start share -> 100% at enemyIncomeFullTime -> max at enemyIncomeMaxTime.</summary>
        public float IncomeMultiplier()
        {
            if (stage == null) return 1f;
            float t1 = Mathf.Max(0.01f, stage.enemyIncomeFullTime);
            if (elapsed < t1) return Mathf.Lerp(stage.enemyIncomeStart, 1f, elapsed / t1);
            float t2 = Mathf.Max(t1 + 0.01f, stage.enemyIncomeMaxTime);
            return Mathf.Lerp(1f, stage.enemyIncomeMax, (elapsed - t1) / (t2 - t1));
        }

        /// <summary>Random pick by weight. Over time cheap units get rarer and expensive ones more common.</summary>
        UnitData PickNext()
        {
            if (stage == null || stage.enemyUnits == null) return null;
            float minCost = float.MaxValue, maxCost = float.MinValue;
            foreach (var c in stage.enemyUnits)
            {
                if (c.unit == null || c.weight <= 0f) continue;
                minCost = Mathf.Min(minCost, c.unit.cost);
                maxCost = Mathf.Max(maxCost, c.unit.cost);
            }
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(1f, stage.enemyIncomeMaxTime));
            float shift = stage.enemyLateShift * progress;

            float total = 0f;
            foreach (var c in stage.enemyUnits)
                if (c.unit != null && c.weight > 0f) total += Weight(c, minCost, maxCost, shift);
            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            foreach (var c in stage.enemyUnits)
            {
                if (c.unit == null || c.weight <= 0f) continue;
                roll -= Weight(c, minCost, maxCost, shift);
                if (roll <= 0f) return c.unit;
            }
            return stage.enemyUnits[stage.enemyUnits.Count - 1].unit;
        }

        static float Weight(EnemyUnitChoice c, float minCost, float maxCost, float shift)
        {
            float strength = maxCost > minCost ? (c.unit.cost - minCost) / (maxCost - minCost) : 0.5f;
            return c.weight * Mathf.Max(0.1f, 1f + shift * (2f * strength - 1f));
        }
    }
}
