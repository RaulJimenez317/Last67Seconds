using UnityEngine;

namespace Codice.Characters
{
    // Swept collision prevents tunnelling. Presentation belongs to the effects phase.
    public sealed class PlayerProjectile : MonoBehaviour
    {
        GameObject owner;
        Vector3 direction;
        float damage, expires;
        LayerMask mask;
        bool critical;
        public void Initialize(GameObject owner, Vector3 direction, float damage, LayerMask mask, bool critical = false)
        { this.critical = critical; this.owner = owner; this.direction = direction.normalized; this.damage = damage; this.mask = mask; expires = Time.time + 3; }
        void Update()
        {
            if (Time.time >= expires) { Destroy(gameObject); return; }
            float distance = 35 * Time.deltaTime;
            var hits = Physics.RaycastAll(transform.position, direction, distance, mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (owner && hit.transform.IsChildOf(owner.transform)) continue;
                hit.collider.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo { amount = damage, critical = critical, hasImpactPoint = true, impactPoint = hit.point, direction = direction, source = owner });
                Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                Destroy(gameObject); return;
            }
            transform.position += direction * distance;
        }
    }
}
