using Codice.Mechanics;
using UnityEngine;

namespace Codice.Characters
{
    // One selection rule for the E key and its HUD prompt, including child colliders.
    public static class InteractionTarget
    {
        public static Component Find(PlayerCharacter player)
        {
            if (!player || !player.view || !player.State.IsAlive || player.ControlsBlocked) return null;
            var origin = player.view.transform.position;
            Component best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var collider in Physics.OverlapSphere(player.transform.position + Vector3.up, 3.5f, player.interactionMask, QueryTriggerInteraction.Collide))
            {
                if (collider.transform.IsChildOf(player.transform)) continue;
                var candidate = collider.GetComponentInParent<IPlayerInteractable>() as Component;
                if (!candidate || !candidate.gameObject.activeInHierarchy) continue;
                if (candidate is TreasureChest chest && chest.Opened || candidate is Merchant shop && !shop.Available ||
                    candidate is LootPickup loot && !loot.Available || candidate is RunPortal portal && !portal.Available) continue;
                Vector3 delta = collider.bounds.center - origin;
                float distance = Vector3.Distance(origin, collider.ClosestPoint(origin));
                float alignment = Vector3.Dot(player.view.transform.forward, delta.normalized);
                if (distance > 3 || alignment < .65f) continue;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, player.interactionMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.IsChildOf(player.transform) || hit.transform.IsChildOf(candidate.transform)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                float score = alignment * 4 - distance;
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }
    }
}
