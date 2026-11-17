using UnityEngine;

namespace TinySwordsBattle
{
    /// <summary>Arrow that flies to its target and deals damage on arrival.</summary>
    public class Projectile : MonoBehaviour
    {
        Unit target;
        Castle castle;
        float damage;
        Vector3 destination;
        const float Speed = 10f;

        public static void Fire(Vector3 from, Unit unitTarget, Castle castleTarget, float damage, Team team)
        {
            Fire(from, unitTarget, castleTarget, damage, team, null);
        }

        public static void Fire(Vector3 from, Unit unitTarget, Castle castleTarget, float damage, Team team, Sprite sprite)
        {
            var bm = BattleManager.Instance;
            var go = new GameObject("Arrow");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : (bm != null ? bm.arrowSprite : null);
            sr.sortingOrder = 900;
            sr.flipX = team == Team.Enemy;

            var p = go.AddComponent<Projectile>();
            p.target = unitTarget;
            p.castle = castleTarget;
            p.damage = damage;
            p.destination = unitTarget != null
                ? unitTarget.transform.position
                : new Vector3(castleTarget != null ? castleTarget.FrontX : from.x, from.y, 0f);
        }

        void Update()
        {
            if (target != null && target.IsAlive)
                destination = target.transform.position + Vector3.up * 0.2f;

            transform.position = Vector3.MoveTowards(transform.position, destination, Speed * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude < 0.01f)
            {
                if (target != null && target.IsAlive) target.TakeDamage(damage);
                else if (target == null && castle != null && castle.IsAlive) castle.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
