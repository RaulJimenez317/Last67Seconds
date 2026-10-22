using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Enemies;
using Codice.Presentation;
using Codice.Mechanics;
using Codice.UI;
using Object=UnityEngine.Object;

public static class SpellMonsterChecks
{
    const string Report="Library/SpellMonsterChecks.txt";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Report,"PASS "+message+"\n");}
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("SpellChecks",false)){SessionState.SetBool("SpellChecks",false);Run();}};
        EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/SpellMonsterChecks.request"))return;File.Delete("Library/SpellMonsterChecks.request");File.WriteAllText(Report,"");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.AppendAllText(Report,"Save scene first");return;}UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("SpellChecks",true);EditorApplication.isPlaying=true;};
    }
    static void Pump(){EditorApplication.QueuePlayerLoopUpdate();foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))if(!e.name.StartsWith("Spell fixture"))e.SetPaused(true);}
    static async Task Until(Func<bool> condition){double end=EditorApplication.timeSinceStartup+50;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timed out");await Task.Delay(50);}}
    static void Capture(Camera cam,string name)
    {
        var old=cam.targetTexture;float aspect=cam.aspect;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=1280f/720;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Library/"+name+".png",image.EncodeToPNG());}
        finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;Object.Destroy(image);Object.Destroy(rt);}
    }
    static async void Run()
    {
        EditorApplication.update+=Pump;
        try
        {
            Application.runInBackground=true;
            await Until(()=>Object.FindFirstObjectByType<MainMenuUI>());
            RunNavigation.StartGame(PlayerClass.Mage,6);
            await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player&&RunDirector.Instance.CombatActive);
            var player=RunDirector.Instance.Player;player.enabled=false;var spells=player.GetComponent<StaffSpells>();var combat=player.GetComponent<PlayerCombat>();
            foreach(var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
            Check(spells&&player.State.Class==PlayerClass.Mage,"mage spells component and class");
            player.State.SelectSpell(3);float mana=player.State.Mana;
            Check(!combat.TryAttack()&&player.State.Mana==mana,"full-health cure costs nothing");
            player.State.Damage(60);float health=player.State.Health;
            Check(combat.TryAttack()&&player.State.Health>health&&Mathf.Abs(player.State.Mana-(mana-35))<.01f,"healing restores HP and costs 35 mana");
            Check(!combat.TryAttack(),"healing cooldown blocks repeat cast");
            player.State.SelectSpell(1);var snapshot=player.State.Capture();player.State.SelectSpell(2);player.State.Restore(snapshot);
            Check(player.State.SelectedSpell==StaffSpell.Ice,"spell selection survives state restore");
            await Task.Delay(1500);
            foreach(string name in new[]{"Slime","TurtleShell","DungeonSkeleton"})
            {
                var prefab=Resources.Load<BasicEnemy>("EnemyVariants/"+name);Check(prefab,"prefab "+name);
                var enemy=Object.Instantiate(prefab,player.transform.position+player.transform.forward*4,Quaternion.identity);enemy.name="Spell fixture "+name;
                enemy.Initialize(player,6);enemy.enabled=false;enemy.SetPaused(false);enemy.GetComponent<NavMeshAgent>().enabled=false;
                await Task.Delay(150);
                Check(enemy.animator.runtimeAnimatorController==prefab.animator.runtimeAnimatorController,"native controller retained "+name);
                var clip=enemy.animator.runtimeAnimatorController.animationClips.First(c=>c.name==name+"Melee");
                var bindings=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)).ToArray();
                int bound=bindings.Count(b=>b.path==""||enemy.animator.transform.Find(b.path));
                Check(bindings.Length>0&&bound>bindings.Length*.8f,"attack bones bound "+name+" "+bound+"/"+bindings.Length);
                foreach(var renderer in enemy.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)Check(material&&material.shader.isSupported,"supported material "+name);
                Physics.SyncTransforms();
                File.AppendAllText(Report,"POSITION "+name+" root="+enemy.transform.position+" collider="+enemy.GetComponent<Collider>().bounds+" camera="+player.view.transform.position+"\n");
                foreach(var renderer in enemy.GetComponentsInChildren<Renderer>())File.AppendAllText(Report,"GEOMETRY "+renderer.name+" "+renderer.bounds+"\n");
                player.view.transform.LookAt(enemy.GetComponent<Collider>().bounds.center);
                enemy.animator.Play("Melee",0,.3f);await Task.Delay(150);Capture(player.view,"Review"+name);
                if(name=="Slime")
                {
                    player.State.TickMana(100);player.State.SelectSpell(1);float hp=enemy.Health;mana=player.State.Mana;
                    Check(combat.TryAttack(),"ice casts");Check(enemy.Health<hp&&enemy.IsStunned,"ice damages and freezes enemy");Check(Mathf.Abs(player.State.Mana-(mana-25))<.01f,"ice costs 25 mana");
                    await Task.Delay(1600);player.State.TickMana(100);player.State.SelectSpell(2);hp=enemy.Health;
                    Check(combat.TryAttack(),"lightning casts");Check(!enemy||enemy.Health<hp,"lightning deals damage");
                }
                if(name=="DungeonSkeleton")
                {
                    await Task.Delay(1100);player.State.TickMana(100);player.State.SelectSpell(2);
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Spell fixture wall";wall.transform.position=Vector3.Lerp(player.view.transform.position,enemy.GetComponent<Collider>().bounds.center,.5f);wall.transform.localScale=new Vector3(3,4,.2f);Physics.SyncTransforms();
                    float hp=enemy.Health;Check(combat.TryAttack(),"lightning fires toward wall");Check(enemy.Health==hp,"wall blocks lightning");
                    await Task.Delay(1100);player.State.TickMana(100);player.State.SelectSpell(1);Check(combat.TryAttack(),"ice fires toward wall");Check(enemy.Health==hp,"wall blocks ice splash");Object.Destroy(wall);
                }
                if(enemy)Object.Destroy(enemy.gameObject);
            }
            await Task.Delay(1600);player.State.TickMana(100);player.State.SelectSpell(0);
            Check(combat.TryAttack(),"arcane projectile retained");Check(Object.FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None).Length>0,"arcane creates projectile");
            await Task.Delay(1200);player.State.SpendMana(player.State.Mana);player.State.SelectSpell(2);
            Check(!combat.TryAttack(),"insufficient mana blocks cast");
            for(int round=1;round<=25;round++)Check((BasicEnemyEncounter.ExtraEnemy(round,9,10)=="DungeonSkeleton")== (round%5==4),"miniboss schedule "+round);
            var existing=Object.FindFirstObjectByType<BasicEnemyEncounter>();
            var go=new GameObject("Spell fixture encounter");var encounter=go.AddComponent<BasicEnemyEncounter>();encounter.startOnPlay=false;encounter.player=player;encounter.prefabs=existing.prefabs;encounter.spawnPoints=existing.spawnPoints;encounter.maxConcurrent=4;
            bool completed=false;encounter.Completed+=()=>completed=true;
            Check(encounter.BeginEncounter(4,new[]{EnemyKind.Swordsman,EnemyKind.Heavy}),"miniboss encounter starts");
            var enemies=go.GetComponentsInChildren<BasicEnemy>();Check(enemies.Length==2&&enemies.Count(e=>e.miniboss)==1,"miniboss replaces a counted slot");
            foreach(var enemy in enemies){enemy.SetPaused(false);enemy.ReceiveDamage(new DamageInfo{amount=100000,ignoreGuard=true});}
            Check(completed&&encounter.Defeated==encounter.Total,"miniboss death completes encounter");Object.Destroy(go);
            player.State.TickMana(100);player.State.SelectSpell(1);RunHUD.Instance.Refresh();
            Check(RunHUD.Instance.transform.Find("Spell book").gameObject.activeSelf,"spell selection UI visible with staff");
            // Temporarily render the overlay through the game camera to review HUD layout.
            var canvas=RunHUD.Instance.Canvas;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=player.view;canvas.planeDistance=.5f;Canvas.ForceUpdateCanvases();Capture(player.view,"ReviewSpellHUD");canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            File.AppendAllText(Report,"SPELL_MONSTER_CHECKS_PASSED\n");
        }
        catch(Exception e){File.AppendAllText(Report,"FAILED "+e);Debug.LogException(e);}
        finally{EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
}
