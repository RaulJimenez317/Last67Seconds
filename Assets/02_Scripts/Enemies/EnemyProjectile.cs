using Codice.Characters;
using UnityEngine;

namespace Codice.Enemies
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        BasicEnemy owner;
        public BasicEnemy Owner => owner;
        PlayerCharacter target;
        Vector3 velocity;
        float damage, remaining = 5;
        public void Launch(BasicEnemy source, PlayerCharacter player, Vector3 direction, float speed, float amount)
        { owner = source; target = player; velocity = direction.normalized * speed; damage = amount; }
        void Update()
        {
            if (!target || !target.State.IsAlive) { Destroy(gameObject); return; }
            if (owner && owner.Paused) return;
            float dt = Time.deltaTime;
            remaining -= dt;
            if (remaining <= 0) { Destroy(gameObject); return; }
            Vector3 step = velocity * dt;
            var hits = Physics.SphereCastAll(transform.position, .08f, velocity.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || (owner && hit.transform.IsChildOf(owner.transform))) continue;
                var player = hit.collider.GetComponentInParent<PlayerCharacter>();
                if (player) player.ReceiveDamage(new DamageInfo { amount = damage, direction = velocity.normalized, source = owner ? owner.gameObject : gameObject });
                var pillar = hit.collider.GetComponentInParent<Codice.Mechanics.DestructiblePillar>();
                if (pillar) pillar.ReceiveDamage(new DamageInfo { amount = damage });
                Destroy(gameObject); return;
            }
            transform.position += step;
        }
    }
}
