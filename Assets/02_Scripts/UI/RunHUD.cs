using TMPro;
using Text = TMPro.TextMeshProUGUI;
using System.Collections.Generic;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Codice.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed partial class RunHUD : MonoBehaviour
    {
        public static RunHUD Instance {get;private set;}
        public static bool HandlesEscapeThisFrame => Instance && escapeFrame==Time.frameCount;
        static int escapeFrame=-1;
        public RunScene Context {get;private set;}
        public PlayerCharacter Player => Context ? Context.player : null;
        public GhostBar PlayerHealth {get;private set;}
        public GhostBar PlayerMana {get;private set;}
        GhostBar posture; Image postureLeft,postureRight;
        public GhostBar BossHealth {get;private set;}
        public Image UltimateMeter {get;private set;}
        public bool Paused {get;private set;}
        public bool ShopVisible => shopPanel && shopPanel.gameObject.activeSelf;
        public bool ResultsVisible => (victoryPanel && victoryPanel.gameObject.activeSelf) || (defeatPanel && defeatPanel.gameObject.activeSelf);
        public bool TooltipVisible => tooltip && tooltip.gameObject.activeSelf;
        public int WorldBarCount => worldBars.Count;
        public Canvas Canvas {get;private set;}
        RunDirector director;
        PlayerMechanics mechanics;
        Text spellChoices;
        Text healthTitle,status,gold,timers,level,progressText,bossTitle,bossAttack,ultimateText,ammo,dash,banner,crosshair,hitmarker,cursorHint;
        Image progress,vignette,reload,ammoReload;
        int displayedSlot=-1;float switchTime;
        readonly Image[] slots=new Image[2], icons=new Image[2];
        readonly Text[] slotLabels=new Text[2], passiveLabels=new Text[3];
        readonly Image[] passiveFrames=new Image[3];
        RectTransform spellPanel;
        RectTransform healthPanel,hotbar,bossPanel,bannerPanel;
        readonly Queue<string> notices=new Queue<string>();
        float bannerEnds,oldHealth,damagePulse,goldPulse,hitEnds;
        int oldGold; bool ready;
        public static RunHUD Install(RunScene scene)
        {
            if(Instance && Instance.Context==scene)return Instance;
            if(Instance){Instance.gameObject.SetActive(false);Destroy(Instance.gameObject);}
            var go=new GameObject("Códice • Interfaz",typeof(RectTransform));var hud=go.AddComponent<RunHUD>();hud.Context=scene;hud.Build();return hud;
        }
        void Awake(){Instance=this;}
        void Build()
        {
            Canvas=gameObject.AddComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=40;
            UIWidgets.ConfigureCanvas(Canvas);
            gameObject.AddComponent<GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>())new GameObject("UI Event System",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
            vignette=UIWidgets.Image(transform,"Damage vignette",0,0,1920,1080,Color.clear,"vignette");Stretch(vignette.rectTransform);
            healthPanel=Panel("Vital signs",32,30,370,155);
            healthTitle=UIWidgets.Label(healthPanel,"Class",16,12,330,25,18,ProceduralUISprites.Gold);
            PlayerHealth=GhostBar.Create(healthPanel,"Player HP",14,43,340,34);
            status=UIWidgets.Label(healthPanel,"Status",18,83,330,42,16);
            gold=UIWidgets.Label(healthPanel,"Gold",18,121,330,29,23,ProceduralUISprites.Gold);
            PlayerMana=GhostBar.Create(transform,"Maná",46,192,340,36);PlayerMana.Fill.color=ProceduralUISprites.Cyan;PlayerMana.Caption.fontSize=16;
            spellPanel=Panel("Spell book",32,232,370,155);
            spellChoices=UIWidgets.Label(spellPanel,"Spell choices",18,12,334,133,18,Color.white);
            posture=GhostBar.Create(transform,"Postura",-170,95,340,32);Anchor((RectTransform)posture.transform,.5f,.5f);posture.Fill.color=ProceduralUISprites.Gold;posture.Caption.fontSize=14;
            postureLeft=UIWidgets.Filled(posture.transform,"Left posture",6,5,162,22,ProceduralUISprites.Gold);postureLeft.fillOrigin=1;
            postureRight=UIWidgets.Filled(posture.transform,"Right posture",172,5,162,22,ProceduralUISprites.Gold);postureRight.fillOrigin=0;
            UIWidgets.Image(posture.transform,"Center crest",165,1,10,30,ProceduralUISprites.Gold,"diamond");posture.Caption.transform.SetAsLastSibling();
            var timePanel=Panel("Run clock",-348,30,316,142);Anchor(timePanel,1,1);
            timers=UIWidgets.Label(timePanel,"Clocks",18,14,280,115,21);
            var round=Panel("Round",-265,25,530,94);Anchor(round,.5f,1);
            level=UIWidgets.Label(round,"Level",16,10,498,28,22);level.alignment=TextAlignmentOptions.Center;
            progress=UIWidgets.Filled(round,"Round progress",18,48,494,7,ProceduralUISprites.Cyan);
            progressText=UIWidgets.Label(round,"Progress",18,63,494,25,16);progressText.alignment=TextAlignmentOptions.Center;
            var merchantMark=UIWidgets.Label(round,"Merchant midpoint",245,35,35,25,18,ProceduralUISprites.Gold);merchantMark.text="•";
            hotbar=UIWidgets.Rect(transform,"Hotbar",-435,-210,870,150);Anchor(hotbar,.5f,0);
            for(int i=0;i<2;i++)
            {
                slots[i]=UIWidgets.Image(hotbar,"Weapon slot "+i,270+i*235,22,220,112,ProceduralUISprites.Tier(1),"frame");
                slots[i].rectTransform.pivot=new Vector2(.5f,.5f);slots[i].rectTransform.anchoredPosition+=new Vector2(110,-56);
                icons[i]=UIWidgets.Image(slots[i].transform,"Silhouette",10,17,57,65,Color.white);
                slotLabels[i]=UIWidgets.Label(slots[i].transform,"Weapon",74,16,134,90,18);
            }
            for(int i=0;i<3;i++)
            {
                passiveFrames[i]=UIWidgets.Image(hotbar,"Passive "+i,i*83,5,77,99,ProceduralUISprites.Ink,"hex");
                passiveLabels[i]=UIWidgets.Label(passiveFrames[i].transform,"Passive name",5,8,67,84,12);passiveLabels[i].alignment=TextAlignmentOptions.Center;
            }
            dash=UIWidgets.Label(hotbar,"Dash",0,116,260,48,17,ProceduralUISprites.Cyan);
            var medallion=UIWidgets.Image(hotbar,"Ultimate",750,8,110,110,ProceduralUISprites.Cyan,"diamond");
            UltimateMeter=UIWidgets.Filled(medallion.transform,"Ultimate charge",9,9,92,92,ProceduralUISprites.Cyan,true);
            ultimateText=UIWidgets.Label(medallion.transform,"Ultimate percentage",19,31,72,50,22);ultimateText.alignment=TextAlignmentOptions.Center;
            var special=UIWidgets.Label(hotbar,"Special name",640,122,330,50,16,ProceduralUISprites.Cyan);special.alignment=TextAlignmentOptions.Center;
            specialName=special;
            ammoReload=UIWidgets.Filled(hotbar,"Ammo regeneration",255,140,24,24,ProceduralUISprites.Cyan,true);
            ammo=UIWidgets.Label(hotbar,"Ammunition",275,143,455,25,17);ammo.alignment=TextAlignmentOptions.Center;
            bossPanel=Panel("Boss",-350,129,700,111);Anchor(bossPanel,.5f,1);
            bossTitle=UIWidgets.Label(bossPanel,"Boss name",18,10,664,28,21,ProceduralUISprites.Gold);bossTitle.alignment=TextAlignmentOptions.Center;
            BossHealth=GhostBar.Create(bossPanel,"Boss HP",18,42,664,28);
            foreach(float f in new[]{.3f,.65f})UIWidgets.Image(BossHealth.transform,"Phase threshold",5+654*f,3,2,24,ProceduralUISprites.Gold);
            bossAttack=UIWidgets.Label(bossPanel,"Telegraph",18,78,664,25,18);bossAttack.alignment=TextAlignmentOptions.Center;
            bannerPanel=Panel("Announcement",-360,255,720,62);Anchor(bannerPanel,.5f,1);
            banner=UIWidgets.Label(bannerPanel,"Notice",18,15,684,35,25,ProceduralUISprites.Gold);banner.alignment=TextAlignmentOptions.Center;bannerPanel.gameObject.SetActive(false);
            crosshair=UIWidgets.Label(transform,"Crosshair",-30,-30,60,60,32);Anchor(crosshair.rectTransform,.5f,.5f);crosshair.alignment=TextAlignmentOptions.Center;
            reload=UIWidgets.Filled(transform,"Reload arc",-34,-34,68,68,ProceduralUISprites.Cyan,true);Anchor(reload.rectTransform,.5f,.5f);
            hitmarker=UIWidgets.Label(transform,"Hitmarker",-100,35,200,45,23,ProceduralUISprites.Gold);Anchor(hitmarker.rectTransform,.5f,.5f);hitmarker.alignment=TextAlignmentOptions.Center;
            cursorHint=UIWidgets.Label(transform,"Cursor help",-370,-28,740,24,15);Anchor(cursorHint.rectTransform,.5f,0);cursorHint.alignment=TextAlignmentOptions.Center;
            BuildMenus();BuildWorld();
            mechanics=Player.GetComponent<PlayerMechanics>();oldHealth=Player.State.Health;oldGold=Player.State.Gold;
            Player.State.Changed+=StateChanged;Player.State.Died+=Died;
            if(mechanics)mechanics.Changed+=MechanicsChanged;
            director=RunDirector.Instance;if(director){director.Notice+=Announce;director.Changed+=RunChanged;}
            CombatFeedback.Resolved+=CombatResolved;
            StateChanged();Refresh();
        }
        Text specialName;
        RectTransform Panel(string name,float x,float y,float w,float h)=>UIWidgets.Image(transform,name,x,y,w,h,ProceduralUISprites.Ink,"frame").rectTransform;
        static void Anchor(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(x,y);}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        public void Announce(string message){if(notices.Count<8)notices.Enqueue(message);}
        void RunChanged(){Refresh();}
        void MechanicsChanged(){if(!mechanics)return;bool now=mechanics.Charge>=100;if(now&&!ready)Announce("¡Especial listo [Q]!");ready=now;}
        void StateChanged()
        {
            if(!Player)return;
            float hp=Player.State.Health;
            if(hp<oldHealth)damagePulse=1;
            else if(hp>oldHealth)SpawnNumber(Player.transform.position+Vector3.up*1.6f,"+"+(hp-oldHealth).ToString("0"),ProceduralUISprites.Cyan,true);
            if(Player.State.Gold>oldGold)goldPulse=1;
            oldHealth=hp;oldGold=Player.State.Gold;PlayerHealth.Set(hp,Player.State.MaxHealth);MechanicsChanged();
        }
        void Died(){SetPaused(false);}
        void Update()
        {
            if(!Context||!Player)return;
            var keys=Keyboard.current;
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame&&Player.State.IsAlive&&!ShopVisible)
            {escapeFrame=Time.frameCount;SetPaused(!Paused);}
            Refresh();UpdateMenus();UpdateWorld();UpdateBossAnnouncement();
            float dt=Time.unscaledDeltaTime;
            damagePulse=Mathf.MoveTowards(damagePulse,0,dt*1.8f);goldPulse=Mathf.MoveTowards(goldPulse,0,dt*2.5f);
            vignette.color=new Color(.85f,.08f,.19f,damagePulse*.65f);
            healthPanel.anchoredPosition=new Vector2(32+Mathf.Sin(Time.unscaledTime*83)*damagePulse*7,-30);
            gold.transform.localScale=Vector3.one*(1+.16f*goldPulse);
            if(Time.unscaledTime>=bannerEnds&&notices.Count>0){banner.text=notices.Dequeue();bannerEnds=Time.unscaledTime+2.6f;}
            bannerPanel.gameObject.SetActive(Time.unscaledTime<bannerEnds);
            bannerPanel.localScale=Vector3.one*Mathf.Lerp(.95f,1,Mathf.Clamp01((bannerEnds-Time.unscaledTime)*3));
            hitmarker.enabled=Time.unscaledTime<hitEnds;
        }
        public void Refresh()
        {
            if(!Player||!PlayerHealth)return;
            var state=Player.State;var weapon=state.ActiveWeapon;
            healthTitle.text="CÓDICE DE CENIZA  /  "+new[]{"GUERRERO","TANQUE","ARQUERO","MAGO","SHINOBI"}[(int)state.Class];
            PlayerHealth.Set(state.Health,state.MaxHealth);
            PlayerMana.gameObject.SetActive(state.HasStaff);PlayerMana.Set(state.Mana,state.MaxMana);PlayerMana.Caption.text=$"MANÁ {state.Mana:0}/{state.MaxMana:0} • {StaffSpells.Cost(state.SelectedSpell):0} por hechizo";
            var guard=Player.GetComponent<ShinobiGuard>();posture.gameObject.SetActive(guard&&state.Class==PlayerClass.Shinobi&&guard.ShowPosture);if(guard){posture.Set(100-guard.Posture,100,guard.Broken?"POSTURA ROTA":guard.RecentPerfect?"DESVÍO PERFECTO":"POSTURA");posture.Fill.color=guard.Broken?ProceduralUISprites.Crimson:guard.RecentPerfect?Color.white:ProceduralUISprites.Gold;postureLeft.fillAmount=postureRight.fillAmount=(100-guard.Posture)/100;postureLeft.color=postureRight.color=posture.Fill.color;posture.Fill.enabled=false;posture.Ghost.enabled=false;}
            status.text=Player.IsStaggered?"DERRIBADO":Player.MovementMultiplier<1?"LODO • Ralentizado −35%":mechanics&&mechanics.Invulnerable?"INVULNERABLE":mechanics&&mechanics.ShadowStep?"PASO SOMBRÍO":mechanics&&mechanics.ShieldActive?"ESCUDO FRONTAL • −50%":"";
            gold.text=$"•  {state.Gold:N0}   ORO";
            if(displayedSlot!=state.ActiveSlot){displayedSlot=state.ActiveSlot;switchTime=Time.unscaledTime;}
            for(int i=0;i<2;i++)
            {
                var w=state.GetWeapon(i);bool active=i==state.ActiveSlot;
                slots[i].color=w==null?ProceduralUISprites.Ink:ProceduralUISprites.Tier(w.tier)*(active?1:.65f);
                slots[i].transform.localScale=Vector3.Lerp(slots[i].transform.localScale,Vector3.one*(active?1.15f+Mathf.Sin(Mathf.Clamp01((Time.unscaledTime-switchTime)/.3f)*Mathf.PI)*.12f:1),1-Mathf.Exp(-15*Time.unscaledDeltaTime));
                slots[i].rectTransform.anchoredPosition=new Vector2(380+i*235,-78+(active?10:0));
                icons[i].enabled=w!=null;if(w!=null)icons[i].sprite=ProceduralUISprites.Weapon(w);
                slotLabels[i].text=w==null?$"[{i+1}]  VACÍO":$"[{i+1}] {UIWidgets.WeaponName(w)}\nT{w.tier}  /  {w.Damage:0} DAÑO\n{w.AttacksPerSecond:0.00}/s  [Tab]";
            }
            for(int i=0;i<3;i++){bool exists=i<state.PassiveCount;passiveLabels[i].text="["+new[]{"T","Y","U"}[i]+"]\n"+(exists?UIWidgets.Passive(state.GetPassive(i))+"\n"+(state.PassiveTime(i)>0?$"{state.PassiveTime(i):0}s":state.PassiveWait(i)>0?$"Espera {state.PassiveWait(i):0}s":"LISTA"):"—");passiveFrames[i].color=exists?ProceduralUISprites.Cyan:ProceduralUISprites.Ink;}
            float charge=mechanics?mechanics.Charge:0;UltimateMeter.fillAmount=charge/100;
            UltimateMeter.color=charge>=100?Color.Lerp(ProceduralUISprites.Cyan,ProceduralUISprites.Gold,(Mathf.Sin(Time.unscaledTime*6)+1)/2):ProceduralUISprites.Cyan;
            ultimateText.text=mechanics&&mechanics.UltimateActive?"ACTIVO":$"{Mathf.FloorToInt(charge)}%\n[Q]";
            specialName.text=weapon==null?"":(weapon.katana?"Tajos de Sangre":weapon.shuriken?"Tormenta de Shuriken":UIWidgets.Special(weapon.kind))+"\n[Q]";
            ammo.text=weapon!=null&&weapon.Capacity>0?$"{(weapon.shuriken?"SHURIKEN":weapon.kind==WeaponKind.Bow?"FLECHAS":"MUNICIÓN")}   {weapon.ammunition} / {weapon.Capacity}"+(weapon.kind==WeaponKind.Musket?$"  •  Reserva {weapon.reserveAmmunition}  [R]":"  •  Recoge suministros") : "";
            var combat=Player.GetComponent<PlayerCombat>();
            spellPanel.gameObject.SetActive(weapon!=null&&weapon.kind==WeaponKind.Staff);
            if(weapon!=null&&weapon.kind==WeaponKind.Staff)
            {
                spellChoices.text="HECHIZOS • Clic izquierdo para lanzar\n";
                for(int i=0;i<4;i++){var s=(StaffSpell)i;spellChoices.text+=(s==state.SelectedSpell?"<color=#FFD978>> ":"  ")+"["+(i+1)+"] "+StaffSpells.Label(s)+" • "+StaffSpells.Cost(s)+" maná"+(s==state.SelectedSpell?"</color>":"")+"\n";}
                ammo.text=$"{StaffSpells.Label(state.SelectedSpell)} • Espera {Player.GetComponent<StaffSpells>().Remaining:0.0}s • [Tab] Arma • [Q] Especial";
            }
            if(state.DualWield)ammo.text="DUALES • +30% velocidad / +25% daño";
            if(combat&&combat.IsReloading)ammo.text+="  •  RECARGANDO";
            reload.fillAmount=combat&&combat.IsReloading?combat.ReloadFraction:0;
            ammoReload.fillAmount=reload.fillAmount;
            crosshair.text=weapon==null?"·":weapon.kind==WeaponKind.Bow?"○":weapon.kind==WeaponKind.Musket?"┼":weapon.Ranged?"○":"·";
            dash.text="SALTO [Espacio] • DASH [Shift]\n"+(Player.DashCooldown<=0?"• LISTO":$"○ {Player.DashCooldown:0.0}s")+(state.HasPassive(PassiveKind.MercuryBoots)?(Player.SecondDashCooldown<=0?"   • LISTO":$"   ○ {Player.SecondDashCooldown:0.0}s"):"");
            if(director)
            {
                timers.text=$"PARTIDA   {UIWidgets.Clock(director.Elapsed)}\nRONDA     {UIWidgets.Clock(director.RoundElapsed)}\n<size=16><color={(director.RoundElapsed<180?"#F5D76E":"#777C91")}>• RAPIDEZ +100 • MENOS DE 03:00</color></size>";
                level.text=$"RONDA {director.Level:00} / {RoundRules.FinalRound}   •   {RoundRules.MapTitle(director.Level)}";
                if(Context.UsesDungeonEncounter(director.Level)){int total=Context.dungeon.Total;progress.fillAmount=total>0?(float)Context.dungeon.Defeated/total:0;progressText.text=$"{total-Context.dungeon.Defeated} / {total} ENEMIGOS  •  MERCADER {(total+1)/2}/{total}  •  JEFE CADA 5";}
                else{progress.fillAmount=Context.arena&&Context.arena.Completed?1:0;progressText.text="ENCUENTRO DE JEFE";}
                if(director.Phase==RunPhase.Shop)progressText.text=$"MERCADER • [E] COMPRAR • [ENTER] CONTINUAR • {director.ShopRemaining:0}s";
                else if(director.Phase==RunPhase.Cleared)progressText.text=RoundRules.OpensPortalAfter(director.Level)?"RONDA COMPLETADA • ENTRA AL PORTAL [E]":"RONDA COMPLETADA • PREPARANDO SIGUIENTE RONDA";
            }
            var boss=Context.arena?Context.arena.ActiveBoss:null;bossPanel.gameObject.SetActive(boss);
            if(boss)
            {bossTitle.text=BossName(boss.kind)+(boss.Evolved?" • EVOLUCIONADO Nv.15+":"");BossHealth.Set(boss.Health,boss.MaxHealth);bossAttack.text=$"FASE {boss.Phase}   /   "+new[]{"Acechando","¡Roca entrante!","¡Lodo!","¡Barrido!","¡Golpe sísmico!","¡Salva de flechas!"}[(int)boss.CurrentAttack];}
            cursorHint.text=Paused?"[Escape] Reanudar y capturar cursor":ShopVisible?"[E / Escape] Cerrar tienda   •   [Enter] Continuar ronda":Cursor.lockState==CursorLockMode.Locked?"[E] Interactuar   •   [Escape] Pausa / liberar cursor":"Haz clic para capturar el cursor   •   [Escape] Pausa";
        }
        static string BossName(BossKind kind)=>kind==BossKind.GoblinKing?"REY GOBLIN DE LA INMUNDICIA":kind==BossKind.DemonLord?"SEÑOR DEMONIO DEL ABISMO":kind==BossKind.MonsterOrc?"MONSTER ORC":kind==BossKind.MonsterMutant?"MONSTER MUTANT":"ORK BERSERKER • JEFE FINAL";
        void CombatResolved(CombatFeedback.Result result)
        {
            if(!Player||!result.hit.source||result.hit.source.GetComponentInParent<PlayerCharacter>()!=Player)return;
            hitEnds=Time.unscaledTime+.22f;
            hitmarker.text=result.blocked?"×  BLOQUEADO −80%":result.hit.critical?"×  CRÍTICO ×2":"×";
            hitmarker.color=result.blocked?ProceduralUISprites.Ink:result.hit.critical?ProceduralUISprites.Gold:Color.white;
            SpawnNumber(result.position,$"{result.amount:0}"+(result.blocked?" • ESCUDO":result.hit.critical?" !":result.hit.reflected?" • ESPINAS":""),hitmarker.color,false);
        }
        void OnDisable()
        {
            SetPaused(false);
            if(Player){Player.State.Changed-=StateChanged;Player.State.Died-=Died;}
            if(mechanics)mechanics.Changed-=MechanicsChanged;
            if(director){director.Notice-=Announce;director.Changed-=RunChanged;}
            CombatFeedback.Resolved-=CombatResolved;
            foreach(var bar in worldBars.Values)if(bar)Destroy(bar.gameObject);
            worldBars.Clear();
            if(Instance==this)Instance=null;
        }
    }
}
