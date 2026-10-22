using UnityEngine;

namespace Codice.Characters
{
    // Swept collision prevents tunnelling. Presentation belongs to the effects phase.
    public sealed class PlayerProjectile : MonoBehaviour
    {
        public string NetworkVisual => star?"Star":magic?"Magic":"Arrow";
        GameObject owner;
        Vector3 direction;
        float damage, expires;
        LayerMask mask;
        bool critical;
        bool magic; Transform star;
        public void AddShurikenVisual(){transform.rotation=Quaternion.LookRotation(direction);var prefab=Resources.Load<GameObject>("Shuriken");if(prefab)star=Instantiate(prefab,transform,false).transform;}
        public void AddMagicVisual()
        {
            magic=true;
            var library=Codice.Presentation.PresentationLibrary.Load();
            if(library&&library.magicProjectile)Instantiate(library.magicProjectile,transform,false);
        }
        public void AddArrowVisual()
        {
            var prefab = Resources.Load<GameObject>("PlayerArrow");
            if (!prefab) return;
            var model = Instantiate(prefab, transform, false);
            model.transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        public void Initialize(GameObject owner, Vector3 direction, float damage, LayerMask mask, bool critical = false)
        { this.critical = critical; this.owner = owner; this.direction = direction.normalized; this.damage = damage; this.mask = mask; expires = Time.time + 3; }
        void Update()
        {
            if (Time.time >= expires) { Destroy(gameObject); return; }
            if(star)star.Rotate(0,0,1500*Time.deltaTime);
            float distance = 35 * Time.deltaTime;
            var hits = Physics.RaycastAll(transform.position, direction, distance, mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (owner && hit.transform.IsChildOf(owner.transform)) continue;
                if(Codice.Multiplayer.LanSession.Active&&hit.collider.GetComponentInParent<PlayerCharacter>())continue;
                hit.collider.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo { amount = damage, critical = critical, hasImpactPoint = true, impactPoint = hit.point, direction = direction, source = owner });
                if(magic)Codice.Presentation.PresentationVfx.Explosion(hit.point);
                else Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                Destroy(gameObject); return;
            }
            transform.position += direction * distance;
        }
    }
}
