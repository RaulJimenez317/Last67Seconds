using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;
public static class MageChecks
{
    const string Report="Library/MageChecks.txt";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MageChecks",false)){SessionState.SetBool("MageChecks",false);Play();}};
        EditorApplication.delayCall+=()=>{
            if(!File.Exists("Library/MageChecks.request")||EditorApplication.isPlaying)return;File.Delete("Library/MageChecks.request");
            try{
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before checks");
                File.WriteAllText(Report,"MAGE, MANA, CHEATS AND VFX\n");
                var lib=AssetDatabase.LoadAssetAtPath<PresentationLibrary>("Assets/Resources/Presentation/Library.asset");
                lib.swordSparks=PresentationSetup.Effect("Sparks/Sparks explode yellow","SwordSparks");lib.magicCast=PresentationSetup.Effect("Hits and explosions/Electro hit","MagicCast");EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
                if(!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MageVisual.prefab")){
                    var holder=new GameObject("Mage authoring");
                    try{var model=CharacterSetup.Model("Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/Characters/fbx/Mage.fbx",holder.transform,1.8f,"mage");foreach(var r in model.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/MageVisual.prefab");}finally{Object.DestroyImmediate(holder);}
                }
                Data();EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("MageChecks",true);EditorApplication.isPlaying=true;
            }catch(Exception e){Log("FAILED: "+e);}
        };
    }
    static void Log(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
    static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
    static void Data()
    {
        foreach(PlayerClass kind in Enum.GetValues(typeof(PlayerClass))){
            var state=new CharacterState();state.Initialize(kind);Check(state.Mana==(kind==PlayerClass.Mage?100:60),kind+" mana capacity");
            if(kind==PlayerClass.Mage)Check(state.ActiveWeapon.kind==WeaponKind.Staff,"mage starts with staff");else state.Equip(new WeaponInstance(WeaponKind.Staff));
            int shots=kind==PlayerClass.Mage?5:3;for(int i=0;i<shots;i++)Check(state.ConsumeAmmo(),kind+" cast "+i);
            Check(!state.ConsumeAmmo()&&state.Mana==0,kind+" empty prevents firing");state.TickMana(1);Check(state.Mana==0,kind+" recharge delay");state.TickMana(1.5f);Check(state.Mana==8,kind+" regeneration 8 per second after delay");
            state.Equip(new WeaponInstance(WeaponKind.Staff));Check(state.Mana==8,kind+" pickup does not refill");state.SelectWeapon(0);state.SelectWeapon(1);Check(state.Mana==8,kind+" slot switch does not refill");
            var restored=new CharacterState();restored.Restore(state.Capture());Check(restored.Mana==8&&restored.MaxMana==state.MaxMana,kind+" snapshot preserves mana");
            state.TickMana(0);Check(state.Mana==8,kind+" paused time unchanged");state.TickMana(100);Check(state.Mana==state.MaxMana,kind+" regen capped");state.Damage(10000);float mana=state.Mana;state.TickMana(10);Check(state.Mana==mana&&!state.SpendMana(20),kind+" dead cannot cast");
        }
    }
    static async Task Until(Func<bool> test,string label){double end=EditorApplication.timeSinceStartup+60;while(!test()){if(EditorApplication.timeSinceStartup>end)throw new Exception(label);await Task.Delay(50);}}
    static void Pump()=>EditorApplication.QueuePlayerLoopUpdate();
    static Button FindButton(string name)=>Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.name==name);
    static async void Play()
    {
        EditorApplication.update+=Pump;
        try{
            Application.runInBackground=true;await Until(()=>Object.FindFirstObjectByType<MainMenuUI>()&&Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="TRUCOS"),"menu");
            Check(!MainMenuUI.CheatsUnlocked&&MainMenuUI.CheatRound==1,"selector initially locked");FindButton("TRUCOS").onClick.Invoke();
            var input=Object.FindFirstObjectByType<TMP_InputField>();Check(input&&input.targetGraphic.raycastTarget,"code input clickable");
            input.text="wrong";FindButton("ACTIVAR").onClick.Invoke();Check(!MainMenuUI.CheatsUnlocked,"wrong code rejected");
            input.text="Jam2026";FindButton("ACTIVAR").onClick.Invoke();Check(MainMenuUI.CheatsUnlocked,"correct code unlocks");MainMenuUI.SelectTestRound(99);Check(MainMenuUI.CheatRound==25,"round upper bound");MainMenuUI.SelectTestRound(-1);Check(MainMenuUI.CheatRound==1,"round lower bound");
            for(int i=0;i<10;i++)FindButton("SIGUIENTE").onClick.Invoke();Check(MainMenuUI.CheatRound==11,"round buttons choose 11");await Task.Delay(800);ScreenCapture.CaptureScreenshot("Library/CheatsMenu.png");await Task.Delay(700);
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="VOLVER").onClick.Invoke();FindButton("INICIAR JUEGO").onClick.Invoke();
            Check(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.name.StartsWith("JUGAR COMO"))==4,"four starting classes visible");ScreenCapture.CaptureScreenshot("Library/MageClasses.png");await Task.Delay(700);FindButton("JUGAR COMO MAGO").onClick.Invoke();
            await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.CombatActive&&RunHUD.Instance,"mage run");
            var player=RunDirector.Instance.Player;Check(RunDirector.Instance.Level==11&&player.State.Class==PlayerClass.Mage,"chosen class and round applied");Check(player.GetComponent<ActorPresentation>().Body&&player.classVisuals.Length==4&&player.classVisuals[3].activeSelf,"mage has own animated visual");
            foreach(var enemy in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            var combat=player.GetComponent<PlayerCombat>();Check(combat.TryAttack(),"actual staff attack");Check(player.State.Mana==80,"actual attack costs 20");Check(PresentationVfx.ActiveCount>0,"magic cast particles");
            player.State.SpendMana(80);await Task.Delay(1000);Check(!combat.TryAttack(),"actual attack blocked without mana");
            Time.timeScale=0;float before=player.State.Mana;await Task.Delay(700);Check(player.State.Mana==before,"pause does not regenerate mana");Time.timeScale=1;
            await Task.Delay(3000);Check(player.State.Mana>before,"live mana regeneration");Check(RunHUD.Instance.PlayerMana.gameObject.activeSelf,"mana bar shown for staff");ScreenCapture.CaptureScreenshot("Library/MageHUD.png");await Task.Delay(600);
            player.State.ActiveWeapon.ultimateCharge=100;player.State.SpendMana(player.State.Mana);Check(!player.GetComponent<PlayerMechanics>().TryUltimate()&&player.State.ActiveWeapon.ultimateCharge==100,"no mana prevents ultimate without consuming charge");
            player.State.Equip(new WeaponInstance(WeaponKind.Sword));await Task.Delay(1000);Check(combat.TryAttack(),"sword attack works");Check(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.transform.root.name.Contains("SwordSparks")),"sword sparks on swing");
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&Object.FindFirstObjectByType<MainMenuUI>(),"return menu");Check(MainMenuUI.CheatsUnlocked,"cheat remains unlocked in session");Log("MAGE_CHECKS_PASSED");
        }catch(Exception e){Log("FAILED: "+e);Debug.LogException(e);}
        finally{Time.timeScale=1;EditorApplication.update-=Pump;EditorApplication.isPlaying=false;}
    }
}
