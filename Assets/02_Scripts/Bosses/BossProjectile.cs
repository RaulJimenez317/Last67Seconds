using Codice.Characters;
using UnityEngine;

namespace Codice.Bosses
{
    public sealed class BossProjectile : MonoBehaviour
    {
        BossCharacter owner;
        BossAttack attack;
        Vector3 velocity;
        float damage, lifetime = 6;
        public void Initialize(BossCharacter boss, BossAttack type, Vector3 direction, float amount)
        { owner = boss; attack = type; damage = amount; velocity = direction.normalized * (type == BossAttack.Bow ? 25 : type == BossAttack.Mud ? 12 : 15); }
        void Update()
        {
            if (!owner || !owner.IsAlive || !owner.Target || !owner.Target.State.IsAlive) { Destroy(gameObject); return; }
            if (owner.Paused) return;
            float dt = Time.deltaTime; lifetime -= dt;
            if (lifetime <= 0) { Destroy(gameObject); return; }
            var step = velocity * dt;
            var hits = Physics.SphereCastAll(transform.position, .18f, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(owner.transform)) continue;
                var player = hit.collider.GetComponentInParent<PlayerCharacter>();
                bool explosive = attack == BossAttack.Bow && owner.Evolved;
                if (player && !explosive)
                {
                    player.ReceiveDamage(new DamageInfo { amount = damage, source = owner.gameObject, direction = velocity.normalized });
                    if (attack == BossAttack.Rock) player.ApplyBossKnockdown();
                }
                if (explosive)
                {
                    Codice.Presentation.PresentationVfx.Explosion(hit.point);
                    var target = owner.Target;
                    Vector3 delta = target.transform.position + Vector3.up - hit.point;
                    bool blocked = Physics.Linecast(hit.point + hit.normal * .2f, target.transform.position + Vector3.up, out var obstruction, ~0, QueryTriggerInteraction.Ignore);
                    if (delta.magnitude <= 2.5f && (!blocked || player || obstruction.collider.GetComponentInParent<PlayerCharacter>() == target))
                        target.ReceiveDamage(new DamageInfo { amount = damage, source = owner.gameObject });
                }
                if (attack == BossAttack.Mud) BossMud.Create(owner, hit.point);
                var pillar = hit.collider.GetComponentInParent<Codice.Mechanics.DestructiblePillar>();
                if (pillar) pillar.ReceiveDamage(new DamageInfo { amount = damage, source = owner.gameObject });
                Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                Destroy(gameObject); return;
            }
            transform.position += step;
        }
    }
}
