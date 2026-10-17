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
            root.transform.position = position + Vector3.up * .5f;
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
            Vector3 destination=target.transform.position+Vector3.up*.7f;
            if(Vector3.Distance(transform.position,destination)<2.5f)
            {
                if(Physics.Linecast(transform.position,destination,out var hit,~0,QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(target.transform))return;
                transform.position=Vector3.MoveTowards(transform.position,destination,6*Time.deltaTime);
                if(Vector3.Distance(transform.position,destination)<.6f)Collect(target);
            }
        }
        public static void CollectRemaining(PlayerCharacter player)
        { foreach(var coin in FindObjectsByType<GoldPickup>(FindObjectsSortMode.None)) coin.Collect(player); }
    }
}
