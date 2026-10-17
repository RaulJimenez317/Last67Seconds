using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.UI;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class UIChecks
{
    public static string Result {get;private set;}="Not run";
    static int count;
    static UIChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("UIChecksArmed",false)){SessionState.SetBool("UIChecksArmed",false);Begin();}};}
    public static void Arm(){SessionState.SetBool("UIChecksArmed",true);}
    static void Check(bool value,string message){if(!value)throw new Exception("UI FAIL: "+message);count++;}
    static async Task Until(Func<bool> condition,int seconds=15){float end=Time.realtimeSinceStartup+seconds;while(!condition()){if(Time.realtimeSinceStartup>end)throw new Exception("UI timeout");await Task.Delay(50);}}
    [MenuItem("Codice/UI/Run UI checks (Play, fresh Calabozo)")]
    public static async void Begin()
    {
        if(!Application.isPlaying||Result=="Running")return;
        Result="Running";count=0;Application.runInBackground=true;
        try
        {
            await Until(()=>RunHUD.Instance&&RunDirector.Instance&&RunDirector.Instance.Player);
            var hud=RunHUD.Instance;var director=RunDirector.Instance;var player=director.Player;var state=player.State;var mechanics=player.GetComponent<PlayerMechanics>();
            foreach(var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            Check(director.Level==1,"fresh dungeon required");
            Check(hud.Canvas.renderMode==RenderMode.ScreenSpaceOverlay,"overlay canvas");
            Check(RunHUD.Install(hud.Context)==hud&&Object.FindObjectsByType<RunHUD>(FindObjectsSortMode.None).Length==1,"idempotent install");
            foreach(string shape in new[]{"solid","ring","frame","hex","diamond","vignette"})Check(ProceduralUISprites.Shape(shape).texture!=null,"procedural "+shape);
            foreach(WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))Check(ProceduralUISprites.Weapon(kind)!=null,"weapon silhouette "+kind);
            string snapshot=state.Capture();state.Damage(20);hud.Refresh();
            Check(hud.PlayerHealth.Fill.fillAmount<hud.PlayerHealth.Ghost.fillAmount,"damage ghost remains behind instant health");
            await Task.Delay(900);Check(Mathf.Abs(hud.PlayerHealth.Fill.fillAmount-hud.PlayerHealth.Ghost.fillAmount)<.03f,"delayed ghost converges");
            state.Heal(20);hud.Refresh();Check(hud.PlayerHealth.Fill.fillAmount==1&&hud.PlayerHealth.Ghost.fillAmount==1,"healing updates both bars");
            mechanics.AddCharge(100);hud.Refresh();Check(hud.UltimateMeter.fillAmount==1,"ultimate linked to charge");Check(hud.UltimateMeter.fillMethod==Image.FillMethod.Radial360,"radial 360");
            state.Equip(new WeaponInstance(WeaponKind.Bow,4));state.ConsumeAmmo();hud.Refresh();Check(state.ActiveWeapon.ammunition==11,"real hotbar inventory");
            state.EquipPassive(PassiveKind.MercuryBoots);state.ActivatePassive(0);Check(player.TryDash(Vector3.forward),"primary dash");await Task.Delay(400);Check(player.TryDash(Vector3.forward)&&player.SecondDashCooldown>0,"second dash shown from actual cooldown");
            float elapsed=director.Elapsed;hud.SetPaused(true);await Task.Delay(200);Check(Time.timeScale==0&&Mathf.Abs(director.Elapsed-elapsed)<.02f,"pause freezes simulation and clocks");hud.SetPaused(false);Check(Time.timeScale==1,"resume restores simulation");
            var pickup=new GameObject("UI test pickup").AddComponent<WeaponPickup>();pickup.weapon=new WeaponInstance(WeaponKind.Sword,3);Check(hud.DescribePickup(pickup).Contains("EQUIPADA")&&hud.DescribePickup(pickup).Contains("SUELO"),"weapon comparison");Object.Destroy(pickup.gameObject);
            state.EquipPassive(PassiveKind.Frenzy);state.EquipPassive(PassiveKind.Overload);
            var seal=LootPickup.Create(player.transform.position,LootKind.Passive,1);Check(hud.DescribePickup(seal).Contains("Shift + E")&&hud.DescribePickup(seal).Contains("Ctrl + E")&&hud.DescribePickup(seal).Contains("Alt + E"),"three replacement commands");Object.Destroy(seal.gameObject);
            state.Restore(snapshot);await Task.Delay(300);Check(hud.WorldBarCount>0,"world health bars discovered");
            var enemyTarget=Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).First();enemyTarget.SetPaused(false);enemyTarget.enabled=false;
            CombatFeedback.Result feedback=default;int hits=0;void Observe(CombatFeedback.Result r){feedback=r;hits++;}CombatFeedback.Resolved+=Observe;
            try{enemyTarget.ReceiveDamage(new DamageInfo{amount=10,source=player.gameObject,critical=true,direction=Vector3.forward});Check(hits==1&&feedback.hit.critical&&feedback.amount==10,"critical result emitted once");}finally{CombatFeedback.Resolved-=Observe;}
            enemyTarget.SetPaused(true);
            // Complete the actual round and purchase through the same public transaction as the buttons.
            while(director.Phase!=RunPhase.Shop){var enemies=Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None);foreach(var e in enemies){e.SetPaused(false);e.ReceiveDamage(new DamageInfo{amount=10000,source=player.gameObject,ignoreGuard=true});}await Task.Delay(80);}
            var merchant=hud.Context.dungeon.merchant;var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=merchant.transform.position+Vector3.forward*2;controller.enabled=true;
            state.AddGold(1000);merchant.Interact(player);await Task.Delay(100);Check(hud.ShopVisible,"merchant UI opens with real customer");
            Check(hud.Purchase(0)&&merchant.GetOffer(0).sold,"UI purchase invokes merchant transaction");Check(!hud.Purchase(0),"sold offer cannot buy twice");merchant.ContinueRound();await Task.Delay(100);Check(!hud.ShopVisible,"shop closes on continue");
            // Real portals, scene-owned UI lifetime, evolved boss and restart.
            while(director.Level<5)
            {
                foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None)){e.SetPaused(false);e.ReceiveDamage(new DamageInfo{amount=10000,source=director.Player.gameObject,ignoreGuard=true});}
                var scene=Object.FindFirstObjectByType<RunScene>();if(scene.dungeon&&scene.dungeon.merchant.Available)scene.dungeon.merchant.ContinueRound();
                if(director.Level==4&&director.Phase==RunPhase.Cleared){Check(scene.portal.Available,"portal enabled after round four");Check(director.Travel(),"portal travel accepted");break;}await Task.Delay(100);
            }
            await Until(()=>RunHUD.Instance&&RunHUD.Instance.Context.IsArena&&RunHUD.Instance.Context.arena.ActiveBoss);
            hud=RunHUD.Instance;var boss=hud.Context.arena.ActiveBoss;boss.SetPaused(true);hud.Refresh();Check(Object.FindObjectsByType<RunHUD>(FindObjectsSortMode.None).Length==1,"single HUD after dungeon to arena");Check(hud.BossHealth.gameObject.activeInHierarchy,"boss bar visible");Check(hud.BossHealth.Fill.fillAmount==1,"boss maximum health");
            boss.Initialize(director.Player,15);hud.Refresh();Check(boss.Evolved&&boss.MaxHealth==1800,"evolved boss source");boss.SetPaused(false);boss.ReceiveDamage(new DamageInfo{amount=100,source=director.Player.gameObject});boss.SetPaused(true);hud.Refresh();Check(hud.BossHealth.Ghost.fillAmount>hud.BossHealth.Fill.fillAmount,"boss ghost bar");
            var pillar=Object.FindFirstObjectByType<DestructiblePillar>();pillar.ReceiveDamage(new DamageInfo{amount=20,source=director.Player.gameObject});await Task.Delay(350);Check(pillar.GetComponentInChildren<WorldHealthBar>()!=null,"pillar world integrity");
            boss.SetPaused(false);boss.ReceiveDamage(new DamageInfo{amount=10000,source=director.Player.gameObject});Check(director.Travel(),"return portal travel");
            await Until(()=>RunHUD.Instance&&!RunHUD.Instance.Context.IsArena&&director.Level==6);
            Check(Object.FindObjectsByType<RunHUD>(FindObjectsSortMode.None).Length==1,"single HUD after return");director.Player.State.Damage(10000);await Task.Delay(100);Check(director.Phase==RunPhase.Defeated,"defeat reaches UI");Check(director.RestartRun(),"restart from defeat");
            await Until(()=>RunHUD.Instance&&director.Player&&director.Level==1&&director.Player.State.IsAlive);
            Check(Object.FindObjectsByType<RunHUD>(FindObjectsSortMode.None).Length==1,"single HUD after restart");
            foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))e.SetPaused(true);
            Result="UI_CHECKS_PASSED: "+count;Debug.Log(Result);
        }
        catch(Exception e){if(RunHUD.Instance)RunHUD.Instance.SetPaused(false);Result=e.ToString();Debug.LogError(Result);}
    }
}
