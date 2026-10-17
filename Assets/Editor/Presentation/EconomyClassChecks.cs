using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class EconomyClassChecks
{
    public static string Result="Not run";
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static async Task Until(Func<bool> ready)
    {float end=Time.realtimeSinceStartup+30;while(!ready()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(50);}}
    static void Move(PlayerCharacter p,Vector3 position)
    {var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=position;c.enabled=true;Physics.SyncTransforms();}
    public static async void Run()
    {
        if(!Application.isPlaying || Result=="Running")return;
        Result="Running";checks=0;Application.runInBackground=true;
        try
        {
            RunNavigation.MainMenu();await Until(()=>!RunNavigation.Loading&&Object.FindFirstObjectByType<MainMenuUI>());
            await Task.Delay(150);
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name=="INICIAR JUEGO").onClick.Invoke();
            Check(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.name.StartsWith("JUGAR COMO"))==3,"Three class choices after Start");
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name=="JUGAR COMO ARQUERO").onClick.Invoke();
            await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player);
            var p=RunDirector.Instance.Player;var s=p.State;
            foreach(var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            Check(s.Class==PlayerClass.Archer&&s.ActiveWeapon.kind==WeaponKind.Bow&&s.MoveSpeed==8,"Archer selection applied before spawn");
            int ammo=s.ActiveWeapon.ammunition;Check(p.GetComponent<PlayerCombat>().TryAttack()&&s.ActiveWeapon.ammunition==ammo-1,"Archer spends ammunition");
            var enemyTarget=Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).First();int reward=enemyTarget.GoldReward;
            enemyTarget.SetPaused(false);enemyTarget.ReceiveDamage(new DamageInfo{amount=100000,source=p.gameObject,breaksGuard=true});
            var coin=Object.FindFirstObjectByType<GoldPickup>();Check(coin&&coin.Amount==reward,"Enemy drops physical gold");
            int balance=s.Gold;Check(coin.Collect(p)&&s.Gold==balance+reward&&!coin.Collect(p),"Gold credited exactly once");
            var triggerCoin=GoldPickup.Drop(p.transform.position+Vector3.up*.7f,7,p);Physics.SyncTransforms();
            await Until(()=>!triggerCoin);Check(s.Gold==balance+reward+7,"Gold collected through physics proximity");
            var chests=Object.FindObjectsByType<TreasureChest>(FindObjectsSortMode.None);
            Check(chests.Length==5,"Five existing dungeon chests installed without duplicate lids");
            var chest=chests.First();Move(p,chest.transform.position+Vector3.forward*1.5f);
            s.SpendGold(s.Gold);Check(!chest.TryOpen(p)&&!chest.Opened,"Insufficient coins preserve chest");
            s.AddGold(500);int before=Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None).Length;
            Check(chest.TryOpen(p)&&s.Gold==440,"Chest charges correct price");
            Check(!chest.TryOpen(p)&&s.Gold==440,"Chest cannot charge twice");
            Check(Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None).Length==before+1,"Chest drops one weapon");
            var shop=Object.FindFirstObjectByType<Merchant>();Check(shop.BeginIntermission(1),"Shop opens");
            Move(p,shop.transform.position+Vector3.forward*2);p.view.transform.LookAt(shop.transform.position+Vector3.up);
            p.TryInteract();Check(shop.Customer==p&&p.ControlsBlocked,"E raycast opens actual merchant");
            int price=shop.GetOffer(0).price;balance=s.Gold;
            await Task.Delay(100);
            Check(RunHUD.Instance.Purchase(0)&&s.Gold==balance-price,"Shop UI purchase delivers and charges");
            Check(!shop.TryPurchase(p,0)&&s.Gold==balance-price,"Sold stock cannot double-charge");
            s.SpendGold(s.Gold);Check(!shop.TryPurchase(p,1),"Shop rejects unaffordable purchase");
            shop.ContinueRound();Check(!p.ControlsBlocked&&!shop.Available,"Continue closes shop and releases player");
            RunNavigation.StartGame(PlayerClass.Tank);await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player!=p);
            p=RunDirector.Instance.Player;s=p.State;
            foreach(var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            Check(s.Class==PlayerClass.Tank&&s.ActiveWeapon.kind==WeaponKind.Hammer&&s.MaxHealth==180&&s.MoveSpeed==4.8f,"Tank stats and hammer");
            Move(p,new Vector3(0,10,0));p.transform.rotation=Quaternion.identity;p.view.transform.rotation=Quaternion.identity;
            var targets=new DestructiblePillar[2];
            for(int i=0;i<2;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.position=p.view.transform.position+Vector3.forward*1.5f+Vector3.right*(i==0?-.7f:.7f);go.transform.localScale=Vector3.one*.4f;targets[i]=go.AddComponent<DestructiblePillar>();}
            Physics.SyncTransforms();Check(p.GetComponent<PlayerCombat>().TryAttack()&&targets.All(t=>t.Health<t.maxHealth),"Hammer hits multiple targets in area");
            RunNavigation.StartGame(PlayerClass.Warrior);await Until(()=>!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance.Player!=p);
            p=RunDirector.Instance.Player;Check(p.State.Class==PlayerClass.Warrior&&p.State.ActiveWeapon.kind==WeaponKind.Sword&&p.State.MaxHealth==120,"Knight defaults");
            foreach(var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))enemy.SetPaused(true);
            Result="ECONOMY_CLASSES_PASSED: "+checks;Debug.Log(Result);
        }
        catch(Exception error){Result="FAILED: "+error;Debug.LogError(Result);}
    }
}
