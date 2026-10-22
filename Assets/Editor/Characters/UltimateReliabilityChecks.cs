using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using Object=UnityEngine.Object;
public static class UltimateReliabilityChecks
{
 const string Report="Library/UltimateReliabilityChecks.txt";
 [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("UltimateChecks",false)){SessionState.SetBool("UltimateChecks",false);Run();}};EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/UltimateChecks.request"))return;File.Delete("Library/UltimateChecks.request");File.WriteAllText(Report,"");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.AppendAllText(Report,"Save scene first");return;}UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("UltimateChecks",true);EditorApplication.isPlaying=true;};}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);File.AppendAllText(Report,"PASS "+text+"\n");}
 static void Pump(){EditorApplication.QueuePlayerLoopUpdate();foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))if(!e.name.StartsWith("Ultimate fixture"))e.SetPaused(true);}
 static async Task Until(Func<bool> test){double end=EditorApplication.timeSinceStartup+60;while(!test()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(25);}}
 static async void Run(){EditorApplication.update+=Pump;try{
  Application.runInBackground=true;await Until(()=>Object.FindFirstObjectByType<MainMenuUI>());RunNavigation.StartGame(PlayerClass.Mage,6);await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.CombatActive);
  var p=RunDirector.Instance.Player;p.enabled=false;var m=p.GetComponent<PlayerMechanics>();var c=p.GetComponent<PlayerCombat>();
  foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
  var enemy=Object.Instantiate(Resources.Load<BasicEnemy>("EnemyVariants/Slime"),p.transform.position+p.transform.forward*3,Quaternion.identity);enemy.name="Ultimate fixture";enemy.healthMultiplier=200;enemy.Initialize(p,6);enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;enemy.SetPaused(false);Physics.SyncTransforms();p.view.transform.LookAt(enemy.GetComponent<Collider>().bounds.center);
  p.State.Equip(new WeaponInstance(WeaponKind.Sword));await Task.Delay(250);p.State.ActiveWeapon.ultimateCharge=100;
  Check(c.TryAttack(),"basic swing starts");Check(m.RequestUltimate()&&m.UltimateQueued,"Q queued during swing");Check(!c.TryAttack(),"held basic attack cannot steal queued Q");await Until(()=>m.UltimateActive);Check(!m.UltimateQueued&&p.State.ActiveWeapon.ultimateCharge<1,"queued Q starts and consumes charge once");await Until(()=>!m.UltimateActive);
  foreach(var kind in new[]{WeaponKind.Staff,WeaponKind.Sword,WeaponKind.Hammer,WeaponKind.Dagger,WeaponKind.Scythe,WeaponKind.Musket}){
   p.State.Equip(new WeaponInstance(kind));p.State.TickMana(100);p.State.ActiveWeapon.ultimateCharge=100;await Task.Delay(120);float hp=enemy.Health;Check(m.TryUltimate(),"starts "+kind);await Until(()=>!m.UltimateActive);Check(enemy.Health<hp,"damages "+kind);
  }
  p.State.Equip(new WeaponInstance(WeaponKind.Staff));p.State.SpendMana(p.State.Mana);p.State.ActiveWeapon.ultimateCharge=100;Check(!m.RequestUltimate()&&p.State.ActiveWeapon.ultimateCharge==100,"low mana preserves charge");
  p.State.ActiveWeapon.ultimateCharge=99.8f;RunHUD.Instance.Refresh();Check(RunHUD.Instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t=>t.text=="99%\n[Q]"),"99.8 does not display ready 100");
  var line=typeof(PlayerMechanics).GetMethod("Line",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Vector3 origin=p.view.transform.position;var direction=p.transform.forward;enemy.transform.position=origin+direction*10.3f;Physics.SyncTransforms();
  var targets=(System.Collections.Generic.List<Component>)line.Invoke(m,new object[]{origin,direction,10f,.5f});Check(targets.Contains(enemy),"end of beam hits overlapping enemy volume");
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=origin+direction*5;wall.transform.localScale=new Vector3(4,4,.5f);Physics.SyncTransforms();targets=(System.Collections.Generic.List<Component>)line.Invoke(m,new object[]{origin,direction,10f,.5f});Check(!targets.Contains(enemy),"wall blocks special");Object.Destroy(wall);Object.Destroy(enemy.gameObject);
  File.AppendAllText(Report,"ULTIMATE_CHECKS_PASSED\n");
 }catch(Exception e){File.AppendAllText(Report,"FAILED "+e);Debug.LogException(e);}finally{EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}}
}
// Run after leaving Play Mode.
