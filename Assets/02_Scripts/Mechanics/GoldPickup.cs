using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class GoldPickup : MonoBehaviour
    {
        public int Amount { get; private set; }
        bool collected;
        PlayerCharacter target;
        public static GoldPickup Drop(Vector3 position, int amount, PlayerCharacter player)
        {
            var root = new GameObject("Oro +" + amount);
            root.transform.position = position;
            var pickup = root.AddComponent<GoldPickup>(); pickup.Amount = amount; pickup.target = player;
            var trigger = root.AddComponent<SphereCollider>(); trigger.radius = .65f; trigger.isTrigger = true;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var prefab = Resources.Load<GameObject>("GoldCoin");
            if (prefab) Instantiate(prefab, root.transform);
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.transform.SetParent(root.transform, false); visual.transform.localScale = new Vector3(.35f,.045f,.35f);
                Destroy(visual.GetComponent<Collider>());
            }
            // The imported coin has an offset pivot. Centre the visual on its
            // pickup collider before rotating it, otherwise it orbits metres away.
            var visuals = root.GetComponentsInChildren<Renderer>();
            if (visuals.Length > 0)
            {
                var bounds = visuals[0].bounds;
                foreach (var renderer in visuals) bounds.Encapsulate(renderer.bounds);
                Vector3 correction = root.transform.position - bounds.center;
                foreach (Transform child in root.transform) child.position += correction;
            }
            // Rest the rendered bottom on scenery, ignoring actors and pickup triggers.
            var groundHits = Physics.RaycastAll(position + Vector3.up * 2, Vector3.down, 30, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(groundHits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var ground in groundHits)
            {
                if (ground.normal.y < .5f || ground.collider.GetComponentInParent<IDamageReceiver>() != null || ground.transform.IsChildOf(root.transform)) continue;
                var renderers = root.GetComponentsInChildren<Renderer>();
                float bottom = root.transform.position.y;
                if (renderers.Length > 0) { bottom = renderers[0].bounds.min.y; foreach (var r in renderers) bottom = Mathf.Min(bottom, r.bounds.min.y); }
                root.transform.position += Vector3.up * (ground.point.y + .02f - bottom);
                break;
            }
            return pickup;
        }
        public bool Collect(PlayerCharacter player)
        {
            if(collected || !player || !player.State.IsAlive || Amount <= 0) return false;
            collected = true; player.State.AddGold(Amount); gameObject.SetActive(false); Destroy(gameObject); return true;
        }
        void OnTriggerEnter(Collider other) { Collect(other.GetComponentInParent<PlayerCharacter>()); }
        void Update()
        {
            transform.Rotate(0,90 * Time.deltaTime,0);
            if(!target || !target.State.IsAlive || Time.timeScale == 0) return;
            Vector3 destination=target.transform.position+Vector3.up*.5f;
            if(Vector3.Distance(transform.position,destination)<1.25f)
            {
                if(Physics.Linecast(transform.position,destination,out var hit,~0,QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(target.transform))return;
                Collect(target);
            }
        }
        public static void CollectRemaining(PlayerCharacter player)
        { foreach(var coin in FindObjectsByType<GoldPickup>(FindObjectsSortMode.None)) coin.Collect(player); }
    }
}
