using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class DestructiblePillar : MonoBehaviour, IDamageReceiver
    {
        [Min(1)] public float maxHealth = 180;
        public float Health { get; private set; }
        void Awake() { Health = maxHealth; }
        public void ReceiveDamage(DamageInfo hit)
        {
            if (Health <= 0 || hit.amount <= 0 || float.IsNaN(hit.amount) || float.IsInfinity(hit.amount)) return;
            float actual = Mathf.Min(Health, hit.amount);
            Health = Mathf.Max(0, Health - hit.amount);
            CombatFeedback.Report(this, hit, actual, false);
            if (Health > 0) return;
            gameObject.SetActive(false);
        }
    }
}
