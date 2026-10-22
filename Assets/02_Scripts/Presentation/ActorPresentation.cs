using System;
using System.Linq;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
namespace Codice.Presentation
{
    // Animation observes combat; animation events never apply damage or change cooldowns.
    [DefaultExecutionOrder(100)]
    public sealed class ActorPresentation : MonoBehaviour
    {
        public Animator Body {get;private set;}
        public Animator Arms {get;private set;}
        public Transform WeaponSocket {get;private set;}
        public string CurrentState {get;private set;}="Idle";
        PlayerCharacter player;BasicEnemy enemy;BossCharacter boss;NavMeshAgent agent;
        GameObject armsRoot;int selectedClass=-1;float lockedUntil,oldHealth,stepDistance;Vector3 previous;
        bool dead,weaponsCalibrated;WeaponKind previousWeapon=(WeaponKind)(-1);
        GameObject nockedArrow,offhand,heldStar; WeaponKind offhandKind; Quaternion offhandRest; int offhandTier=-1; float throwUntil,throwRelease; bool dualWasVisible; int katanaSlash; float bowShotAt=-100,bowCycle=.8f,musketShotAt=-100;
        public static ActorPresentation Install(GameObject actor)
        {
            if(!PresentationLibrary.Load())return null;
            var p=actor.GetComponent<ActorPresentation>();return p?p:actor.AddComponent<ActorPresentation>();
        }
        void Start()
        {
            player=GetComponent<PlayerCharacter>();enemy=GetComponent<BasicEnemy>();boss=GetComponent<BossCharacter>();agent=GetComponent<NavMeshAgent>();previous=transform.position;
            if(player){oldHealth=player.State.Health;if(Codice.Multiplayer.LanAvatar.IsRemote(player)){Body=player.classVisuals[(int)player.State.Class].GetComponentInChildren<Animator>();Configure(Body,PresentationLibrary.Load().medium);}else SetupPlayer();}
            else if(enemy){Body=enemy.animator;if(!enemy.useNativeAnimator)Configure(Body,PresentationLibrary.Load().medium);else if(Body){Body.applyRootMotion=false;Body.cullingMode=AnimatorCullingMode.AlwaysAnimate;}oldHealth=enemy.Health;}
            else if(boss){Body=boss.visual.GetComponentInChildren<Animator>();if(!Body)Body=boss.visual.GetChild(0).gameObject.AddComponent<Animator>();if(!boss.useNativeAnimator)Configure(Body,boss.kind==BossKind.GoblinKing?PresentationLibrary.Load().goblin:PresentationLibrary.Load().demon);else {Body.applyRootMotion=false;Body.cullingMode=AnimatorCullingMode.AlwaysAnimate;}oldHealth=boss.Health;AttachBossWeapons();}
        }
        static void Configure(Animator a,RuntimeAnimatorController controller){if(!a)return;a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.speed=1;a.SetFloat("ActionSpeed",1);}
        static Transform Bone(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase));
        void SetupPlayer()
        {
            if(player.classVisuals==null||player.classVisuals.Length<3||!player.view)return;
            int index=(int)player.State.Class;if(index==selectedClass)return;selectedClass=index;
            if(armsRoot){DetachWeapons();Destroy(armsRoot);}
            var combatModels=player.GetComponent<PlayerCombat>();
            if(index==(int)PlayerClass.Shinobi&&combatModels.weaponModels[0].name!="Katana"){var blade=Resources.Load<GameObject>("Katana");if(blade){Destroy(combatModels.weaponModels[0]);combatModels.weaponModels[0]=Instantiate(blade);combatModels.weaponModels[0].name="Katana";}}
            var visual=player.classVisuals[index<player.classVisuals.Length?index:0];if(!visual)return;
            Body=visual.GetComponentInChildren<Animator>(true);Configure(Body,PresentationLibrary.Load().medium);
            armsRoot=Instantiate(visual,player.view.transform);armsRoot.name="First person arms — "+player.State.Class;armsRoot.SetActive(true);
            armsRoot.transform.localPosition=new Vector3(0,-.65f,.55f);armsRoot.transform.localRotation=Quaternion.identity;
            armsRoot.transform.localScale=Vector3.one*.55f;
            foreach(var renderer in armsRoot.GetComponentsInChildren<Renderer>(true)){
                renderer.enabled=renderer.name.IndexOf("Arm",StringComparison.OrdinalIgnoreCase)>=0;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Arms=armsRoot.GetComponentInChildren<Animator>(true);Configure(Arms,PresentationLibrary.Load().medium);
            WeaponSocket=Bone(armsRoot.transform,"handslot.r");previousWeapon=(WeaponKind)(-1);AttachPlayerWeapons();
        }
        void DetachWeapons(){var combat=player.GetComponent<PlayerCombat>();if(combat&&combat.weaponModels!=null)foreach(var model in combat.weaponModels)if(model)model.transform.SetParent(player.view.transform,true);}
        void AttachPlayerWeapons()
        {
            var combat=player.GetComponent<PlayerCombat>();if(!WeaponSocket||!combat||combat.weaponModels==null)return;
            for(int i=0;i<combat.weaponModels.Length;i++){
                var model=combat.weaponModels[i];if(!model)continue;
                var hand=i==(int)WeaponKind.Bow||i==(int)WeaponKind.Shield?Bone(armsRoot.transform,"handslot.l"):WeaponSocket;
                model.transform.SetParent(hand,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
                // Prefab wrappers were centred for a floating camera prop. Restore the model's grip origin.
                if(!weaponsCalibrated&&i!=(int)WeaponKind.Bow&&model.transform.childCount>0){var mesh=model.transform.GetChild(0);mesh.localPosition=i==(int)WeaponKind.Shield?Vector3.zero:mesh.localPosition-Vector3.up*.1f;}
                // Dagger1_3_5 has its guard near the origin and the grip along +Y.
                // Centre that grip in the palm, independently of the generic bottom alignment.
                if(i==(int)WeaponKind.Dagger&&model.transform.childCount>0){var mesh=model.transform.GetChild(0);mesh.localPosition=-Vector3.up*(.08f*mesh.localScale.y);}
                model.transform.localScale=Vector3.one/Mathf.Max(.01f,hand.lossyScale.x);
                if(i==(int)WeaponKind.Musket)model.transform.localRotation=Quaternion.Euler(90,0,0);
                // Dagger1_3_5 points down its local Y axis, opposite to the stab clip.
                if(i==(int)WeaponKind.Dagger)model.transform.localRotation=Quaternion.Euler(0,0,180);
                // Blink blades face local X, while the grip animation expects local Z.
                if(i==(int)WeaponKind.Axe||i==(int)WeaponKind.Scythe)model.transform.localRotation=Quaternion.Euler(0,-90,0);
            }
            weaponsCalibrated=true;
        }
        void LateUpdate()
        {
            if(!player||!player.view||Codice.Multiplayer.LanAvatar.IsRemote(player))return;
            var combat=player.GetComponent<PlayerCombat>();
            if(!combat||combat.weaponModels==null||combat.weaponModels.Length<3)return;
            var bow=combat.weaponModels[(int)WeaponKind.Bow];
            bool throwing=player.State.ActiveWeapon?.shuriken==true;
            bool drawing=player.State.IsAlive&&player.State.ActiveWeapon?.kind==WeaponKind.Bow&&!throwing;
            if(throwing&&bow)bow.SetActive(false);
            if(!heldStar){var star=Resources.Load<GameObject>("Shuriken");if(star){heldStar=Instantiate(star,WeaponSocket);heldStar.transform.localPosition=Vector3.zero;heldStar.transform.localScale=Vector3.one/Mathf.Max(.01f,WeaponSocket.lossyScale.x);}}
            if(heldStar)heldStar.SetActive(throwing&&player.State.IsAlive&&(Time.time<throwRelease||Time.time>=throwUntil));
            bool dual=player.State.DualWield&&!combat.Blocking&&player.State.IsAlive;
            if(dual&&(!offhand||offhandKind!=player.State.ActiveWeapon.kind||offhandTier!=player.State.GetWeapon(1-player.State.ActiveSlot).tier)){if(offhand)Destroy(offhand);offhandKind=player.State.ActiveWeapon.kind;offhand=Instantiate(combat.weaponModels[(int)offhandKind],Bone(armsRoot.transform,"handslot.l"));offhandTier=player.State.GetWeapon(1-player.State.ActiveSlot).tier;WeaponVisualCatalog.Apply(offhand,player.State.GetWeapon(1-player.State.ActiveSlot));offhand.name="Dual offhand";offhand.transform.localPosition=Vector3.zero;offhandRest=combat.weaponModels[(int)offhandKind].transform.localRotation;offhand.transform.localRotation=offhandRest;}
            if(offhand){offhand.SetActive(dual);if(dual)offhand.transform.localRotation=offhandRest;}
            var view=player.view.transform;
            if(dual||dualWasVisible){
                var primary=combat.weaponModels[(int)player.State.ActiveWeapon.kind];
                if(primary)primary.transform.localRotation=Quaternion.identity;
                if(offhand)offhand.transform.localRotation=Quaternion.identity;
                if(dual&&!combat.IsSwinging&&Time.time>=lockedUntil){
                    // Both grips stay on the animated hands; blades rest apart and up.
                    if(primary)primary.transform.rotation=Quaternion.LookRotation(view.forward,(view.up+view.right*.25f).normalized);
                    if(offhand)offhand.transform.rotation=Quaternion.LookRotation(view.forward,(view.up-view.right*.25f).normalized);
                }
            }
            dualWasVisible=dual;
            // The catalog standardizes barrel direction to +Y. Map it to camera +Z,
            // independently of the imported hand bone's rotation.
            if(player.State.ActiveWeapon?.kind==WeaponKind.Musket){
                var musket=combat.weaponModels[(int)WeaponKind.Musket];
                if(musket){float recoil=Mathf.Exp(-Mathf.Max(0,Time.time-musketShotAt)*16);
                    musket.transform.position=view.TransformPoint(new Vector3(.23f,-.24f,.48f-recoil*.10f));
                    musket.transform.rotation=view.rotation*Quaternion.Euler(90-recoil*5,0,0);
                }
            }
            float elapsed=Time.time-bowShotAt;
            float release=Mathf.Clamp01(elapsed/Mathf.Max(.1f,bowCycle));
            float kick=elapsed<bowCycle?Mathf.Sin(release*Mathf.PI)*.065f:0;
            if(bow&&drawing)
            {
                // A short recoil and return accompany the release, instead of freezing
                // the prop in exactly the same camera pose throughout the animation.
                float sway=Mathf.Sin(Time.time*2)*.004f;
                bow.transform.position=view.TransformPoint(new Vector3(-.28f+sway,-.22f-kick*.35f,.7f-kick));
                bow.transform.rotation=view.rotation*Quaternion.Euler(kick*80,0,-kick*100);
            }
            if(!nockedArrow)
            {
                var prefab=Resources.Load<GameObject>("PlayerArrow");
                if(prefab){nockedArrow=Instantiate(prefab,view);nockedArrow.name="Nocked arrow";}
            }
            if(nockedArrow){
                bool visible=drawing&&player.State.ActiveWeapon.ammunition>0&&elapsed>=bowCycle*.55f;
                nockedArrow.SetActive(visible);
                if(visible){float draw=elapsed<bowCycle?Mathf.SmoothStep(0,1,(release-.55f)/.45f):1;
                    nockedArrow.transform.position=bow.transform.position+view.right*.055f+view.up*.035f+view.forward*(.18f-.12f*draw);
                    nockedArrow.transform.rotation=view.rotation;
                }
            }
        }
        void AttachBossWeapons()
        {
            var right=Bone(Body.transform,"hand_r");var left=Bone(Body.transform,"hand_l");
            foreach(var t in Body.GetComponentsInChildren<Transform>(true))if(t.name=="Puglin_Stick"||t.name=="Imp_Mace")t.gameObject.SetActive(false);
            Attach(boss.meleeWeapon,right);Attach(boss.bowWeapon,left);
        }
        static void Attach(GameObject item,Transform hand){if(!item||!hand)return;var scale=item.transform.lossyScale;item.transform.SetParent(hand,false);item.transform.localPosition=Vector3.zero;item.transform.localRotation=Quaternion.identity;item.transform.localScale=scale/Mathf.Max(.01f,hand.lossyScale.x);}
        void Update()
        {
            if(dead&&player&&player.State.IsAlive){dead=false;oldHealth=player.State.Health;lockedUntil=0;CurrentState="";if(armsRoot)armsRoot.SetActive(true);}
            if(dead)return;
            if(player&&!Codice.Multiplayer.LanAvatar.IsRemote(player))SetupPlayer();
            bool alive=player?player.State.IsAlive:enemy?enemy.IsAlive:boss&&boss.IsAlive;
            float health=player?player.State.Health:enemy?enemy.Health:boss?boss.Health:oldHealth;
            if(!alive){Die();return;}
            bool paused=Time.timeScale==0||(enemy&&enemy.Paused)||(boss&&boss.Paused)||(player&&player.ControlsBlocked);
            if(Body)Body.speed=paused?0:1;if(Arms)Arms.speed=paused?0:1;
            if(health<oldHealth&&!(player&&player.GetComponent<PlayerCombat>().IsSwinging&&!player.IsStaggered))Hit();oldHealth=health;
            float distance=Vector3.ProjectOnPlane(transform.position-previous,Vector3.up).magnitude;previous=transform.position;
            if(paused)return;
            float speed=Time.deltaTime>0?distance/Time.deltaTime:0;
            if(distance<2&&speed>.4f){stepDistance+=distance;if(stepDistance>1.7f){stepDistance=0;PresentationVfx.Footstep(transform.position);}}
            if(player&&player.State.ActiveWeapon!=null&&previousWeapon!=player.State.ActiveWeapon.kind){previousWeapon=player.State.ActiveWeapon.kind;if(!player.GetComponent<PlayerCombat>().IsSwinging){lockedUntil=0;CurrentState="";}}
            if(player&&player.GetComponent<ShinobiGuard>()?.Guarding==true){if(Time.time>=lockedUntil)Play("ShinobiGuard");return;}
            if(player&&player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true){Play("BlockIdle");return;}
            if(Time.time<lockedUntil)return;
            string state=speed>.2f?(player?"Movement":enemy&&enemy.IsPatrolling?"Patrol":"Chase"):IdleState();
            if(player&&player.IsDashing)state="Dash";
            Play(state);
        }
        string IdleState(){if(player){if(player.GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShieldActive==true)return "BlockIdle";if(player.State.DualWield)return "DualIdle";if(player.State.ActiveWeapon?.shuriken==true)return "Ready";if(player.State.ActiveWeapon?.kind==WeaponKind.Staff)return "StaffIdle";var k=player.State.ActiveWeapon?.kind;if(k==WeaponKind.Bow)return "BowIdle";if(k==WeaponKind.Musket)return "RifleIdle";if(k==WeaponKind.Shield)return "BlockIdle";return "Ready";}return enemy&&enemy.kind==EnemyKind.Crossbowman?"RifleIdle":enemy&&enemy.kind==EnemyKind.Heavy?"BlockIdle":"Idle";}
        public void Play(string state,float duration=0,bool restart=false)
        {
            if(dead&&state!="Death")return;
            if(duration>0){SetActionSpeed(Body,state,duration);SetActionSpeed(Arms,state,duration);}
            if(CurrentState!=state||restart){CurrentState=state;if(Body&&Body.isActiveAndEnabled)Body.CrossFadeInFixedTime(state,.08f,0);if(Arms&&Arms.isActiveAndEnabled)Arms.CrossFadeInFixedTime(state,.06f,0);}
            if(duration>0)lockedUntil=Time.time+duration;
        }
        static void SetActionSpeed(Animator a,string state,float duration){if(!a||!a.runtimeAnimatorController)return;var clip=a.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==state) ?? a.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.EndsWith(state,StringComparison.Ordinal));a.SetFloat("ActionSpeed",clip?clip.length/Mathf.Max(.1f,duration):1);}
        public void Attack(bool ranged,float duration=.5f)
        {
            var weapon=GetComponent<PlayerCharacter>()?.State.ActiveWeapon;
            var cue=weapon!=null?(weapon.kind==WeaponKind.Bow?SoundCue.Bow:weapon.kind==WeaponKind.Musket?SoundCue.Gun:weapon.kind==WeaponKind.Staff?SoundCue.Magic:weapon.kind==WeaponKind.Hammer||weapon.kind==WeaponKind.Axe?SoundCue.Heavy:SoundCue.Swing):GetComponent<BossCharacter>()?SoundCue.Boss:ranged?SoundCue.Bow:SoundCue.Swing;
            if(weapon!=null)GameAudio.Play(cue);else GameAudio.PlayAt(cue,transform.position);
            if(player&&weapon!=null){if(weapon.kind==WeaponKind.Bow&&!weapon.shuriken){bowShotAt=Time.time;bowCycle=Mathf.Max(.2f,duration);}if(weapon.kind==WeaponKind.Musket)musketShotAt=Time.time;}
            string state=ranged?"Shoot":"Melee";
            if(boss&&boss.useNativeAnimator&&boss.CurrentAttack==BossAttack.Slam&&Body&&Body.HasState(0,Animator.StringToHash("Slam")))state="Slam";
            if(player){var k=player.State.ActiveWeapon?.kind;if(k==WeaponKind.Bow)state="ShootBow";else if(k==WeaponKind.Staff)state="ShootMagic";else if(k==WeaponKind.Hammer||k==WeaponKind.Axe||k==WeaponKind.Scythe)state="HeavyAttack";else if(k==WeaponKind.Polearm||k==WeaponKind.Dagger)state="Thrust";}
            if(player&&player.State.ActiveWeapon?.shuriken==true){state="ThrowShuriken";throwRelease=Time.time+.12f;throwUntil=Time.time+duration*.85f;}
            if(player&&player.State.ActiveWeapon?.kind==WeaponKind.Staff)state="StaffCast";
            if(player&&player.State.ActiveWeapon?.katana==true)state=(katanaSlash++%2==0)?"KatanaSlashA":"KatanaSlashB";
            if(player&&player.State.DualWield)state=weapon.kind==WeaponKind.Dagger?"DualStab":"DualSlice";
            if(enemy&&enemy.kind==EnemyKind.Mage)state="ShootMagic";
            if(enemy&&enemy.kind==EnemyKind.Heavy)state="HeavyAttack";
            Play(state,Mathf.Max(.15f,duration),true);
        }
        public void Hit(){Play("Hit",.25f,true);}
        public void Die()
        {
            if(dead)return;dead=true;Play("Death",4,true);
            GameAudio.PlayAt(SoundCue.Death,transform.position);
            if(player){if(armsRoot)armsRoot.SetActive(false);return;}
            if(!Body)return;
            // Visual-only corpse lets the existing combat system remove the actor immediately.
            var corpse=Instantiate(Body.gameObject,Body.transform.position,Body.transform.rotation);corpse.name="Animated remains";corpse.transform.localScale=Body.transform.lossyScale;
            foreach(var c in corpse.GetComponentsInChildren<Collider>())Destroy(c);
            var animator=corpse.GetComponent<Animator>();animator.speed=1;animator.Play("Death",0,0);if(enemy&&enemy.miniboss){animator.enabled=false;corpse.AddComponent<CorpseCollapse>();}Destroy(corpse,3.5f);
        }
    }
}
