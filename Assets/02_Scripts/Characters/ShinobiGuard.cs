using UnityEngine;
using UnityEngine.InputSystem;
using Codice.Presentation;
using Codice.Mechanics;
namespace Codice.Characters
{
    [DefaultExecutionOrder(-50)]
    public sealed class ShinobiGuard:MonoBehaviour
    {
        public float Posture{get;private set;}=100;
        public bool Guarding{get;private set;}
        public bool Perfect=>Guarding&&Time.time<=perfectUntil;
        public bool Broken=>Time.time<brokenUntil;
        public bool ShowPosture=>Time.time<showUntil;
        public bool RecentPerfect=>Time.time<flashUntil;
        float showUntil,flashUntil;
        bool wasHeld; PlayerCharacter player;float perfectUntil=-1,nextParry,brokenUntil,regenAt;
        void Awake(){player=GetComponent<PlayerCharacter>();}
        public void SetGuard(bool held)
        {
            bool pressed=held&&!wasHeld;wasHeld=held;
            bool allowed=Posture>0&&player.State.IsAlive&&!player.ControlsBlocked&&!player.IsStaggered&&!Broken&&Time.timeScale>0&&!Codice.UI.RunNavigation.Loading&&!(Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.ResultsVisible)&&!GetComponent<PlayerMechanics>().UltimateActive;
            if(pressed&&allowed&&Time.time>=nextParry){perfectUntil=Time.time+.2f;nextParry=Time.time+.5f;}
            Guarding=held&&allowed;
        }
        void Update(){if(Codice.Multiplayer.LanSession.Client)return;SetGuard(Codice.Multiplayer.LanAvatar.IsRemote(this)?GetComponent<Codice.Multiplayer.LanAvatar>().block:Cursor.lockState==CursorLockMode.Locked&&Mouse.current!=null&&Mouse.current.rightButton.isPressed);if(player.State.IsAlive&&!player.ControlsBlocked&&!Guarding&&Time.time>=regenAt)Posture=Mathf.Min(100,Posture+20*Time.deltaTime);}
        public float Resolve(DamageInfo hit)
        {
            if(hit.amount<=0||!Guarding||Broken||hit.secondary||hit.direction.sqrMagnitude<.01f||Vector3.Dot(transform.forward,-hit.direction.normalized)<.5f)return hit.amount;
            showUntil=Time.time+2.5f;
            if(Perfect){flashUntil=Time.time+.45f;GetComponent<ActorPresentation>()?.Play("ShinobiDeflect",.28f,true);GameAudio.Play(SoundCue.Block);PresentationVfx.SwordSwing(transform.position+Vector3.up+transform.forward,transform.forward);if(hit.source){var enemy=hit.source.GetComponentInParent<IDamageReceiver>() as Component;if(CombatTargets.IsEnemy(enemy))CombatTargets.Stun(enemy,.5f);}return 0;}
            GetComponent<ActorPresentation>()?.Play("ShinobiGuardHit",.3f,true);
            Posture=Mathf.Max(0,Posture-Mathf.Max(15,hit.amount*.8f));regenAt=Time.time+1.5f;
            if(Posture<=0){brokenUntil=Time.time+1.2f;Guarding=false;player.ApplyBossKnockdown();}
            return hit.amount*.4f;
        }
        void OnDisable(){Guarding=false;perfectUntil=-1;}
    }
}
