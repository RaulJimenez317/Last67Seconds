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
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class DungeonExpansionChecks
{
    const string Report="Library/ExpansionChecks.txt";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Expansion.Checks",false)){SessionState.SetBool("Expansion.Checks",false);Run();}};
        EditorApplication.delayCall+=()=>{
            if(!File.Exists("Library/ExpansionChecks.request")||EditorApplication.isPlaying)return;
            File.Delete("Library/ExpansionChecks.request");
            if(SceneManager.GetActiveScene().isDirty){Debug.LogError("Save scene before checks");return;}
            RenderEffects();
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            File.WriteAllText(Report,"25 ROUND EXPANSION CHECKS\n");SessionState.SetBool("Expansion.Checks",true);EditorApplication.isPlaying=true;
        };
    }
    static void Log(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
    static void Check(bool value,string message){if(!value)throw new Exception(message);Log("PASS: "+message);}
    static async Task Until(Func<bool> condition,string label,int seconds=60){double end=EditorApplication.timeSinceStartup+seconds;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout "+label);await Task.Delay(50);}}
    static void Pump(){EditorApplication.QueuePlayerLoopUpdate();}
    static void RenderEffects()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var library=PresentationLibrary.Load();
        var camera=new GameObject("VFX review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2,-8);camera.transform.LookAt(new Vector3(0,1,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.21f);
        int index=0;foreach(var prefab in new[]{library.blood,library.gunSmoke,library.explosion}){
            var go=Object.Instantiate(prefab,new Vector3(-2.5f+index++*2.5f,1,0),Quaternion.identity);if(prefab==library.blood)go.transform.localScale*=.55f;if(prefab==library.explosion)go.transform.localScale*=.65f;
            foreach(var ps in go.GetComponentsInChildren<ParticleSystem>())ps.Simulate(.16f,false,true);
        }
        var rt=new RenderTexture(1200,600,24);var tex=new Texture2D(1200,600,TextureFormat.RGB24,false);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,600),0,0);tex.Apply();File.WriteAllBytes("Library/CombatEffectsReview.png",tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
    }
    static async void Run()
    {
        EditorApplication.update+=Pump;
        try{
            Application.runInBackground=true;
            await Until(()=>Object.FindFirstObjectByType<MainMenuUI>(),"menu");
            Check(RoundRules.FinalRound==25,"final round 25");
            foreach(int boundary in new[]{5,10,15,20,24})Check(RoundRules.OpensPortalAfter(boundary),"portal after "+boundary);
            foreach(int noPortal in new[]{1,11,16,19,21,25})Check(!RoundRules.OpensPortalAfter(noPortal),"no premature portal "+noPortal);
            var library=PresentationLibrary.Load();Check(library&&library.blood&&library.gunSmoke&&library.magicProjectile&&library.explosion,"VFX asset references");
            foreach(var effect in new[]{library.blood,library.gunSmoke,library.magicProjectile})foreach(var r in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))if(r.renderMode!=ParticleSystemRenderMode.None)foreach(var m in r.sharedMaterials)Check(m&&m.shader&&m.shader.isSupported&&m.GetTag("RenderPipeline",false)=="UniversalPipeline",effect.name+" URP material");
            RunNavigation.StartGame(PlayerClass.Warrior,1);
            for(int round=1;round<=RoundRules.FinalRound;round++){
                int expected=round;
                await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Level==expected&&RunDirector.Instance.CombatActive&&RunHUD.Instance&&RunHUD.Instance.Context.gameObject.scene.name==RoundRules.SceneForRound(expected),"start round "+round);
                var director=RunDirector.Instance;var context=RunHUD.Instance.Context;var player=director.Player;
                player.enabled=false;player.GetComponent<PlayerCombat>().enabled=false;player.State.Heal(player.State.MaxHealth);
                Check(SceneManager.GetActiveScene().name==RoundRules.SceneForRound(round),"round "+round+" scene "+SceneManager.GetActiveScene().name);
                if(round==1){
                    int before=PresentationVfx.ActiveCount;player.ReceiveDamage(new DamageInfo{amount=2,source=null,ignoreGuard=true});Check(PresentationVfx.ActiveCount==before+1,"player damage creates blood");
                    before=PresentationVfx.ActiveCount;CombatFeedback.Report(player,new DamageInfo(),0,true);Check(PresentationVfx.ActiveCount==before,"zero damage does not create blood");
                    PresentationVfx.GunSmoke(player.transform.position+Vector3.up,Vector3.forward);Check(PresentationVfx.ActiveCount==before+1,"musket smoke emitted");
                    var shot=new GameObject("Magic visual test").AddComponent<PlayerProjectile>();shot.Initialize(player.gameObject,Vector3.forward,1,~0);shot.AddMagicVisual();Check(shot.GetComponentInChildren<ParticleSystem>(),"staff projectile is visible");Object.Destroy(shot.gameObject);
                }
                if(RoundRules.IsBoss(round)){
                    var boss=context.arena.ActiveBoss;Check(boss&&boss.IsAlive,"boss round "+round+" active");
                    Check((boss.kind==Codice.Bosses.BossKind.OrkBerserker)==(round==25),"final boss reserved for round 25");
                    boss.ReceiveDamage(new DamageInfo{amount=999999,source=player.gameObject});
                }else{
                    Check(context.dungeon&&context.dungeon.merchant&&context.lootMarkers.Length>0,"merchant, wave and loot round "+round);
                    bool visitedShop=false;double end=EditorApplication.timeSinceStartup+40;
                    while(director.Phase==RunPhase.Combat||director.Phase==RunPhase.Shop){
                        if(EditorApplication.timeSinceStartup>end)throw new Exception("Stuck wave "+round);
                        player.State.Heal(player.State.MaxHealth);
                        foreach(var enemy in context.dungeon.GetComponentsInChildren<BasicEnemy>())if(enemy.IsAlive)enemy.ReceiveDamage(new DamageInfo{amount=999999,source=player.gameObject,ignoreGuard=true});
                        if(director.Phase==RunPhase.Shop){visitedShop=true;context.dungeon.merchant.ContinueRound();}
                        await Task.Delay(50);
                    }
                    Check(visitedShop,"merchant intermission round "+round);
                }
                Check(director.Phase==(round==25?RunPhase.Victory:RunPhase.Cleared),"completed round "+round);
                if(RoundRules.OpensPortalAfter(round)){Check(context.portal&&context.portal.Available,"portal enabled "+round);Check(director.Travel(),"travel "+round);}
            }
            Check(RunDirector.Instance.RestartRun(),"restart after victory");
            await Until(()=>RunDirector.Instance.Level==1&&RunDirector.Instance.CombatActive,"restart first round");
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&SceneManager.GetActiveScene().name=="MainMenu","menu return");
            await Task.Delay(3000);Check(PresentationVfx.ActiveCount==0,"VFX cleaned up on scene change");
            Log("ALL_25_ROUNDS_AND_VFX_PASSED (automated kills; not a difficulty/balance playtest)");
        }catch(Exception e){Log("FAILED: "+e);Debug.LogException(e);}
        finally{EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
}
