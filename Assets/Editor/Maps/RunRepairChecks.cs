using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codice.Bosses;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class RunRepairChecks
{
    const string Report="Library/RunRepairReport.txt";
    const string Request="Library/RunRepair.request";
    [InitializeOnLoadMethod] static void Initialize()
    {
        EditorApplication.delayCall+=()=>
        {
            if(File.Exists("Library/BossChecks.request")&&!EditorApplication.isPlaying){File.Delete("Library/BossChecks.request");SessionState.SetBool("RunRepair.Boss",true);EditorApplication.isPlaying=true;}
            else if(EditorApplication.isPlaying&&SessionState.GetBool("RunRepair.Boss",false)){SessionState.SetBool("RunRepair.Boss",false);BossChecks();}
            else if(File.Exists(Request)&&!EditorApplication.isPlaying){File.Delete(Request);RepairAndTest();}
            else if(EditorApplication.isPlaying&&SessionState.GetBool("RunRepair.Play",false)){SessionState.SetBool("RunRepair.Play",false);PlayChecks();}
        };
    }
    static void Log(string message){File.AppendAllText(Report,message+Environment.NewLine);Debug.Log(message);}
    static void Check(bool value,string message){if(!value)throw new Exception(message);Log("PASS: "+message);}
    [MenuItem("Codice/Maps/Repair and verify complete run")]
    public static void RepairAndTest()
    {
        try
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
            if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scene edits before running checks");
            File.WriteAllText(Report, "RUN REPAIR\n");
            string[] names={"MonsterOrc","MonsterMutant","OrkBerserker"};
            foreach(var name in names)RepairBoss(name);
            foreach(var name in new[]{"Calabozo","Coliseo","FinalArena"})RepairScene(name);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            SessionState.SetBool("RunRepair.Play",true);EditorApplication.isPlaying=true;
        }
        catch(Exception e){Log("FAILED: "+e);}
    }
    static void RepairBoss(string name)
    {
        string path="Assets/01_Prefabs/Bosses/"+name+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var boss=root.GetComponent<BossCharacter>();var animator=boss.visual.GetComponentInChildren<Animator>(true);
            Check(animator&&animator.runtimeAnimatorController,name+" imported animator");
            var clips=animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            // On subsequent runs the normalized controller already contains these states.
            string controllerPath="Assets/01_Prefabs/Bosses/"+name+"Combat.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
                string[] states={"Idle","Chase","Patrol","Melee","Shoot","Hit","Death"};
                string[][] terms={new[]{"idle"},new[]{"run","walk"},new[]{"walk","run"},new[]{"attack","atack"},new[]{"attack","atack"},new[]{"gethit","hit","wound"},new[]{"death","die"}};
                for(int i=0;i<states.Length;i++)
                {
                    var source=terms[i].Select(term=>clips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains(term))).FirstOrDefault(c=>c);
                    Check(source,name+" clip for "+states[i]+" among "+string.Join(",",clips.Select(c=>c.name)));
                    var clip=Object.Instantiate(source);clip.name=states[i];
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=i<3;AnimationUtility.SetAnimationClipSettings(clip,settings);
                    AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
                    AssetDatabase.CreateAsset(clip,"Assets/01_Prefabs/Bosses/"+name+states[i]+".anim");
                    var state=controller.layers[0].stateMachine.AddState(states[i]);state.motion=clip;
                    if(i>=3){state.speedParameter="ActionSpeed";state.speedParameterActive=true;}
                }
            }
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            boss.rockMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/01_Prefabs/Bosses/Rock.mat");
            boss.fireMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/01_Prefabs/Bosses/Fire.mat");
            boss.mudMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/01_Prefabs/Bosses/Mud.mat");
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void RepairScene(string name)
    {
        var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/"+name+".unity");var run=Object.FindFirstObjectByType<RunScene>();
        Check(run&&run.player&&run.arena,name+" scene references");
        if(name!="FinalArena")
        {
            run.arena.bossPrefabs=new[]{"GoblinKing","DemonLord","MonsterOrc","MonsterMutant"}.Select(n=>AssetDatabase.LoadAssetAtPath<BossCharacter>("Assets/01_Prefabs/Bosses/"+n+".prefab")).ToArray();
            Check(run.dungeon&&run.dungeon.merchant,name+" merchant and wave");
        }
        var surfaces=Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach(var s in surfaces)s.RemoveData();
        var surface=surfaces.FirstOrDefault();if(!surface)surface=new GameObject("Run navigation").AddComponent<NavMeshSurface>();
        foreach(var s in surfaces.Where(s=>s!=surface))s.enabled=false;
        var player=run.player.gameObject;player.SetActive(false);
        if(name=="FinalArena")
        {
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(!filter.GetComponent<Collider>()&&!filter.GetComponentInParent<RunPortal>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        }
        surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
        string path="Assets/01_Prefabs/Bosses/"+name+"RunNavigation.asset";
        var saved=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if(saved){EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();EditorUtility.SetDirty(saved);}
        else AssetDatabase.CreateAsset(surface.navMeshData,path);
        player.SetActive(true);
        Check(NavMesh.SamplePosition(run.player.transform.position,out var start,30,NavMesh.AllAreas),name+" player navigation");
        run.player.transform.position=start.position+Vector3.up*.1f;
        Check(NavMesh.SamplePosition(run.arena.spawnPoint.position,out var bossPos,30,NavMesh.AllAreas),name+" boss navigation");
        run.arena.spawnPoint.position=bossPos.position;
        if(run.dungeon)foreach(var spawn in run.dungeon.spawnPoints)
        {
            Check(NavMesh.SamplePosition(spawn.position,out var hit,10,NavMesh.AllAreas),name+" spawn "+spawn.name);
            spawn.position=hit.position;
            var route=new NavMeshPath();Check(NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete,name+" reachable spawn "+spawn.name);
        }
        Check(NavMesh.CalculateTriangulation().vertices.Length>0,name+" navigation persisted");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static async Task Until(Func<bool> predicate,string message,int seconds=25)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!predicate()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout: "+message);await Task.Delay(50);}
    }
    static async void PlayChecks()
    {
        try
        {
            Application.runInBackground=true;
            await Until(()=>Object.FindFirstObjectByType<MainMenuUI>(),"main menu");
            await Task.Delay(500);
            Check(GameObject.Find("Main Menu/Test round")!=null||Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.name=="Test round"&&t.gameObject.activeInHierarchy),"round selector visible on home");
            RunNavigation.StartGame(PlayerClass.Warrior,1);
            for(int round=1;round<=20;round++)
            {
                int expected=round;
                await Until(()=>RunDirector.Instance&&RunDirector.Instance.Level==expected&&RunDirector.Instance.CombatActive&&RunHUD.Instance&&RunHUD.Instance.Context.gameObject.scene.name==RoundRules.SceneForRound(expected),"round "+round);
                var run=RunDirector.Instance;var context=RunHUD.Instance.Context;
                run.Player.enabled=false;run.Player.GetComponent<PlayerCombat>().enabled=false;
                Check(SceneManager.GetActiveScene().name==RoundRules.SceneForRound(round),"round "+round+" correct scene");
                if(RoundRules.IsBoss(round))
                {
                    var boss=context.arena.ActiveBoss;Check(boss&&boss.IsAlive,"boss spawned round "+round);
                    await Task.Delay(150);
                    Check(Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.name=="JEFE"&&t.gameObject.activeInHierarchy),"boss announcement round "+round);
                    boss.ReceiveDamage(new DamageInfo{amount=999999,source=run.Player.gameObject});
                }
                else
                {
                    double deadline=EditorApplication.timeSinceStartup+20;
                    while(run.Phase==RunPhase.Combat||run.Phase==RunPhase.Shop)
                    {
                        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Wave stuck: "+round+" phase "+run.Phase+" kills "+context.dungeon.Defeated+"/"+context.dungeon.Total);
                        foreach(var enemy in context.dungeon.GetComponentsInChildren<BasicEnemy>())if(enemy.IsAlive)enemy.ReceiveDamage(new DamageInfo{amount=999999,source=run.Player.gameObject,ignoreGuard=true});
                        if(run.Phase==RunPhase.Shop)context.dungeon.merchant.ContinueRound();
                        await Task.Delay(50);
                    }
                }
                Check(run.Phase==(round==20?RunPhase.Victory:RunPhase.Cleared),"round "+round+" completed");
                if(RoundRules.OpensPortalAfter(round)){Check(context.portal&&context.portal.Available,"portal round "+round);Check(run.Travel(),"travel round "+round);}
            }
            Check(RunDirector.Instance.RestartRun(),"restart after victory");
            await Until(()=>RunDirector.Instance.Level==1&&RunDirector.Instance.CombatActive,"restart first round");
            Log("ALL_ROUNDS_PASSED");
        }
        catch(Exception e){Log("FAILED: "+e);}
        finally{Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
    static async void BossChecks()
    {
        try
        {
            Application.runInBackground=true;
            await Task.Delay(500);
            foreach(int round in new[]{5,6,10,11,15,16,19,20})
            {
                RunNavigation.StartGame(PlayerClass.Warrior,round);
                await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Level==round&&RunDirector.Instance.CombatActive,"selected round "+round);
                Check(SceneManager.GetActiveScene().name==RoundRules.SceneForRound(round),"menu selection round "+round);
            }
            RunNavigation.StartGame(PlayerClass.Warrior,10);
            await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Level==10&&RunDirector.Instance.CombatActive,"boss fixtures");
            var context=RunHUD.Instance.Context;var player=RunDirector.Instance.Player;
            player.enabled=false;player.GetComponent<PlayerCombat>().enabled=false;
            Object.Destroy(context.arena.ActiveBoss.gameObject);await Task.Delay(100);
            foreach(string name in new[]{"GoblinKing","DemonLord","MonsterOrc","MonsterMutant","OrkBerserker"})
            {
                player.State.Heal(player.State.MaxHealth);
                player.GetComponent<CharacterController>().enabled=false;
                player.transform.position=new Vector3(0,.1f,-3);
                player.GetComponent<CharacterController>().enabled=true;
                var prefab=AssetDatabase.LoadAssetAtPath<BossCharacter>("Assets/01_Prefabs/Bosses/"+name+".prefab");
                Check(NavMesh.SamplePosition(Vector3.zero,out var spawn,5,NavMesh.AllAreas),name+" spawn on navmesh");
                var boss=Object.Instantiate(prefab,spawn.position,Quaternion.identity);boss.Initialize(player,name=="OrkBerserker"?20:5);
                float hp=player.State.Health;
                await Until(()=>player.State.Health<hp,name+" attack applies damage",12);
                Check(player.State.IsAlive,name+" attack and player survive");
                var presentation=boss.GetComponent<Codice.Presentation.ActorPresentation>();
                Check(presentation&&presentation.Body&&presentation.Body.HasState(0,Animator.StringToHash("Melee"))&&presentation.Body.HasState(0,Animator.StringToHash("Death")),name+" animation states");
                boss.ReceiveDamage(new DamageInfo{amount=10,source=player.gameObject});
                Check(boss.Health<boss.MaxHealth,name+" receives damage");
                boss.ReceiveDamage(new DamageInfo{amount=999999,source=player.gameObject});
                Check(!boss.IsAlive,name+" dies");
                await Task.Delay(150);
            }
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&SceneManager.GetActiveScene().name=="MainMenu","return to menu");
            Log("ALL_BOSSES_AND_SELECTOR_PASSED");
        }
        catch(Exception e){Log("FAILED: "+e);}
        finally{Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
}
