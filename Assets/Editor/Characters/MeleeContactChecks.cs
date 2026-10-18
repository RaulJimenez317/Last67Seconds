using System;
using System.IO;
using System.Reflection;
using Codice.Characters;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public class MeleeTestTarget : MonoBehaviour, IDamageReceiver
{
    public int hits;
    public void ReceiveDamage(DamageInfo hit){hits++;}
}
public static class MeleeContactChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(PlayerCombat c,string name,object value)=>typeof(PlayerCombat).GetField(name,Private).SetValue(c,value);
    static void Call(PlayerCombat c,string name,params object[] args)=>typeof(PlayerCombat).GetMethod(name,Private).Invoke(c,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText("Library/MeleeChecks.txt","PASS "+message+"\n");}
    [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(File.Exists("Library/MeleeChecks.request")&&!EditorApplication.isPlaying){File.Delete("Library/MeleeChecks.request");Run();}};}
    public static void Run()
    {
        var root=new GameObject("Temporary melee checks");
        try
        {
            File.WriteAllText("Library/MeleeChecks.txt","");
            root.transform.position=new Vector3(1000,1000,1000);
            var player=root.AddComponent<PlayerCharacter>();player.State.Initialize(PlayerClass.Warrior);
            var cam=new GameObject("View");cam.transform.SetParent(root.transform,false);cam.transform.localPosition=Vector3.up*1.6f;player.view=cam.AddComponent<Camera>();
            var combat=root.AddComponent<PlayerCombat>();Set(combat,"player",player);
            var target=new GameObject("Target");target.transform.SetParent(root.transform,false);var receiver=target.AddComponent<MeleeTestTarget>();var box=target.AddComponent<BoxCollider>();box.size=Vector3.one*.4f;
            // Targets are siblings of the player so the self-hit filter remains exercised.
            target.transform.SetParent(null,true);
            try
            {
                foreach(var kind in new[]{WeaponKind.Sword,WeaponKind.Axe,WeaponKind.Dagger,WeaponKind.Hammer,WeaponKind.Polearm,WeaponKind.Scythe,WeaponKind.Shield})
                {
                    var weapon=new WeaponInstance(kind);player.State.Equip(weapon);float duration=1/weapon.AttacksPerSecond;
                    Action<float> begin=distance=>{Call(combat,"CancelSwing");Set(combat,"swinging",weapon);Set(combat,"swingDuration",duration);Set(combat,"swingElapsed",0f);receiver.hits=0;target.transform.position=cam.transform.position+Vector3.forward*distance;Physics.SyncTransforms();};
                    begin(PlayerCombat.MeleeRange(kind)-.1f);
                    Call(combat,"UpdateSwing",duration*(PlayerCombat.ContactStart(kind)-.01f));Check(receiver.hits==0,kind+" no damage during preparation");
                    Call(combat,"UpdateSwing",duration*.02f);Check(receiver.hits==1,kind+" damages during contact");
                    Call(combat,"UpdateSwing",duration*.1f);Check(receiver.hits==1,kind+" one hit per swing");
                    begin(1);Call(combat,"UpdateSwing",duration*1.1f);Check(receiver.hits==1&&!combat.IsSwinging,kind+" low frame rate preserves contact");
                    begin(1);Time.timeScale=0;Call(combat,"UpdateSwing",duration*.45f);Check(receiver.hits==0,kind+" pause suspends contact");Time.timeScale=1;
                    begin(PlayerCombat.MeleeRange(kind)+.5f);Call(combat,"UpdateSwing",duration*.45f);Check(receiver.hits==0,kind+" outside range misses");
                    begin(1);target.transform.position=cam.transform.position-Vector3.forward;Physics.SyncTransforms();Call(combat,"UpdateSwing",duration*.45f);Check(receiver.hits==0,kind+" rear target misses");
                    begin(1);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=cam.transform.position+Vector3.forward*.5f;wall.transform.localScale=new Vector3(2,2,.1f);Physics.SyncTransforms();Call(combat,"UpdateSwing",duration*.45f);Check(receiver.hits==0,kind+" wall blocks attack");Object.DestroyImmediate(wall);
                    begin(1);player.State.Equip(new WeaponInstance(WeaponKind.Bow));Call(combat,"UpdateSwing",duration*.45f);Check(receiver.hits==0&&!combat.IsSwinging,kind+" switching cancels swing");
                }
            }finally{Object.DestroyImmediate(target);}
            File.AppendAllText("Library/MeleeChecks.txt","ALL_MELEE_CHECKS_PASSED\n");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Player.prefab");
            var preview=Object.Instantiate(prefab);preview.transform.position=new Vector3(2000,2000,2000);
            try {
                var pc=preview.GetComponent<PlayerCharacter>();pc.State.Initialize(PlayerClass.Warrior);
                var presentation=preview.GetComponent<Codice.Presentation.ActorPresentation>()??preview.AddComponent<Codice.Presentation.ActorPresentation>();
                typeof(Codice.Presentation.ActorPresentation).GetMethod("Start",Private).Invoke(presentation,null);
                var weapons=preview.GetComponent<PlayerCombat>().weaponModels;for(int i=0;i<weapons.Length;i++)weapons[i].SetActive(i==(int)WeaponKind.Dagger);
                presentation.Arms.Rebind();presentation.Arms.Play("Ready",0,0);presentation.Arms.Update(0);
                var dagger=weapons[(int)WeaponKind.Dagger];var mesh=dagger.GetComponentInChildren<MeshFilter>();
                File.AppendAllText("Library/MeleeChecks.txt","DAGGER mesh bounds "+mesh.sharedMesh.bounds+" grip "+mesh.transform.localPosition+" scale "+mesh.transform.localScale+"\n");
                var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-30,0);
                var camera=pc.view;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.2f,.2f);
                var rt=new RenderTexture(960,720,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(960,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();File.WriteAllBytes("Library/DaggerGrip.png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(light.gameObject);
            }finally{Object.DestroyImmediate(preview);}

        }
        catch(Exception e){File.AppendAllText("Library/MeleeChecks.txt","FAILED "+e);Debug.LogException(e);}
        finally{Time.timeScale=1;Object.DestroyImmediate(root);}
    }
}
