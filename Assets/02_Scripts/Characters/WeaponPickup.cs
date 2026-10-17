using UnityEngine;

namespace Codice.Characters
{
    public sealed class WeaponPickup : MonoBehaviour, IPlayerInteractable
    {
        public WeaponInstance weapon = new WeaponInstance(WeaponKind.Sword);
        public GameObject visual;
        public void Interact(PlayerCharacter player)
        {
            if (weapon == null || !player.State.IsAlive || player.ControlsBlocked) return;
            var previous = player.State.Equip(weapon);
            weapon = previous;
            if (previous == null) Destroy(gameObject);
            else
            {
                name = "Dropped " + previous.kind + " T" + previous.tier;
                player.GetComponent<PlayerCombat>()?.AddPickupVisual(this);
            }
        }
    }
}
