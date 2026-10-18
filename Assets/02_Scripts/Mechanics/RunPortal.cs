using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class RunPortal : MonoBehaviour, IPlayerInteractable
    {
        public GameObject visual;
        public bool Available { get; private set; }
        public void SetAvailable(bool value)
        { Available = value; if (visual) visual.SetActive(value); var collider=GetComponent<Collider>();if(collider)collider.enabled = value; }
        public void Interact(PlayerCharacter player)
        {
            if (Available && player && player.State.IsAlive && !player.ControlsBlocked && Vector3.Distance(player.transform.position, transform.position) <= 3.5f && RunDirector.Instance && RunDirector.Instance.Player == player)
                RunDirector.Instance.Travel();
        }
    }
}
