using System.Collections.Generic;
using UnityEngine;
namespace Codice.Presentation
{
    public static class PresentationVfx
    {
        static readonly List<GameObject> live=new List<GameObject>();
        public static int ActiveCount {get{live.RemoveAll(x=>!x);return live.Count;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>live.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Subscribe()
        {
            Codice.Characters.CombatFeedback.Resolved -= Damage;
            Codice.Characters.CombatFeedback.Resolved += Damage;
        }
        static void Damage(Codice.Characters.CombatFeedback.Result hit)
        {
            if(hit.amount<=0 || !hit.target)return;
            if(!(hit.target is Codice.Characters.PlayerCharacter) && !(hit.target is Codice.Enemies.BasicEnemy) && !(hit.target is Codice.Bosses.BossCharacter))return;
            var l=PresentationLibrary.Load();
            if(l)Spawn(l.blood,hit.position,Quaternion.identity,.55f,2.5f);
        }
        public static void SwordSwing(Vector3 point,Vector3 forward)
        {var l=PresentationLibrary.Load();if(l)Spawn(l.swordSparks,point,Quaternion.LookRotation(forward),.2f,.6f);}
        public static void MagicCast(Vector3 point,Vector3 forward)
        {var l=PresentationLibrary.Load();if(l)Spawn(l.magicCast,point,Quaternion.LookRotation(forward),.2f,.6f);}
        public static void GunSmoke(Vector3 point,Vector3 forward)
        {var l=PresentationLibrary.Load();if(l)Spawn(l.gunSmoke,point,Quaternion.LookRotation(forward),1,1.8f);}
        static void Spawn(GameObject prefab,Vector3 point,Quaternion rotation,float scale,float lifetime)
        {
            if(!prefab)return;if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EmitEffect(prefab.name,point,scale,lifetime);live.RemoveAll(x=>!x);if(live.Count>=40)return;
            var go=Object.Instantiate(prefab,point,rotation);go.transform.localScale*=scale;go.name="VFX • "+prefab.name;live.Add(go);
            foreach(var system in go.GetComponentsInChildren<ParticleSystem>()){var main=system.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;system.Play();}
            Object.Destroy(go,lifetime);
        }
        public static void Impact(Vector3 point,Vector3 normal){var l=PresentationLibrary.Load();if(l)Spawn(l.impact,point,normal.sqrMagnitude>.001f?Quaternion.LookRotation(normal):Quaternion.identity,.3f,1.2f);}
        public static void Footstep(Vector3 point){var l=PresentationLibrary.Load();if(Physics.Raycast(point+Vector3.up*.2f,Vector3.down,out var hit,.6f,~0,QueryTriggerInteraction.Ignore)){GameAudio.PlayAt(SoundCue.Footstep,point);if(l)Spawn(l.dust,hit.point+hit.normal*.03f,Quaternion.identity,.045f,.65f);}}
        public static void Explosion(Vector3 point){GameAudio.PlayAt(SoundCue.Explosion,point);var l=PresentationLibrary.Load();if(l)Spawn(l.explosion,point,Quaternion.identity,.65f,2.5f);}
        public static void Muzzle(Vector3 point,Vector3 forward){var l=PresentationLibrary.Load();if(l)Spawn(l.muzzle,point,Quaternion.LookRotation(forward),.12f,.18f);}
    }
}
