using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Codice.Characters;
using Codice.Enemies;
using Codice.Presentation;
using Codice.Mechanics;
using Codice.UI;
using Object=UnityEngine.Object;
public static class DualScalingChecks
{
 const string Report="Library/DualScalingChecks.txt";
 [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DualChecks",false)){SessionState.SetBool("DualChecks",false);Run();}};EditorApplication.delayCall+=()=>{if(!File.Exists("Library/DualChecks.request")||EditorApplication.isPlaying)return;File.Delete("Library/DualChecks.request");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.WriteAllText(Report,"Save current scene first");return;}File.WriteAllText(Report,"");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("DualChecks",true);EditorApplication.isPlaying=true;};}
 static void Check(bool value,string name){if(!value)throw new Exception(name);File.AppendAllText(Report,"PASS "+name+"\n");}
 static async Task Until(Func<bool> test){double end=EditorApplication.timeSinceStartup+60;while(!test()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(50);}}
 static void Pump(){EditorApplication.QueuePlayerLoopUpdate();foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))e.SetPaused(true);}
 static async void Run(){EditorApplication.update+=Pump;try{
  Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).maximized=true;
  await Until(()=>Object.FindFirstObjectByType<MainMenuUI>());RunNavigation.StartGame(PlayerClass.Warrior,6);await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player&&RunDirector.Instance.CombatActive);
  var p=RunDirector.Instance.Player;var pose=p.GetComponent<ActorPresentation>();var combat=p.GetComponent<PlayerCombat>();
  foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
  p.view.transform.localRotation=Quaternion.Euler(-15,80,0);
  foreach(var kind in new[]{WeaponKind.Sword,WeaponKind.Dagger}){
   p.State.Equip(new WeaponInstance(kind,2));p.State.SelectWeapon(1-p.State.ActiveSlot);p.State.Equip(new WeaponInstance(kind,1));await Task.Delay(700);
   Check(pose.CurrentState=="DualIdle","idle "+kind);var left=p.GetComponentsInChildren<Transform>().First(t=>t.name=="Dual offhand");var right=combat.weaponModels[(int)kind];Check(Vector3.Distance(left.position,right.transform.position)>.12f,"separate grips "+kind);
   Check(Vector3.Dot(right.transform.up,p.view.transform.up)>.8f&&Vector3.Dot(left.up,p.view.transform.up)>.8f,"upright blades "+kind);
   var cam=p.view;var oldTarget=cam.targetTexture;float oldAspect=cam.aspect;var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.aspect=1280f/720;cam.Render();var oldActive=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Library/Dual"+kind+"Review.png",texture.EncodeToPNG());RenderTexture.active=oldActive;cam.targetTexture=oldTarget;cam.aspect=oldAspect;Object.Destroy(texture);Object.Destroy(rt);await Task.Delay(200);pose.Attack(false,.5f);Check(pose.CurrentState==(kind==WeaponKind.Dagger?"DualStab":"DualSlice"),"attack "+kind);await Task.Delay(700);
  }
  for(int type=0;type<8;type++){
   string path=type<4?"Assets/01_Prefabs/Enemies/"+(EnemyKind)type+".prefab":"Assets/Resources/EnemyVariants/"+(EnemyKind)(type-4)+".prefab";
   var enemy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<BasicEnemy>(path));enemy.gameObject.SetActive(false);float health=0,damage=0;
   for(int round=1;round<=25;round++){enemy.Initialize(p,round);Check(enemy.MaxHealth>health&&enemy.AttackDamage>damage,"scaling "+type+" round "+round);health=enemy.MaxHealth;damage=enemy.AttackDamage;}
   Object.Destroy(enemy.gameObject);
  }
  Check(BasicEnemy.SpeedScale(25)<=1.12f,"bounded movement speed");File.AppendAllText(Report,"DUAL_SCALING_PASSED\n");
 }catch(Exception e){File.AppendAllText(Report,"FAILED "+e);Debug.LogException(e);}finally{Time.timeScale=1;EditorApplication.update-=Pump;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).maximized=false;EditorApplication.isPlaying=false;}}
}
