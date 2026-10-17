using System.Collections.Generic;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Mechanics
{
    public static class CombatTargets
    {
        public static bool IsEnemy(Component target) => target && (target.GetComponent<BasicEnemy>() || target.GetComponent<BossCharacter>());
        public static bool Visible(Vector3 origin, Component target)
        {
            var collider = target.GetComponent<Collider>();
            if (!collider) return false;
            Vector3 point = collider.bounds.center;
            foreach (var hit in Physics.RaycastAll(origin, (point - origin).normalized, Vector3.Distance(origin, point), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(target.transform) && !hit.collider.GetComponentInParent<PlayerCharacter>() && !IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component)) return false;
            return true;
        }
        public static List<Component> InRadius(Vector3 origin, float radius)
        {
            var result = new List<Component>();
            foreach (var collider in Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var receiver = collider.GetComponentInParent<IDamageReceiver>() as Component;
                if (IsEnemy(receiver) && !result.Contains(receiver) && Visible(origin, receiver)) result.Add(receiver);
            }
            return result;
        }
        public static void Hit(Component target, PlayerCharacter player, float damage, bool guard = false, float push = 0, bool secondary = false)
        {
            if (!target) return;
            (target as IDamageReceiver)?.ReceiveDamage(new DamageInfo { amount = damage, source = player.gameObject, direction = (target.transform.position - player.transform.position).normalized, breaksGuard = guard, ignoreGuard = guard, knockback = push, secondary = secondary });
        }
        public static void Stun(Component target, float seconds)
        {
            if (target is BasicEnemy enemy) enemy.ApplyStun(seconds);
            if (target is BossCharacter boss) boss.ApplyStun(seconds);
        }
        public static void Pull(Component target, Vector3 point, float distance)
        {
            if (!target) return;
            var agent = target.GetComponent<NavMeshAgent>();
            if (agent && agent.enabled && agent.isOnNavMesh) agent.Move(Vector3.ClampMagnitude(Vector3.ProjectOnPlane(point - target.transform.position, Vector3.up), distance));
        }
    }
}
