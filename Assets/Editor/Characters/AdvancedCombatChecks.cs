using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class AdvancedCombatChecks
{
 const string Report="Library/AdvancedCombatChecks.txt";
 [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("AdvancedCombat",false)){SessionState.SetBool("AdvancedCombat",false);Play();}};EditorApplication.delayCall+=()=>{if(!File.Exists("Library/AdvancedCombat.request")||EditorApplication.isPlaying)return;File.Delete("Library/AdvancedCombat.request");try{if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before tests");File.WriteAllText(Report,"ADVANCED COMBAT\n");Assets();EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("AdvancedCombat",true);EditorApplication.isPlaying=true;}catch(Exception e){Log("FAILED: "+e);}};}
 static void Log(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
 static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
 static Material Mat(string name,Color color,bool particles=false){string path="Assets/Resources/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(particles?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,path);}return m;}
 static void Part(Transform root,PrimitiveType type,Vector3 p,Vector3 size,Material mat){var go=GameObject.CreatePrimitive(type);go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;}
 static void Assets(){
  Mat("SpecialGlow",Color.white,true);var steel=Mat("ShinobiSteel",new Color(.6f,.66f,.72f));var grip=Mat("ShinobiGrip",new Color(.13f,.035f,.05f));
  if(!Resources.Load<GameObject>("Katana")){var root=new GameObject("Katana");Part(root.transform,PrimitiveType.Cube,new Vector3(0,-.08f,0),new Vector3(.055f,.25f,.055f),grip);Part(root.transform,PrimitiveType.Cube,new Vector3(0,.06f,0),new Vector3(.17f,.025f,.09f),steel);for(int i=0;i<12;i++)Part(root.transform,PrimitiveType.Cube,new Vector3(.07f*Mathf.Pow(i/11f,2),.105f+i*.073f,0),new Vector3(.05f*(1-i/15f),.08f,.018f),steel);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Katana.prefab");Object.DestroyImmediate(root);}
  if(!Resources.Load<GameObject>("Shuriken")){var root=new GameObject("Shuriken");for(int i=0;i<4;i++){var blade=GameObject.CreatePrimitive(PrimitiveType.Cube);blade.transform.SetParent(root.transform);blade.transform.localScale=new Vector3(.055f,.28f,.018f);blade.transform.localRotation=Quaternion.Euler(0,0,i*45);Object.DestroyImmediate(blade.GetComponent<Collider>());blade.GetComponent<Renderer>().sharedMaterial=steel;}PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Shuriken.prefab");Object.DestroyImmediate(root);}
  if(!Resources.Load<GameObject>("ShinobiVisual")){var root=new GameObject("Authoring");try{var model=CharacterSetup.Model("Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/Characters/fbx/Rogue_Hooded.fbx",root.transform,1.8f,"rogue");foreach(var r in model.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/ShinobiVisual.prefab");}finally{Object.DestroyImmediate(root);}}
  AssetDatabase.SaveAssets();
 }
 static async Task Until(Func<bool> f,string label){double end=EditorApplication.timeSinceStartup+60;while(!f()){if(EditorApplication.timeSinceStartup>end)throw new Exception(label);await Task.Delay(50);}}
 static void Pump(){EditorApplication.QueuePlayerLoopUpdate();foreach(var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))if(e.name!="Combat fixture")e.SetPaused(true);}
 static async void Play(){EditorApplication.update+=Pump;try{
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).maximized=true; Application.runInBackground=true;await Until(()=>Object.FindFirstObjectByType<MainMenuUI>(),"menu");await Task.Delay(500);
  var menu=Object.FindFirstObjectByType<MainMenuUI>();menu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="INICIAR JUEGO").onClick.Invoke();await Task.Delay(300);ScreenCapture.CaptureScreenshot("Library/ShinobiClasses.png");await Task.Delay(500);
  Check(menu.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b=>b.name.StartsWith("JUGAR COMO"))==5,"five classes");RunNavigation.StartGame(PlayerClass.Shinobi,6);
  await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.CombatActive&&RunHUD.Instance,"shinobi run");
  var p=RunDirector.Instance.Player;var m=p.GetComponent<PlayerMechanics>();var c=p.GetComponent<PlayerCombat>();var guard=p.GetComponent<ShinobiGuard>();
  foreach(var enemy in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
  var stars=p.State.GetWeapon(1);Check(stars.Capacity==30&&stars.ammunition==30,"thirty starting shuriken");stars.ammunition=0;p.State.TickAmmunition(100);Check(stars.ammunition==0,"no automatic ammo regeneration");
  var supply=LootPickup.Create(p.transform.position,LootKind.Shuriken,8);Check(supply.TryCollect(p)&&stars.ammunition==8,"collect secondary shuriken ammo");
  var arrows=new WeaponInstance(WeaponKind.Bow);p.State.Equip(arrows);arrows.ammunition=0;var bundle=LootPickup.Create(p.transform.position,LootKind.Arrows,8);Check(bundle.TryCollect(p)&&arrows.ammunition==8,"collect arrows");arrows.ammunition=arrows.Capacity;var full=LootPickup.Create(p.transform.position,LootKind.Arrows,8);Check(!full.TryCollect(p),"full ammo leaves pickup intact");Object.Destroy(full.gameObject);p.State.Equip(new WeaponInstance(WeaponKind.Sword){katana=true});
  Check(p.State.ActiveWeapon.katana&&p.State.GetWeapon(1).shuriken,"katana and shuriken loadout");Check(p.classVisuals.Length==5&&p.classVisuals[4].activeSelf,"shinobi visual");
  Check(!guard.ShowPosture,"posture hidden before contact");guard.enabled=false;guard.SetGuard(true);Check(!guard.ShowPosture,"holding guard does not show posture");float posture=guard.Posture;Check(guard.Resolve(new DamageInfo{amount=30,direction=-p.transform.forward,melee=true})==0&&guard.Posture==posture,"perfect parry no damage or posture cost");Check(guard.ShowPosture&&guard.RecentPerfect,"deflect reveals posture and flash");await Task.Delay(60);ScreenCapture.CaptureScreenshot("Library/DeflectHUD.png");await Task.Delay(300);Check(guard.Resolve(new DamageInfo{amount=30,direction=-p.transform.forward,melee=true})==12&&guard.Posture<posture,"late guard reduces damage and loses posture");Check(guard.Resolve(new DamageInfo{amount=30,direction=p.transform.forward,melee=true})==30,"rear attack bypasses guard");guard.Resolve(new DamageInfo{amount=200,direction=-p.transform.forward});Check(guard.Broken&&guard.Posture==0,"posture break");Check(guard.Resolve(new DamageInfo{amount=10,direction=-p.transform.forward})==10,"broken guard takes full damage");guard.SetGuard(false);await Task.Delay(1600);
  p.State.ActiveWeapon.ultimateCharge=100;Check(m.TryUltimate(),"katana ultimate starts");await Until(()=>!m.UltimateActive,"katana finishes");
  p.State.SelectWeapon(1);var aim=p.view.transform.localRotation;p.view.transform.localRotation=Quaternion.Euler(-60,0,0);Check(c.TryAttack(),"shuriken throw");await Until(()=>Object.FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None).Any(s=>s.transform.childCount>0),"shuriken release");Check(Object.FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None).Any(s=>s.transform.childCount>0),"visible projectile");p.view.transform.localRotation=aim;await Task.Delay(1000);
  foreach(var kind in new[]{WeaponKind.Sword,WeaponKind.Staff,WeaponKind.Hammer,WeaponKind.Dagger,WeaponKind.Scythe,WeaponKind.Musket,WeaponKind.Bow}){
   var point=p.transform.position+p.transform.forward*3;
   var prefab=AssetDatabase.LoadAssetAtPath<Codice.Enemies.BasicEnemy>("Assets/01_Prefabs/Enemies/Swordsman.prefab");
   var target=Object.Instantiate(prefab,point,Quaternion.identity);target.name="Combat fixture";target.Initialize(p,1);target.enabled=false;target.SetPaused(false);float hp=target.Health;
   p.State.Equip(new WeaponInstance(kind));p.State.TickMana(100);p.State.ActiveWeapon.ultimateCharge=100;await Task.Delay(200);Check(m.TryUltimate(),kind+" special starts");Check(!c.TryAttack(),kind+" blocks normal attacks during special");
   if(kind==WeaponKind.Staff){await Task.Delay(250);Check(Object.FindObjectsByType<VolumetricLines.VolumetricLineBehavior>(FindObjectsSortMode.None).Length==1,"one persistent volumetric laser");var laser=Object.FindFirstObjectByType<VolumetricLines.VolumetricLineBehavior>();Check(laser.GetComponent<Renderer>().sharedMaterial.shader.isSupported,"laser shader supported");ScreenCapture.CaptureScreenshot("Library/StaffLaserReview.png");}
   await Until(()=>!m.UltimateActive,kind+" finishes");Check(p.State.ActiveWeapon.ultimateCharge<100,kind+" consumes charge");await Task.Delay(400);Check(!target||!target.IsAlive||target.Health<hp,kind+" damages target");if(target)Object.Destroy(target.gameObject);
  }
  p.State.Equip(new WeaponInstance(WeaponKind.Sword));p.State.SelectWeapon(1-p.State.ActiveSlot);p.State.Equip(new WeaponInstance(WeaponKind.Sword));Check(p.State.DualWield,"two swords enable duals");await Task.Delay(500);Check(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Dual offhand"&&t.gameObject.activeInHierarchy),"offhand visible");
  p.State.ActiveWeapon.ultimateCharge=100;Check(m.TryUltimate(),"cancel fixture starts");p.State.SelectWeapon(1-p.State.ActiveSlot);await Task.Delay(100);Check(!m.UltimateActive,"swap cancels special");
  for(int i=0;i<4;i++){
   var variant=Resources.Load<BasicEnemy>("EnemyVariants/"+(EnemyKind)i);Check(variant&&variant.animator&&variant.weaponPivot,"enemy variant "+i);
   var instance=Object.Instantiate(variant,p.transform.position+p.transform.forward*3,Quaternion.identity);instance.Initialize(p,6);instance.SetPaused(false);Check(instance.IsAlive&&instance.MaxHealth>0,"variant initialized "+i);instance.ReceiveDamage(new DamageInfo{amount=9999,source=p.gameObject,ignoreGuard=true});Check(!instance.IsAlive,"variant killable "+i);Object.Destroy(instance.gameObject);
  }
  Check(ProceduralUISprites.Weapon(new WeaponInstance(WeaponKind.Bow){shuriken=true})!=ProceduralUISprites.Weapon(new WeaponInstance(WeaponKind.Bow)),"distinct shuriken icon");
  float previousHealth=0,previousDamage=0;
  for(int round=1;round<=25;round++){Check(BasicEnemy.HealthScale(round)>previousHealth&&BasicEnemy.DamageScale(round)>previousDamage,"enemy scales round "+round);previousHealth=BasicEnemy.HealthScale(round);previousDamage=BasicEnemy.DamageScale(round);}
  p.State.Equip(new WeaponInstance(WeaponKind.Sword));p.State.SelectWeapon(1-p.State.ActiveSlot);p.State.Equip(new WeaponInstance(WeaponKind.Sword));await Task.Delay(600);
  Check(p.GetComponent<ActorPresentation>().CurrentState=="DualIdle","dual idle state");ScreenCapture.CaptureScreenshot("Library/DualIdleReview.png");await Task.Delay(250);
  p.GetComponent<ActorPresentation>().Attack(false,.5f);Check(p.GetComponent<ActorPresentation>().CurrentState=="DualSlice","dual attack state");await Task.Delay(650);
  var catalog=WeaponVisualCatalog.Load();Check(catalog&&catalog.tiers.Length==40,"forty tier models");Check(catalog.icons.Length==42&&catalog.icons.All(s=>s),"forty two weapon icons");
  for(int kind=0;kind<10;kind++)for(int tier=1;tier<=4;tier++){
   var weapon=new WeaponInstance((WeaponKind)kind,tier);p.State.Equip(weapon);await Task.Delay(90);
   var wrapper=c.weaponModels[kind];Check(wrapper.GetComponentsInChildren<Renderer>().Any(),"held "+weapon.kind+" T"+tier);
   var pickup=c.CreatePickup(weapon,p.transform.position+Vector3.up);Check(pickup.visual.name.StartsWith(catalog.Get(weapon).name),"pickup "+weapon.kind+" T"+tier);Object.Destroy(pickup.gameObject);
  }
  foreach(string state in new[]{"ThrowShuriken","StaffIdle","StaffCast","StaffChannel","KatanaSlashA","KatanaSlashB"})Check(p.GetComponent<ActorPresentation>().Arms.HasState(0,Animator.StringToHash(state)),"animation "+state);
  for(int tier=1;tier<=4;tier++){
   p.State.Equip(new WeaponInstance(WeaponKind.Musket,tier));await Task.Delay(150);var gun=c.weaponModels[(int)WeaponKind.Musket];Check(Vector3.Dot(gun.transform.up,p.view.transform.forward)>.99f,"musket points forward T"+tier);
   if(tier==2){ScreenCapture.CaptureScreenshot("Library/MusketGripReview.png");await Task.Delay(100);}
   p.State.Equip(new WeaponInstance(WeaponKind.Bow,tier));await Task.Delay(200);var bow=c.weaponModels[(int)WeaponKind.Bow];var before=bow.transform.position;
   p.GetComponent<ActorPresentation>().Attack(true,.8f);await Task.Delay(180);
   var arrow=p.view.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Nocked arrow");Check(!arrow.gameObject.activeSelf,"arrow leaves bow T"+tier);Check(Vector3.Distance(before,bow.transform.position)>.01f,"bow moves on release T"+tier);
   await Task.Delay(750);Check(arrow.gameObject.activeSelf,"new arrow nocked T"+tier);
   if(tier==2){ScreenCapture.CaptureScreenshot("Library/BowGripReview.png");await Task.Delay(100);}
  }
  p.State.Equip(new WeaponInstance(WeaponKind.Bow){shuriken=true});await Task.Delay(200);ScreenCapture.CaptureScreenshot("Library/ShinobiHUD.png");await Task.Delay(600);RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&Object.FindFirstObjectByType<MainMenuUI>(),"menu return");Log("ADVANCED_COMBAT_PASSED");
 }catch(Exception e){Log("FAILED: "+e);Debug.LogException(e);}finally{EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).maximized=false;Time.timeScale=1;EditorApplication.update-=Pump;EditorApplication.isPlaying=false;}}
}
// Tier catalog and animation regression checks.
// Ammo pickup regression.
// Dual idle and enemy progression checks.
