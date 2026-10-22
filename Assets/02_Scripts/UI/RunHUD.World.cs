using TMPro;
using Text = TMPro.TextMeshProUGUI;
using System.Collections.Generic;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        readonly Dictionary<Component,WorldHealthBar> worldBars=new Dictionary<Component,WorldHealthBar>();
        readonly List<Component> removed=new List<Component>();
        readonly List<FloatingNumber> numbers=new List<FloatingNumber>();
        WeaponPickup[] weapons=new WeaponPickup[0];LootPickup[] loot=new LootPickup[0];
        TreasureChest[] chests=new TreasureChest[0];
        RectTransform tooltip;Text tooltipText, equippedComparison, groundComparison;
        readonly Text[] waypoints=new Text[3];
        float nextScan;
        void BuildWorld()
        {
            tooltip=Panel("Pickup comparison",0,0,630,205);tooltipText=UIWidgets.Label(tooltip,"Details",18,14,594,180,19);
            equippedComparison=UIWidgets.Label(tooltip,"Equipped comparison",18,14,285,150,19);groundComparison=UIWidgets.Label(tooltip,"Ground comparison",327,14,285,150,19);tooltip.gameObject.SetActive(false);
            for(int i=0;i<3;i++){waypoints[i]=UIWidgets.Label(transform,"Waypoint "+i,0,0,240,45,18,ProceduralUISprites.Gold);waypoints[i].alignment=TextAlignmentOptions.Center;}
        }
        public string DescribePickup(Component item)
        {
            if(item is Merchant shop)return shop.Available?"MERCADER ERRANTE\n[E] Abrir tienda • Comprar armas, pasivas y vida":"";
            if(item is TreasureChest chest)return chest.Opened?"COFRE ABIERTO":$"COFRE DE ARMAS • {chest.price} ORO\n[E] Abrir y obtener un arma aleatoria\n"+(Player.State.Gold<chest.price?"Te faltan "+(chest.price-Player.State.Gold)+" monedas":"Saldo: "+Player.State.Gold+" oro");
            if(item is WeaponPickup pickup)
            {
                var ground=pickup.weapon;var held=Player.State.ActiveWeapon;if(ground==null)return "";
                string comparison=held==null?"Sin arma":$"{UIWidgets.WeaponName(held.kind)} T{held.tier}  /  {held.Damage:0} daño  /  {held.AttacksPerSecond:0.00}/s\n{UIWidgets.Special(held.kind)}";
                string change=held==null||ground.Damage>=held.Damage?"<color=#57DBDB>▲</color>":"<color=#D92B4B>▼</color>";
                return $"EQUIPADA: {comparison}\n<color=#{ColorUtility.ToHtmlStringRGB(ProceduralUISprites.Tier(ground.tier))}>SUELO: {UIWidgets.WeaponName(ground.kind)} T{ground.tier}</color>  {change} {ground.Damage:0} daño / {ground.AttacksPerSecond:0.00}/s\n{UIWidgets.Special(ground.kind)}\n[E] Recoger / intercambiar arma activa";
            }
            if(item is LootPickup p)
            {
                if(p.IsAmmo)return $"{p.AmmoName}  /  +{p.amount}\nTocar / [E] recoger";
                string text=UIWidgets.Passive(p.passive)+"\nTocar / [E] recoger • T/Y/U activar (10 s)";
                if(Player.State.PassiveCount==3){text="REEMPLAZAR POR: "+UIWidgets.Passive(p.passive);for(int i=0;i<3;i++)text+="\n["+new[]{"Shift","Ctrl","Alt"}[i]+" + E] "+UIWidgets.Passive(Player.State.GetPassive(i));}
                return text;
            }
            return item is RunPortal?"PORTAL ABIERTO\n[E] Viajar al "+RoundRules.SceneForRound(RunDirector.Instance.Level+1):"";
        }
        static string CompareWeapon(WeaponInstance w)=>w==null?"Vacía":$"<color=#{ColorUtility.ToHtmlStringRGB(ProceduralUISprites.Tier(w.tier))}>{UIWidgets.WeaponName(w)} T{w.tier}</color>\n{w.Damage:0} daño • {w.AttacksPerSecond:0.00}/s\n{UIWidgets.Special(w.kind)}";
        void ScanWorld()
        {
            foreach(var enemy in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))if(enemy.IsAlive&&!worldBars.ContainsKey(enemy))worldBars.Add(enemy,WorldHealthBar.Create(enemy));
            foreach(var pillar in FindObjectsByType<DestructiblePillar>(FindObjectsSortMode.None))if(!worldBars.ContainsKey(pillar))worldBars.Add(pillar,WorldHealthBar.Create(pillar));
            removed.Clear();foreach(var pair in worldBars)if(!pair.Key||!pair.Key.gameObject.activeInHierarchy){if(pair.Value)Destroy(pair.Value.gameObject);removed.Add(pair.Key);}foreach(var key in removed)worldBars.Remove(key);
            weapons=FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);loot=FindObjectsByType<LootPickup>(FindObjectsSortMode.None);
            chests=FindObjectsByType<TreasureChest>(FindObjectsSortMode.None);
        }
        void UpdateWorld()
        {
            Camera camera=Player.view?Player.view:Camera.main;if(!camera)return;
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.25f;ScanWorld();}
            foreach(var bar in worldBars.Values)if(bar)bar.Refresh(camera);
            bool overlay=Paused||ShopVisible||!Player.State.IsAlive||(victoryPanel&&victoryPanel.gameObject.activeSelf);
            Component focus=InteractionTarget.Find(Player);
            tooltip.gameObject.SetActive(focus&&!overlay);
            if(focus&&!overlay){tooltipText.text=DescribePickup(focus);
                bool compare=focus is WeaponPickup;equippedComparison.enabled=groundComparison.enabled=compare;
                if(compare){var w=((WeaponPickup)focus).weapon;var held=Player.State.ActiveWeapon;
                    equippedComparison.text="EQUIPADA\n"+CompareWeapon(held);groundComparison.text="EN EL SUELO\n"+CompareWeapon(w)+"\n"+(held==null||w.Damage>=held.Damage?"<color=#57DBDB>▲ Daño</color>":"<color=#D92B4B>▼ Daño</color>");
                    tooltipText.text="\n\n\n\n\n\n[E] Recoger / intercambiar arma activa";}
                PositionOnScreen(tooltip,camera.WorldToScreenPoint(focus.transform.position+Vector3.up*.7f),new Vector2(20,-25));}
            for(int i=0;i<waypoints.Length;i++)waypoints[i].enabled=false;
            if(!overlay)
            {
                if(Context.dungeon&&Context.dungeon.merchant&&Context.dungeon.merchant.Available)Waypoint(0,Context.dungeon.merchant.transform.position,"• MERCADER",camera);
                if(Context.portal&&Context.portal.Available)Waypoint(1,Context.portal.transform.position,"○ PORTAL",camera);
                var active=Player.State.ActiveWeapon;
                if(active!=null&&active.Capacity>0&&active.ammunition<=active.Capacity/3)
                {LootPickup nearest=null;float best=float.MaxValue;foreach(var p in loot)if(p&&p.Available&&p.IsAmmo&&p.Supports(active)){float d=(p.transform.position-Player.transform.position).sqrMagnitude;if(d<best){best=d;nearest=p;}}if(nearest)Waypoint(2,nearest.transform.position,"↑ MUNICIÓN",camera);}
            }
            for(int i=numbers.Count-1;i>=0;i--)
            {
                var number=numbers[i];float age=Time.unscaledTime-number.born;
                if(age>1.05f){Destroy(number.label.gameObject);numbers.RemoveAt(i);continue;}
                var screen=camera.WorldToScreenPoint(number.position+Vector3.up*age*.9f);number.label.enabled=screen.z>0;
                if(number.self)screen=new Vector3(Screen.width*.5f,Screen.height*.57f+age*50,1);
                PositionOnScreen(number.label.rectTransform,screen,new Vector2(-80,0));var color=number.label.color;color.a=Mathf.Clamp01((1.05f-age)*3);number.label.color=color;
            }
        }
        void PositionOnScreen(RectTransform rect,Vector3 screen,Vector2 offset)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var local);
            var bounds=((RectTransform)transform).rect;
            rect.anchoredPosition=new Vector2(Mathf.Clamp(local.x-bounds.xMin+offset.x,10,bounds.width-rect.rect.width-10),Mathf.Clamp(local.y-bounds.yMax+offset.y,-bounds.height+rect.rect.height+10,-10));
        }
        void Waypoint(int index,Vector3 position,string name,Camera camera)
        {
            Vector3 screen=camera.WorldToScreenPoint(position+Vector3.up*1.7f);bool off=screen.z<=0||screen.x<60||screen.x>Screen.width-60||screen.y<60||screen.y>Screen.height-60;
            if(screen.z<=0){screen.x=Screen.width-screen.x;screen.y=Screen.height-screen.y;}
            screen.x=Mathf.Clamp(screen.x,125,Screen.width-125);screen.y=Mathf.Clamp(screen.y,190,Screen.height-225);
            Vector2 direction=new Vector2(screen.x-Screen.width*.5f,screen.y-Screen.height*.5f);
            string arrow=Mathf.Abs(direction.y)>Mathf.Abs(direction.x)?(direction.y>0?"▲ ":"▼ "):(direction.x<0?"← ":"→ ");
            waypoints[index].enabled=true;waypoints[index].text=(off?arrow:"")+name+$"  {Vector3.Distance(Player.transform.position,position):0}m";
            PositionOnScreen(waypoints[index].rectTransform,screen,new Vector2(-120,0));
        }
        sealed class FloatingNumber{public Text label;public Vector3 position;public float born;public bool self;}
        void SpawnNumber(Vector3 position,string value,Color color,bool self)
        {
            if(numbers.Count>=48){Destroy(numbers[0].label.gameObject);numbers.RemoveAt(0);}
            var label=UIWidgets.Label(transform,"Combat number",0,0,180,55,27,color);label.text=value;label.alignment=TextAlignmentOptions.Center;
            numbers.Add(new FloatingNumber{label=label,position=position,born=Time.unscaledTime,self=self});
        }
    }
    public sealed class WorldHealthBar : MonoBehaviour
    {
        Component target;GhostBar health;Text status;Canvas canvas;Image shieldIcon;
        public GhostBar Health => health;
        public static WorldHealthBar Create(Component target)
        {
            var root=new GameObject("World health • "+target.name,typeof(RectTransform));root.transform.SetParent(target.transform,false);
            var bar=root.AddComponent<WorldHealthBar>();bar.target=target;
            bar.canvas=root.AddComponent<Canvas>();bar.canvas.renderMode=RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta=new Vector2(200,65);root.transform.localScale=Vector3.one*.012f;
            bar.health=GhostBar.Create(root.transform,"Integrity",0,28,200,24);
            bar.status=UIWidgets.Label(root.transform,"Status",0,0,200,26,19,ProceduralUISprites.Gold);bar.status.alignment=TextAlignmentOptions.Center;
            bar.shieldIcon=UIWidgets.Image(root.transform,"Guard icon",-27,25,24,28,ProceduralUISprites.Gold);bar.shieldIcon.sprite=ProceduralUISprites.Weapon(WeaponKind.Shield);return bar;
        }
        public void Refresh(Camera camera)
        {
            if(!target){Destroy(gameObject);return;}
            float hp,max;string label="";
            if(target is BasicEnemy enemy){hp=enemy.Health;max=enemy.MaxHealth;label=enemy.IsStunned?"* ATURDIDO":enemy.miniboss?"GUARDIÁN • MINIJEFE":!string.IsNullOrEmpty(enemy.variantName)?enemy.variantName:enemy.kind==EnemyKind.Heavy?(enemy.GuardBroken?"○ ESCUDO ROTO":"• ESCUDO"):"";}
            else if(target is DestructiblePillar pillar){hp=pillar.Health;max=pillar.maxHealth;label="PILAR";}else return;
            var collider=target.GetComponent<Collider>();Vector3 position=collider?new Vector3(collider.bounds.center.x,collider.bounds.max.y+.35f,collider.bounds.center.z):target.transform.position+Vector3.up*2.5f;
            transform.position=position;transform.rotation=camera.transform.rotation;
            bool visible=hp>0&&(!(target is DestructiblePillar)||hp<max)&&Vector3.Distance(camera.transform.position,position)<35;
            // Depth-tested world-space UI remains behind walls.
            shieldIcon.enabled=target is BasicEnemy heavy&&heavy.kind==EnemyKind.Heavy;shieldIcon.color=target is BasicEnemy broken&&broken.GuardBroken?ProceduralUISprites.Crimson:ProceduralUISprites.Gold;
            if(target is BasicEnemy guarded)shieldIcon.transform.localRotation=Quaternion.Euler(0,0,guarded.GuardBroken?35:0);
            canvas.enabled=visible;health.Set(hp,max);status.text=label;
        }
    }
}
