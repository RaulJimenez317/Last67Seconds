using UnityEngine;

namespace Codice.Characters
{
    public struct DamageInfo
    {
        public float amount;
        public Vector3 direction;
        public GameObject source;
        public bool breaksGuard;
        public float knockback;
    }
    public interface IDamageReceiver { void ReceiveDamage(DamageInfo hit); }
    public interface IPlayerInteractable { void Interact(PlayerCharacter player); }
}
