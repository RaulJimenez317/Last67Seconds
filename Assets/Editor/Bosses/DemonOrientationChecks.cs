using System;
using System.Reflection;
using Codice.Bosses;
using Codice.Characters;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

public static class DemonOrientationChecks
{
    public static string Run()
    {
        var sceneBoss=Object.FindFirstObjectByType<BossCharacter>();
        if(!Application.isPlaying||!sceneBoss)throw new Exception("Run in Coliseo with an active boss.");
        var player=new GameObject("Demon orientation test player").AddComponent<PlayerCharacter>();player.enabled=false;
        player.GetComponent<CharacterController>().enabled=false;
        var boss=Object.Instantiate(AssetDatabase.LoadAssetAtPath<BossCharacter>("Assets/01_Prefabs/Bosses/DemonLord.prefab"),sceneBoss.transform.position,Quaternion.identity);boss.enabled=false;
        int checks=0;var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
        void Set(string field,object value)=>typeof(BossCharacter).GetField(field,flags).SetValue(boss,value);
        try
        {
            foreach(int level in new[]{10,20})
            foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                player.transform.position=boss.transform.position+direction*7;boss.Initialize(player,level);
                Check(Vector3.Dot(boss.transform.forward,direction)>.999f,"logical facing at spawn");
                Check(Vector3.Dot(-boss.visual.forward,direction)>.999f,"demon model faces player");
                Check(!boss.GetComponent<NavMeshAgent>().updateRotation,"navigation does not override rotation");
                Set("<State>k__BackingField",BossState.Windup);Set("windup",10f);Set("facing",direction);
                Set("retreatRemaining",.4f);Set("retreatDirection",-direction);
                boss.SendMessage("LateUpdate");
                Check(Vector3.Dot(-boss.visual.forward,direction)>.999f,"retreat keeps front toward aim");
                Set("<State>k__BackingField",BossState.Windup);Set("windup",0f);Set("<CurrentAttack>k__BackingField",BossAttack.Sweep);
                boss.SendMessage("Update");
                Check(boss.State==BossState.Recovery&&Vector3.Dot(-boss.visual.forward,direction)>.999f,"attack recovery preserves visual correction");
                Check(boss.Evolved==(level>=15),"base and evolved forms");
            }
            var result="DEMON_ORIENTATION_PASSED: "+checks;Debug.Log(result);return result;
        }
        finally{boss.gameObject.SetActive(false);Object.Destroy(boss.gameObject);Object.Destroy(player.gameObject);}
    }
}
