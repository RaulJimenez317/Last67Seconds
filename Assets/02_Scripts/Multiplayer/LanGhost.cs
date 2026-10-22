using UnityEngine;
namespace Codice.Multiplayer
{
    public sealed class LanGhost : MonoBehaviour
    {
        public LanEntity snapshot;
        Animator animator;string state;
        public void Apply(LanEntity value,bool first)
        {
            snapshot=value;if(value.key.StartsWith("Player:")){var equipment=GetComponent<LanHeldWeapon>()??gameObject.AddComponent<LanHeldWeapon>();equipment.Apply(value.weapon);}if(value.key=="Beam"){var line=GetComponent<LineRenderer>();line.SetPosition(0,value.position);line.SetPosition(1,value.end);}if(first)transform.SetPositionAndRotation(value.position,value.rotation);
            if(!animator)animator=GetComponentInChildren<Animator>();
            if(animator&&animator.runtimeAnimatorController&&!string.IsNullOrEmpty(value.animation)&&state!=value.animation&&animator.HasState(0,Animator.StringToHash(value.animation)))
            {state=value.animation;animator.CrossFadeInFixedTime(state,.08f);}
        }
        void Update(){if(snapshot==null)return;float t=1-Mathf.Exp(-18*Time.unscaledDeltaTime);transform.position=Vector3.Lerp(transform.position,snapshot.position,t);transform.rotation=Quaternion.Slerp(transform.rotation,snapshot.rotation,t);}
        void OnGUI()
        {
            if(snapshot==null||snapshot.maxHealth<=0||!Camera.main)return;
            var p=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*2.6f);
            if(p.z<=0||p.z>35)return;
            GUI.Box(new Rect(p.x-90,Screen.height-p.y,180,42),snapshot.name+"\n"+Mathf.CeilToInt(snapshot.health)+" / "+Mathf.CeilToInt(snapshot.maxHealth));
        }
    }
}
