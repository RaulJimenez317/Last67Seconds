using Codice.Characters;
using UnityEngine;
namespace Codice.Multiplayer
{
    public sealed class LanAvatar : MonoBehaviour
    {
        public int id; public PlayerClass playerClass; public bool remote;
        public bool block; public Vector2 move; public float yaw,pitch;
        public bool jump,dash,attack; float vertical,dashUntil; Vector3 dashDirection;
        public static bool IsRemote(Component actor)=>actor&&actor.GetComponent<LanAvatar>()?.remote==true;
        public void Motor(PlayerCharacter player,float dt)
        {
            if(!player.State.IsAlive||player.ControlsBlocked)return;
            var controller=player.GetComponent<CharacterController>();
            transform.rotation=Quaternion.Euler(0,yaw,0);player.view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            if(controller.isGrounded&&vertical<0)vertical=-2;
            if(jump&&controller.isGrounded)vertical=Mathf.Sqrt(player.jumpHeight*-4*Physics.gravity.y);
            jump=false;vertical+=Physics.gravity.y*2*dt;
            var direction=transform.TransformDirection(new Vector3(move.x,0,move.y));
            if(dash&&player.TryDash(direction)){dashUntil=Time.time+.3f;dashDirection=direction.sqrMagnitude>.01f?direction:transform.forward;}dash=false;
            controller.Move(((Time.time<dashUntil?dashDirection*18:player.IsStaggered?Vector3.zero:direction*player.State.MoveSpeed*player.MovementMultiplier)+Vector3.up*vertical)*dt);
            if(transform.position.y < -20)player.State.Damage(player.State.MaxHealth);
        }
    }
}
