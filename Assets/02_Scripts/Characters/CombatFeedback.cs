using System;
using UnityEngine;

namespace Codice.Characters
{
    // Observational combat result: presentation never participates in damage calculation.
    public static class CombatFeedback
    {
        public struct Result
        {
            public Component target;
            public DamageInfo hit;
            public float amount;
            public bool blocked;
            public Vector3 position;
        }
        public static event Action<Result> Resolved;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Resolved = null; }
        public static void Report(Component target, DamageInfo hit, float amount, bool blocked)
        {
            var collider = target.GetComponent<Collider>();
            Resolved?.Invoke(new Result { target = target, hit = hit, amount = amount, blocked = blocked,
                position = hit.hasImpactPoint ? hit.impactPoint : collider ? collider.bounds.center : target.transform.position + Vector3.up });
        }
    }
}
