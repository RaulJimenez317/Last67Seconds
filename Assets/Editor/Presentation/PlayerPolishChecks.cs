using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class PlayerPolishChecks
{
    public static string Result="Not run";
    static int checks;
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;}
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Fields).SetValue(owner,value);
    static void Move(PlayerCharacter p,Vector3 position)
    {var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(position,Quaternion.identity);cc.enabled=true;p.view.transform.localRotation=Quaternion.identity;Physics.SyncTransforms();}
    static void MouseState(Mouse mouse,MouseButton? button)
    {var state=new UnityEngine.InputSystem.LowLevel.MouseState();if(button.HasValue)state=state.WithButton(button.Value);InputSystem.QueueStateEvent(mouse,state);InputSystem.Update();}
    public static async void Run()
    {
        if(!Application.isPlaying||Result=="Running")return;
        Result="Running";checks=0;Application.runInBackground=true;
        Mouse mouse=null;GameObject floor=null,target=null,wall=null,selfCollider=null;
        try
        {
            var p=RunDirector.Instance.Player;
            Check(p&&p.State.IsAlive&&!p.ControlsBlocked,"live player fixture");
            foreach(var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>())e.SetPaused(true);
            p.enabled=false;var combat=p.GetComponent<PlayerCombat>();combat.enabled=false;
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,99.5f,1000);floor.transform.localScale=new Vector3(30,1,30);
            Move(p,new Vector3(1000,100.05f,1000));
            mouse=InputSystem.AddDevice<Mouse>();Cursor.lockState=CursorLockMode.Locked;
            selfCollider=GameObject.CreatePrimitive(PrimitiveType.Cube);selfCollider.transform.SetParent(p.view.transform,false);selfCollider.transform.localPosition=Vector3.forward*.4f;selfCollider.transform.localScale=Vector3.one*.15f;
            foreach(PlayerClass playerClass in Enum.GetValues(typeof(PlayerClass)))
            foreach(WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
            {
                p.State.Initialize(playerClass);p.State.Equip(new WeaponInstance(kind));
                target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=p.view.transform.position+Vector3.forward*1.4f;target.transform.localScale=Vector3.one*.6f;
                var pillar=target.AddComponent<DestructiblePillar>();Physics.SyncTransforms();
                Set(combat,"nextAttack",0f);MouseState(mouse,MouseButton.Right);combat.SendMessage("Update");
                await Task.Delay(70);
                Check(pillar.Health==pillar.maxHealth,"right click never attacks: "+playerClass+" / "+kind);
                if(playerClass==PlayerClass.Warrior)Check(combat.Blocking,"warrior right click blocks with "+kind);
                MouseState(mouse,MouseButton.Left);combat.SendMessage("Update");
                await Task.Delay(90);
                Check(pillar.Health<pillar.maxHealth,"left click hits through own collider: "+playerClass+" / "+kind);
                MouseState(mouse,null);Object.Destroy(target);target=null;await Task.Delay(20);
            }
            Object.Destroy(selfCollider);selfCollider=null;await Task.Delay(30);
            p.State.Initialize(PlayerClass.Tank);p.State.Equip(new WeaponInstance(WeaponKind.Axe));Set(combat,"nextAttack",0f);
            target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=p.view.transform.position+Vector3.forward*1.8f;
            var behindWall=target.AddComponent<DestructiblePillar>();
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=p.view.transform.position+Vector3.forward*.9f;wall.transform.localScale=new Vector3(2,2,.2f);Physics.SyncTransforms();
            Check(combat.TryAttack()&&behindWall.Health==behindWall.maxHealth,"melee respects walls");
            Object.Destroy(wall);Object.Destroy(target);wall=target=null;await Task.Delay(30);
            // A close target's centre is ahead even when its closest point lies below the eye.
            target=GameObject.CreatePrimitive(PrimitiveType.Capsule);target.transform.position=p.transform.position+Vector3.up*.7f+Vector3.forward*.45f;target.transform.localScale=new Vector3(.6f,.65f,.6f);
            var close=target.AddComponent<DestructiblePillar>();Set(combat,"nextAttack",0f);Physics.SyncTransforms();
            Check(combat.TryAttack()&&close.Health<close.maxHealth,"close melee target registers");Object.Destroy(target);target=null;await Task.Delay(30);
            var chestRoot=GameObject.CreatePrimitive(PrimitiveType.Cube);chestRoot.name="chest";chestRoot.transform.position=p.transform.position+Vector3.forward*2+Vector3.up*.5f;
            TreasureChest.InstallInScene();var chest=chestRoot.GetComponent<TreasureChest>();
            p.view.transform.LookAt(chestRoot.transform.position);Physics.SyncTransforms();
            Check(chest&&InteractionTarget.Find(p)==chest,"plain chest has matching prompt target");
            p.State.AddGold(100);int gold=p.State.Gold;p.TryInteract();
            Check(chest.Opened&&p.State.Gold==gold-chest.price,"E opens selected chest exactly once");p.TryInteract();Check(p.State.Gold==gold-chest.price,"opened chest cannot charge twice");Object.Destroy(chestRoot);
            var coin=GoldPickup.Drop(p.transform.position+Vector3.right*5+Vector3.up*3,10,p);await Task.Delay(30);
            var bounds=coin.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();
            Check(bounds.Min(b=>b.min.y)>=100&&bounds.Min(b=>b.min.y)<100.05f,"gold rests on floor from elevated spawn");
            Check(Vector3.Distance(bounds[0].center,coin.transform.position)<.02f,"coin visual matches pickup collider");
            var coinPosition=coin.transform.position;await Task.Delay(200);Check(Vector3.Distance(coin.transform.position,coinPosition)<.01f,"gold does not fly toward player");
            gold=p.State.Gold;Check(coin.Collect(p)&&p.State.Gold==gold+10&&!coin.Collect(p),"gold collection remains unique");
            p.view.transform.localRotation=Quaternion.identity;
            p.GetComponent<CharacterController>().Move(Vector3.down*.2f);Set(p,"verticalSpeed",-2f);
            Check(p.Grounded&&p.TryJump(),"grounded jump accepted");Check(!p.IsDashing&&!p.TryJump(),"jump is not dash and cannot repeat in air");
            p.enabled=true;Cursor.lockState=CursorLockMode.Locked;float start=p.transform.position.y;
            await Task.Delay(180);Check(p.transform.position.y>start+.2f,"jump moves player upward");
            await Task.Delay(1000);Check(p.Grounded&&Mathf.Abs(p.transform.position.y-100)<.2f,"jump returns to floor");p.enabled=false;
            Time.timeScale=0;Check(!p.TryJump(),"pause blocks jump");Time.timeScale=1;
            p.State.Initialize(PlayerClass.Archer);combat.SendMessage("Update");await Task.Delay(100);
            var bow=combat.weaponModels[(int)WeaponKind.Bow];
            var bowBounds=bow.GetComponentInChildren<Renderer>().bounds;
            Check(bow.activeInHierarchy&&Mathf.Max(bowBounds.size.x,bowBounds.size.y,bowBounds.size.z)>.65f,"bow has readable size");
            Check(p.view.WorldToViewportPoint(bowBounds.center).z>0,"bow ahead of camera");
            Check(p.view.transform.Find("Nocked arrow")&&p.view.transform.Find("Nocked arrow").gameObject.activeSelf,"nocked arrow visible");
            Set(combat,"nextAttack",0f);Check(combat.TryAttack(),"bow shoots");
            var arrow=Object.FindObjectsByType<PlayerProjectile>().FirstOrDefault(s=>s.GetComponentInChildren<Renderer>());
            Check(arrow,"fired arrow has rendered model");
            Result="PLAYER_POLISH_PASSED: "+checks;Debug.Log(Result);
        }
        catch(Exception e){Result="FAILED: "+e;Debug.LogError(Result);}
        finally
        {
            Time.timeScale=1;if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(target)Object.Destroy(target);if(wall)Object.Destroy(wall);if(selfCollider)Object.Destroy(selfCollider);if(floor)Object.Destroy(floor);
        }
    }
}
