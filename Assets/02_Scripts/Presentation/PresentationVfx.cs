using System.Collections.Generic;
using UnityEngine;
namespace Codice.Presentation
{
    public static class PresentationVfx
    {
        static readonly List<GameObject> live=new List<GameObject>();
        public static int ActiveCount {get{live.RemoveAll(x=>!x);return live.Count;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>live.Clear();
        static void Spawn(GameObject prefab,Vector3 point,Quaternion rotation,float scale,float lifetime)
        {
            if(!prefab)return;live.RemoveAll(x=>!x);if(live.Count>=40)return;
            var go=Object.Instantiate(prefab,point,rotation);go.transform.localScale*=scale;go.name="VFX • "+prefab.name;live.Add(go);
            foreach(var system in go.GetComponentsInChildren<ParticleSystem>()){var main=system.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;system.Play();}
            Object.Destroy(go,lifetime);
        }
        public static void Impact(Vector3 point,Vector3 normal){var l=PresentationLibrary.Load();if(l)Spawn(l.impact,point,normal.sqrMagnitude>.001f?Quaternion.LookRotation(normal):Quaternion.identity,.3f,1.2f);}
        public static void Footstep(Vector3 point){var l=PresentationLibrary.Load();if(l&&Physics.Raycast(point+Vector3.up*.2f,Vector3.down,out var hit,.6f,~0,QueryTriggerInteraction.Ignore))Spawn(l.dust,hit.point+hit.normal*.03f,Quaternion.identity,.16f,1);}
        public static void Explosion(Vector3 point){var l=PresentationLibrary.Load();if(l)Spawn(l.explosion,point,Quaternion.identity,.65f,2.5f);}
        public static void Muzzle(Vector3 point,Vector3 forward){var l=PresentationLibrary.Load();if(l)Spawn(l.muzzle,point,Quaternion.LookRotation(forward),.12f,.18f);}
    }
}
