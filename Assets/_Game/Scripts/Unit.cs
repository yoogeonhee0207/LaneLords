using System.Collections.Generic;
using UnityEngine;

namespace TinySwordsBattle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        public UnitData Data { get; private set; }
        public Team Team { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool IsAlive => Hp > 0f;

        SpriteRenderer sr;
        Animator anim;
        HealthBar bar;
        float attackPower;
        float healMultiplier = 1f;
        float attackTimer;
        string currentState;
        Vector3 workPoint;
        float workFaceX;
        int workSpot = -1;
        bool atWork;

        float Dir => Team == Team.Player ? 1f : -1f;
        const float FeetOffset = 0.5f;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
        }

        public void Init(UnitData data, Team team, float statMultiplier)
        {
            Init(data, team, statMultiplier, statMultiplier);
        }

        public void Init(UnitData data, Team team, float hpMultiplier, float attackMultiplier)
        {
            Data = data;
            Team = team;
            MaxHp = data.maxHp * hpMultiplier;
            Hp = MaxHp;
            attackPower = data.attack * attackMultiplier;
            healMultiplier = attackMultiplier;
            attackTimer = 0.3f;

            if (data.scale > 0f && !Mathf.Approximately(data.scale, 1f)) transform.localScale = Vector3.one * data.scale;
            if (anim == null) anim = gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = team == Team.Player ? data.playerController : data.enemyController;
            sr.flipX = team == Team.Enemy; // Tiny Swords sprites face right
            bar = HealthBar.Create(transform, team, data.barHeight > 0f ? data.barHeight : 0.8f);

            All.Add(this);

            if (data.IsWorker)
            {
                workSpot = BattleManager.Instance.ClaimWorkSpot(out workPoint, out workFaceX);
                GoldManager.Instance.AddWorker();
            }
            Play("Run");
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        void Update()
        {
            if (!IsAlive || Data == null) return;

            var bm = BattleManager.Instance;
            if (bm == null || !bm.IsRunning)
            {
                Play("Idle");
                LoopIfFinished();
                return;
            }

            // Sort by the feet (sprite centre is ~0.5 above them) so units in front of a tree trunk draw over the tree.
            sr.sortingOrder = Mathf.RoundToInt(-(transform.position.y - FeetOffset * Mathf.Max(1f, Data.scale)) * 100f);
            // Working woodcutters always draw in front of the forest (trees use sorting orders below ~50),
            // with a per-spot offset so two workers never share the same order and flicker.
            if (Data.IsWorker && atWork) sr.sortingOrder = 100 + Mathf.Max(0, workSpot);
            attackTimer -= Time.deltaTime;

            if (Data.IsWorker) UpdateWorker();
            else if (Data.IsHealer) UpdateHealer();
            else UpdateFighter();

            LoopIfFinished();
        }

        // ---------- Behaviours ----------

        void UpdateWorker()
        {
            if (atWork) return;
            transform.position = Vector3.MoveTowards(transform.position, workPoint, Data.moveSpeed * Time.deltaTime);
            sr.flipX = workPoint.x < transform.position.x;
            Play("Run");
            if ((transform.position - workPoint).sqrMagnitude < 0.0025f)
            {
                atWork = true;
                sr.flipX = workFaceX < transform.position.x; // face the trunk
                Play("Attack"); // chopping animation
            }
        }

        void UpdateFighter()
        {
            Unit target = FindEnemyInRange(Data.attackRange);
            if (target != null)
            {
                if (attackTimer <= 0f) target = PickFrontLineTarget(target, Data.attackRange);
                TryAttack(target, null);
                return;
            }

            Castle castle = BattleManager.Instance.GetCastle(Opposite(Team));
            if (castle != null && castle.IsAlive && DistanceToCastle(castle) <= Data.attackRange)
            {
                TryAttack(null, castle);
                return;
            }

            Move();
        }

        void UpdateHealer()
        {
            bool shouldHold = FindEnemyInRange(Data.attackRange + 0.5f) != null || IsBehindFrontAlly();
            if (!shouldHold)
            {
                Move();
                return;
            }

            if (attackTimer > 0f) return;

            // Holy burst: aim at the nearest enemy (or a hurt ally) in range;
            // everything within skillRadius of that spot: enemies take damage, allies are healed.
            Unit enemy = FindEnemyInRange(Data.attackRange);
            if (enemy != null) enemy = PickFrontLineTarget(enemy, Data.attackRange);
            Unit ally = enemy == null ? FindHurtAlly(Data.attackRange) : null;
            Unit focus = enemy != null ? enemy : ally;
            if (focus == null)
            {
                Play("Idle");
                return;
            }

            attackTimer = Data.attackInterval;
            Play("Attack", true);
            float centerX = focus.transform.position.x;
            float radius = Data.skillRadius > 0f ? Data.skillRadius : 1.8f;
            var targets = new List<Unit>();
            foreach (var u in All)
                if (u != null && u.IsAlive && Mathf.Abs(u.transform.position.x - centerX) <= radius) targets.Add(u);
            foreach (var u in targets)
            {
                if (u.Team != Team) u.TakeDamage(attackPower);
                else if (u.Hp < u.MaxHp) u.Heal(Data.healAmount * healMultiplier);
            }
            Sfx.PlayAt("monk_burst", focus.transform.position);
            var bm = BattleManager.Instance;
            if (bm != null)
                bm.SpawnEffect(bm.healEffect, new Vector3(centerX, focus.transform.position.y, 0f), 0.8f, radius * 1.1f);
        }

        void Move()
        {
            transform.position += new Vector3(Dir * Data.moveSpeed * Time.deltaTime, 0f, 0f);
            Play("Run");
        }

        void TryAttack(Unit unitTarget, Castle castleTarget)
        {
            if (attackTimer > 0f)
            {
                if (currentState == "Run") Play("Idle");
                return;
            }

            attackTimer = Data.attackInterval;
            Play("Attack", true);
            AttackSound();

            float damage = attackPower;
            if (unitTarget != null && Data.hasCounter && unitTarget.Data.type == Data.strongAgainst)
                damage *= Data.counterMultiplier;

            if (Data.isRanged)
            {
                Projectile.Fire(transform.position + Vector3.up * 0.2f * Mathf.Max(1f, Data.scale), unitTarget, castleTarget, damage, Team, Data.projectileSprite);
            }
            else if (unitTarget != null)
            {
                unitTarget.TakeDamage(damage);
            }
            else if (castleTarget != null)
            {
                castleTarget.TakeDamage(damage);
            }
        }

        /// <summary>Attack sound by side and kind of unit.</summary>
        void AttackSound()
        {
            string key;
            if (Team == Team.Player) key = Data.isRanged ? "bow" : "hit_metal";
            else if (Data.isBoss) key = "boss_attack";
            else if (Data.isRanged) key = Data.displayName == "Bomb Fish" ? "bomb" : "throw";
            else key = Data.isCursedAnimal ? "hit_animal" : "hit_enemy";
            Sfx.PlayAt(key, transform.position);
        }

        // ---------- Queries ----------

        Unit FindEnemyInRange(float range)
        {
            Unit best = null;
            float bestDist = float.MaxValue;
            float x = transform.position.x;
            for (int i = 0; i < All.Count; i++)
            {
                Unit other = All[i];
                if (other == null || other == this || other.Team == Team || !other.IsAlive || other.Data.IsWorker) continue;
                float dx = (other.transform.position.x - x) * Dir; // distance in front of me
                if (dx < -0.3f) continue;
                if (dx <= range && dx < bestDist)
                {
                    best = other;
                    bestDist = dx;
                }
            }
            return best;
        }

        /// <summary>Width of the "front line": enemies standing within this distance behind the nearest one.</summary>
        public const float FrontLineDepth = 0.6f;
        static readonly List<Unit> lineBuffer = new List<Unit>();

        /// <summary>Random enemy from the front line (the nearest enemy plus anyone standing just behind it, still in range).
        /// Spreads damage across a crowded front instead of focusing one unit.</summary>
        Unit PickFrontLineTarget(Unit nearest, float range)
        {
            float x = transform.position.x;
            float limit = Mathf.Min(range, (nearest.transform.position.x - x) * Dir + FrontLineDepth);
            lineBuffer.Clear();
            for (int i = 0; i < All.Count; i++)
            {
                Unit other = All[i];
                if (other == null || other == this || other.Team == Team || !other.IsAlive || other.Data.IsWorker) continue;
                float dx = (other.transform.position.x - x) * Dir;
                if (dx >= -0.3f && dx <= limit) lineBuffer.Add(other);
            }
            return lineBuffer.Count > 1 ? lineBuffer[Random.Range(0, lineBuffer.Count)] : nearest;
        }

        bool IsBehindFrontAlly()
        {
            float x = transform.position.x;
            for (int i = 0; i < All.Count; i++)
            {
                Unit other = All[i];
                if (other == null || other == this || other.Team != Team || !other.IsAlive) continue;
                if (other.Data.IsWorker || other.Data.IsHealer) continue;
                float dx = (other.transform.position.x - x) * Dir;
                if (dx > 0f && dx < 1.5f) return true;
            }
            return false;
        }

        Unit FindHurtAlly(float range)
        {
            Unit best = null;
            float lowest = 0.999f;
            for (int i = 0; i < All.Count; i++)
            {
                Unit other = All[i];
                if (other == null || other.Team != Team || !other.IsAlive || other.Data.IsWorker) continue;
                if (Mathf.Abs(other.transform.position.x - transform.position.x) > range) continue;
                float ratio = other.Hp / other.MaxHp;
                if (ratio < lowest)
                {
                    lowest = ratio;
                    best = other;
                }
            }
            return best;
        }

        float DistanceToCastle(Castle castle)
        {
            return (castle.FrontX - transform.position.x) * Dir;
        }

        static Team Opposite(Team t) => t == Team.Player ? Team.Enemy : Team.Player;

        // ---------- Health ----------

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            Hp -= amount;
            if (bar != null) bar.Set(Hp / MaxHp);
            if (Hp <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            float before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (bar != null) bar.Set(Hp / MaxHp);
            var bm = BattleManager.Instance;
            if (bm == null) return;
            // Green sparkle on the healed unit + a floating "+N" so heals are easy to see.
            var fx = bm.SpawnEffect(bm.healEffect, transform.position + Vector3.up * 0.3f * Mathf.Max(1f, Data.scale), 0.8f, 0.7f * Mathf.Max(1f, Data.scale));
            if (fx != null) fx.GetComponent<SpriteRenderer>().color = new Color(0.55f, 1f, 0.55f, 1f);
            int healed = Mathf.RoundToInt(Hp - before);
            if (healed > 0 && Team == Team.Player) Stats.Add(Stats.Healed, healed);
            if (healed > 0)
            {
                float top = (Data.barHeight > 0f ? Data.barHeight : 0.8f) * Mathf.Max(1f, Data.scale) + 0.25f;
                bm.PopText("+" + healed, transform.position + new Vector3(Random.Range(-0.15f, 0.15f), top, 0f), new Color(0.45f, 1f, 0.4f));
            }
        }

        void Die()
        {
            Hp = 0f;
            All.Remove(this);
            if (!Data.IsWorker) Sfx.PlayAt(Data.isCursedAnimal ? "curse_free" : "death", transform.position);
            if (Team == Team.Enemy)
            {
                Stats.Add(Stats.Kills);
                if (Data.isCursedAnimal) Stats.Add(Stats.Freed);
            }

            var bm = BattleManager.Instance;
            if (Team == Team.Enemy && GoldManager.Instance != null)
                GoldManager.Instance.AddGold(Data.cost * 0.3f); // kill reward
            if (Data.IsWorker && GoldManager.Instance != null && Team == Team.Player)
                GoldManager.Instance.RemoveWorker();
            if (Data.IsWorker && bm != null) bm.ReleaseWorkSpot(workSpot);
            if (bm != null)
            {
                if (Data.isCursedAnimal) bm.SpawnEffect(bm.healEffect, transform.position, 0.8f, 1.1f * Mathf.Max(1f, Data.scale)); // curse broken
                else bm.SpawnEffect(bm.dustEffect, transform.position, 0.8f, 1f);
            }

            Destroy(gameObject);
        }

        // ---------- Animation ----------

        /// <summary>
        /// Tiny Swords clips are not set to loop, so Idle/Run (and mining) are restarted when they finish.
        /// </summary>
        void LoopIfFinished()
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            bool shouldLoop = currentState == "Idle" || currentState == "Run" || (Data.IsWorker && atWork);
            if (!shouldLoop) return;
            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                anim.Play(currentState, 0, 0f);
        }

        void Play(string state, bool restart = false)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            if (!restart && currentState == state) return;
            currentState = state;
            anim.Play(state, 0, 0f);
        }
    }
}
