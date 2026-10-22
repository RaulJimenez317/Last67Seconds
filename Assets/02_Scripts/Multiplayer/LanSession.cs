using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Codice.Multiplayer
{
    [DefaultExecutionOrder(-150)]
    public sealed class LanSession : MonoBehaviour
    {
        public static LanSession Instance {get;private set;}
        public static bool Active=>Instance;
        public static bool Host=>Instance&&Instance.host;
        public static bool Client=>Instance&&Instance.client;
        const string Protocol="CodiceLAN-1";
        sealed class Peer
        {
            public LanWire wire; public int id,playerClass; public string name="Invitado",saved;
            public bool accepted;public float lastSeen,joined;public PlayerCharacter player;public LanMessage input;
        }
        readonly Dictionary<UnityEngine.Object,int> objectIds=new Dictionary<UnityEngine.Object,int>();int nextObjectId=100;
        int Id(UnityEngine.Object value){if(!objectIds.TryGetValue(value,out int id)){id=nextObjectId++;objectIds[value]=id;}return id;}
        readonly List<Peer> peers=new List<Peer>();
        readonly Dictionary<int,LanGhost> ghosts=new Dictionary<int,LanGhost>();
        TcpListener listener;LanWire server;LanCatalog catalog;RunScene context;
        bool host,client,connecting,started,leaving;int nextId=1,localId=-1,chosenClass;
        string address="192.168.1.10",nickname="Jugador",status="",loadedScene="",localSaved;
        float heartbeat,nextSend,lastServer,clientYaw,clientPitch;
        LanMember[] members=Array.Empty<LanMember>();
        LanMessage pending;
        public bool AnyAlive=>Players().Any(p=>p&&p.State.IsAlive);
        public static void Open()
        {if(Instance)return;new GameObject("Partida LAN").AddComponent<LanSession>();}
        void Awake(){if(Instance){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;catalog=Resources.Load<LanCatalog>("LanCatalog");}
        void OnDestroy(){CloseSockets();if(Instance==this)Instance=null;}
        void OnApplicationQuit(){CloseSockets();}
        void CloseSockets(){listener?.Stop();listener=null;server?.Dispose();server=null;foreach(var peer in peers)peer.wire.Dispose();}
        public void Leave(){leaving=true;CloseSockets();host=client=false;Destroy(gameObject);}
        public void CreateHost()
        {
            try{listener=new TcpListener(IPAddress.Any,LanWire.Port);listener.Start(4);host=true;localId=0;status="Comparte tu IP local y espera a tus compañeros.";RefreshMembers();}
            catch(Exception e){status="No se pudo crear la partida: "+e.Message;listener?.Stop();listener=null;}
        }
        public async void Join()
        {
            if(connecting)return;connecting=true;status="Conectando…";
            try{var connection=await LanWire.Connect(address.Trim());if(!this||leaving){connection.Dispose();return;}server=connection;client=true;lastServer=Time.realtimeSinceStartup;server.Send(JsonUtility.ToJson(new LanMessage{type="hello",version=Protocol,playerClass=chosenClass,name=nickname}));status="Esperando al anfitrión…";}
            catch(Exception e){if(this)status=e.Message;}
            finally{if(this)connecting=false;}
        }
        public void StartMatch()
        {
            if(!host||started||!catalog)return;started=true;RunNavigation.StartGame((PlayerClass)chosenClass,1);
        }
        public IEnumerable<PlayerCharacter> Players()
        {
            if(context&&context.player)yield return context.player;
            foreach(var peer in peers)if(peer.accepted&&peer.player)yield return peer.player;
        }
        public PlayerCharacter NearestPlayer(Vector3 position)=>Players().Where(p=>p.State.IsAlive).OrderBy(p=>(p.transform.position-position).sqrMagnitude).FirstOrDefault();
        public void ReviveTeam()
        {
            if(!context)return;
            foreach(var p in Players())if(!p.State.IsAlive){var saved=p.State.Capture();p.RestartAfterDefeat(context.player.transform.position+Vector3.right*1.5f,Quaternion.identity);p.State.Restore(saved);p.State.NetworkRevive();}
        }
        public void SaveTeam(){foreach(var peer in peers)if(peer.player)peer.saved=peer.player.State.Capture();}
        public void ResetTeam(){foreach(var peer in peers){peer.saved=null;if(peer.player)peer.player.State.Initialize((PlayerClass)peer.playerClass);}}
        public void EnterHost(RunScene scene)
        {
            context=scene;loadedScene=scene.gameObject.scene.name;
            foreach(var peer in peers.Where(p=>p.accepted))SpawnPeer(peer);
        }
        void SpawnPeer(Peer peer)
        {
            if(peer.player||!context)return;
            var holder=new GameObject("Remote player construction");holder.SetActive(false);
            var clone=Instantiate(context.player.gameObject,holder.transform);clone.name="LAN • "+peer.name;
            var player=clone.GetComponent<PlayerCharacter>();var avatar=clone.GetComponent<LanAvatar>()??clone.AddComponent<LanAvatar>();avatar.remote=true;avatar.id=peer.id;avatar.playerClass=(PlayerClass)peer.playerClass;
            foreach(var camera in clone.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.tag="Untagged";}
            foreach(var audio in clone.GetComponentsInChildren<AudioListener>(true))audio.enabled=false;
            foreach(var child in clone.GetComponentsInChildren<Transform>(true))if(child.name.StartsWith("First person arms"))Destroy(child.gameObject);
            var spawn=context.player.transform.position+Vector3.right*(peer.id*1.2f);if(NavMesh.SamplePosition(spawn,out var nav,4,NavMesh.AllAreas))spawn=nav.position;clone.transform.position=spawn;
            clone.transform.SetParent(null,true);Destroy(holder);peer.player=player;
            if(!string.IsNullOrEmpty(peer.saved))player.RestoreRunState(peer.saved);if(!player.State.IsAlive)player.State.NetworkRevive();
            foreach(var weapon in player.GetComponent<PlayerCombat>().weaponModels)if(weapon)foreach(var renderer in weapon.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            var equipment=clone.AddComponent<LanHeldWeapon>();equipment.player=player;
            player.State.Died+=()=>RunDirector.Instance?.CheckTeamDefeat();
        }
        public void EnterClient(RunScene scene)
        {
            context=scene;loadedScene=scene.gameObject.scene.name;ghosts.Clear();localSaved=null;
            foreach(var enemy in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            foreach(var boss in FindObjectsByType<BossCharacter>(FindObjectsSortMode.None))Destroy(boss.gameObject);
            foreach(var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))Destroy(pickup.gameObject);
            foreach(var pickup in FindObjectsByType<LootPickup>(FindObjectsSortMode.None))Destroy(pickup.gameObject);
            var avatar=scene.player.GetComponent<LanAvatar>()??scene.player.gameObject.AddComponent<LanAvatar>();avatar.playerClass=(PlayerClass)chosenClass;
            if(!RunDirector.Instance)new GameObject("LAN round display").AddComponent<RunDirector>();
            RunHUD.Install(scene);clientYaw=scene.player.transform.eulerAngles.y;clientPitch=0;
            if(pending!=null)Apply(pending);
        }
        public void EmitEffect(string key,Vector3 point,float scale,float lifetime)
        {if(!host)return;var json=JsonUtility.ToJson(new LanMessage{type="effect",name=key,scene=loadedScene,point=point,scale=scale,lifetime=lifetime});foreach(var peer in peers.Where(p=>p.accepted))peer.wire.Send(json);}
        void ReceiveEffect(LanMessage msg)
        {
            if(!context||msg.scene!=loadedScene)return;var prefab=msg.name.StartsWith("Spell:")?Resources.Load<GameObject>("SpellEffects/"+msg.name.Substring(6)):catalog.Find("Vfx:"+msg.name);
            if(!prefab)return;var effect=Instantiate(prefab,msg.point,Quaternion.identity);effect.transform.localScale*=msg.scale;Destroy(effect,Mathf.Clamp(msg.lifetime,.05f,5));
        }
        void RefreshMembers(){members=new[]{new LanMember{id=0,name=nickname,playerClass=chosenClass}}.Concat(peers.Where(p=>p.accepted).Select(p=>new LanMember{id=p.id,name=p.name,playerClass=p.playerClass})).ToArray();}
        void Update()
        {
            if(host)
            {
                while(listener!=null&&listener.Pending())
                {
                    var socket=listener.AcceptTcpClient();
                    if(started||peers.Count>=3){socket.Close();continue;}
                    peers.Add(new Peer{wire=new LanWire(socket),id=nextId++,joined=Time.realtimeSinceStartup,lastSeen=Time.realtimeSinceStartup});
                }
                for(int i=peers.Count-1;i>=0;i--)
                {
                    var peer=peers[i];int handled=0;
                    while(handled++<32&&peer.wire.TryRead(out var json))
                    {
                        try{var msg=JsonUtility.FromJson<LanMessage>(json);Handle(peer,msg);peer.lastSeen=Time.realtimeSinceStartup;}
                        catch{peer.wire.Dispose();}
                    }
                    if(peer.player&&Time.realtimeSinceStartup-peer.lastSeen>.3f){var avatar=peer.player.GetComponent<LanAvatar>();avatar.move=Vector2.zero;avatar.attack=avatar.block=false;}
                    if(peer.wire.Closed||Time.realtimeSinceStartup-peer.lastSeen>45||!peer.accepted&&Time.realtimeSinceStartup-peer.joined>5)
                    {peer.wire.Dispose();if(peer.player)Destroy(peer.player.gameObject);peers.RemoveAt(i);RefreshMembers();RunDirector.Instance?.CheckTeamDefeat();status="Un compañero se desconectó.";}
                }
                if(Time.realtimeSinceStartup>=nextSend){nextSend=Time.realtimeSinceStartup+.08f;Broadcast();}
            }
            else if(client&&server!=null)
            {
                int handled=0;while(handled++<32&&server.TryRead(out var json))
                {
                    try{var msg=JsonUtility.FromJson<LanMessage>(json);lastServer=Time.realtimeSinceStartup;if(msg.type=="lobby"){localId=msg.id;members=msg.members;status="Esperando a que el anfitrión inicie.";}else if(msg.type=="world"){localId=msg.id;pending=msg;Apply(msg);}else if(msg.type=="effect")ReceiveEffect(msg);}
                    catch(Exception e){Debug.LogWarning("LAN: "+e.Message);Disconnect("Datos de partida incompatibles.");break;}
                }
                if(server!=null&&(server.Closed||Time.realtimeSinceStartup-lastServer>45))Disconnect("Se perdió la conexión con el anfitrión.");
                if(server!=null&&Time.realtimeSinceStartup>=heartbeat){heartbeat=Time.realtimeSinceStartup+1;Send(new LanMessage{type="ping"});}
            }
        }
        void Disconnect(string message){status=message;client=false;server?.Dispose();server=null;started=false;context=null;pending=null;if(SceneManager.GetActiveScene().name!="MainMenu")RunNavigation.MainMenu();}
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        void Handle(Peer peer,LanMessage msg)
        {
            if(!peer.accepted)
            {
                if(msg.type!="hello"||msg.version!=Protocol||msg.playerClass<0||msg.playerClass>4){peer.wire.Dispose();return;}
                peer.accepted=true;peer.playerClass=msg.playerClass;peer.name=new string((msg.name??"Invitado").Where(c=>char.IsLetterOrDigit(c)||c==' ').Take(20).ToArray());RefreshMembers();return;
            }
            if(msg.type!="input"||!peer.player||msg.scene!=loadedScene)return;
            if(!Finite(msg.yaw)||!Finite(msg.pitch)||!Finite(msg.move.x)||!Finite(msg.move.y)){peer.wire.Dispose();return;}
            var p=peer.player;var avatar=p.GetComponent<LanAvatar>();avatar.move=Vector2.ClampMagnitude(msg.move,1);avatar.yaw=msg.yaw%360;avatar.pitch=Mathf.Clamp(msg.pitch,-85,85);avatar.attack=msg.attack;avatar.block=msg.block;avatar.jump|=msg.jump;avatar.dash|=msg.dash;
            if(msg.slot>=0&&msg.slot<2)p.State.SelectWeapon(msg.slot);if(msg.spell>=0&&msg.spell<4)p.State.SelectSpell(msg.spell);
            if(msg.special)p.GetComponent<PlayerMechanics>().RequestUltimate();if(msg.reload)p.GetComponent<PlayerCombat>().TryReload();
            if(msg.passive>=0&&msg.passive<3)p.TryActivatePassive(msg.passive);
            var merchant=context.dungeon?context.dungeon.merchant:null;
            if(msg.interact)p.TryInteract();
            if(merchant&&merchant.Customer==p){if(msg.buy>=0&&msg.buy<4)merchant.TryPurchase(p,msg.buy);if(msg.closeShop)merchant.CloseShop();}
            // Only the host advances the shared round/portal. Guests can still shop.
        }
        void Send(LanMessage msg){if(server!=null)server.Send(JsonUtility.ToJson(msg));}
        public void ShopCommand(int buy=-1,bool close=false){Send(new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch,buy=buy,closeShop=close});}
        public void ClientControls(PlayerCharacter p)
        {
            if(!client||server==null||server.Closed||!context)return;
            var keyboard=Keyboard.current;var mouse=Mouse.current;if(keyboard==null||mouse==null)return;
            var avatar=p.GetComponent<LanAvatar>();if(!avatar)return;
            bool shopping=context.dungeon&&context.dungeon.merchant&&context.dungeon.merchant.Customer==p;
            bool allowed=p.State.IsAlive&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)&&Cursor.lockState==CursorLockMode.Locked;
            if(!allowed&&mouse.leftButton.wasPressedThisFrame&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(allowed){var look=mouse.delta.ReadValue()*p.mouseSensitivity;clientYaw+=look.x;clientPitch=Mathf.Clamp(clientPitch-look.y,-85,85);}
            var msg=new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch};
            msg.move=allowed?new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0)).normalized:Vector2.zero;
            msg.attack=allowed&&mouse.leftButton.isPressed;msg.block=allowed&&mouse.rightButton.isPressed;
            msg.jump=allowed&&keyboard.spaceKey.wasPressedThisFrame;msg.dash=allowed&&keyboard.leftShiftKey.wasPressedThisFrame;msg.special=allowed&&keyboard.qKey.wasPressedThisFrame;msg.reload=allowed&&keyboard.rKey.wasPressedThisFrame;msg.interact=allowed&&keyboard.eKey.wasPressedThisFrame;
            if(allowed){int digit=keyboard.digit1Key.wasPressedThisFrame?0:keyboard.digit2Key.wasPressedThisFrame?1:keyboard.digit3Key.wasPressedThisFrame?2:keyboard.digit4Key.wasPressedThisFrame?3:-1;if(p.State.ActiveWeapon?.kind==WeaponKind.Staff)msg.spell=digit;else if(digit<2)msg.slot=digit;if(keyboard.tabKey.wasPressedThisFrame||mouse.scroll.ReadValue().y!=0)msg.slot=1-p.State.ActiveSlot;msg.passive=keyboard.tKey.wasPressedThisFrame?0:keyboard.yKey.wasPressedThisFrame?1:keyboard.uKey.wasPressedThisFrame?2:-1;}
            if(shopping){msg.buy=keyboard.digit1Key.wasPressedThisFrame?0:keyboard.digit2Key.wasPressedThisFrame?1:keyboard.digit3Key.wasPressedThisFrame?2:keyboard.digit4Key.wasPressedThisFrame?3:-1;msg.closeShop=keyboard.eKey.wasPressedThisFrame||keyboard.escapeKey.wasPressedThisFrame;}
            avatar.move=msg.move;avatar.yaw=clientYaw;avatar.pitch=clientPitch;avatar.jump=msg.jump;avatar.dash=msg.dash;avatar.Motor(p,Time.deltaTime);
            // Send edge-triggered actions immediately; continuous input at 30 Hz.
            if(Time.realtimeSinceStartup>=nextSend||msg.jump||msg.dash||msg.special||msg.reload||msg.interact||msg.slot>=0||msg.spell>=0||msg.passive>=0||msg.buy>=0||msg.closeShop){nextSend=Time.realtimeSinceStartup+1f/30;Send(msg);}
        }
        void Broadcast()
        {
            if(!started||!context||!RunDirector.Instance){foreach(var p in peers.Where(p=>p.accepted))p.wire.Send(JsonUtility.ToJson(new LanMessage{type="lobby",id=p.id,members=members}));return;}
            var director=RunDirector.Instance;var entities=new List<LanEntity>();
            foreach(var p in Players())
            {
                int owner=p==context.player?0:p.GetComponent<LanAvatar>().id;string saved=p.State.Capture();var peer=peers.FirstOrDefault(x=>x.id==owner);if(peer!=null)peer.saved=saved;
                entities.Add(new LanEntity{id=owner,owner=owner,key="Player:"+(int)p.State.Class,position=p.transform.position,rotation=p.transform.rotation,state=saved,weapon=p.State.ActiveWeapon,health=p.State.Health,maxHealth=p.State.MaxHealth,name=members.FirstOrDefault(m=>m.id==owner)?.name??"Jugador",animation=p.GetComponent<ActorPresentation>()?.CurrentState});
            }
            foreach(var e in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(e),key="Enemy:"+e.kind+":"+e.variantName,position=e.transform.position,rotation=e.transform.rotation,health=e.Health,maxHealth=e.MaxHealth,name=e.variantName??e.kind.ToString(),animation=e.GetComponent<ActorPresentation>()?.CurrentState});
            foreach(var b in FindObjectsByType<BossCharacter>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(b),key="Boss:"+b.kind,position=b.transform.position,rotation=b.transform.rotation,health=b.Health,maxHealth=b.MaxHealth,name=b.kind.ToString(),animation=b.GetComponent<ActorPresentation>()?.CurrentState});
            foreach(var w in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(w),key="Weapon",position=w.transform.position,rotation=w.transform.rotation,weapon=w.weapon});
            foreach(var l in FindObjectsByType<LootPickup>(FindObjectsSortMode.None))if(l.Available)entities.Add(new LanEntity{id=Id(l),key="Loot",position=l.transform.position,rotation=l.transform.rotation,kind=(int)l.kind,passive=(int)l.passive,amount=l.amount});
            foreach(var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(shot),key="Shot",position=shot.transform.position,rotation=shot.transform.rotation});
            foreach(var coin in FindObjectsByType<GoldPickup>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(coin),key="Gold",position=coin.transform.position,rotation=coin.transform.rotation});
            foreach(var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(chest),key="Chest",position=chest.transform.position,available=chest.Opened});
            foreach(var shot in FindObjectsByType<PlayerProjectile>(FindObjectsSortMode.None))entities.Add(new LanEntity{id=Id(shot),key="Projectile:"+shot.NetworkVisual,position=shot.transform.position,rotation=shot.transform.rotation});
            foreach(var p in Players())if(p.GetComponent<PlayerMechanics>().UltimateActive&&p.State.ActiveWeapon?.kind==WeaponKind.Staff)entities.Add(new LanEntity{id=Id(p.GetComponent<PlayerMechanics>()),key="Beam",position=p.view.transform.position+p.view.transform.forward*.7f,end=p.view.transform.position+p.view.transform.forward*28});
            var merchant=context.dungeon?context.dungeon.merchant:null;
            var world=new LanMessage{type="world",scene=loadedScene,level=director.Level,phase=(int)director.Phase,elapsed=director.Elapsed,roundTime=director.RoundElapsed,kills=director.TotalKills,entities=entities.ToArray(),members=members,defeated=context.dungeon?context.dungeon.Defeated:0,total=context.dungeon?context.dungeon.Total:0,shop=merchant&&merchant.Available,offers=merchant?Enumerable.Range(0,merchant.OfferCount).Select(merchant.GetOffer).ToArray():null,portal=context.portal&&context.portal.Available,customer=merchant&&merchant.Customer?(merchant.Customer==context.player?0:merchant.Customer.GetComponent<LanAvatar>().id):-1};
            foreach(var peer in peers.Where(p=>p.accepted)){world.id=peer.id;peer.wire.Send(JsonUtility.ToJson(world));}
        }
        void Apply(LanMessage msg)
        {
            if(string.IsNullOrEmpty(msg.scene)||!Application.CanStreamedLevelBeLoaded(msg.scene))throw new InvalidOperationException("Falta mapa de la partida.");
            if(SceneManager.GetActiveScene().name!=msg.scene)
            {
                if(RunNavigation.Loading)return;
                context=null;ghosts.Clear();started=true;RunNavigation.StartGame((PlayerClass)chosenClass,msg.level);return;
            }
            if(!context)return;
            RunDirector.Instance.ApplyLan(context,msg.level,(RunPhase)msg.phase,msg.elapsed,msg.roundTime,msg.kills);
            if(context.dungeon)context.dungeon.ApplyLan(msg.total,msg.defeated);
            if(context.portal)context.portal.SetAvailable(msg.portal);
            var found=new HashSet<int>();
            foreach(var entity in msg.entities??Array.Empty<LanEntity>())
            {
                if(entity.key=="Chest"){var chest=FindObjectsByType<TreasureChest>(FindObjectsSortMode.None).FirstOrDefault(c=>Vector3.Distance(c.transform.position,entity.position)<.1f);if(chest)chest.ApplyLan(entity.available);continue;}
                if(entity.owner==localId)
                {
                    bool first=string.IsNullOrEmpty(localSaved);bool wasAlive=context.player.State.IsAlive;context.player.RestoreRunState(entity.state);localSaved=entity.state;
                    if(first||Vector3.Distance(context.player.transform.position,entity.position)>.65f){var controller=context.player.GetComponent<CharacterController>();controller.enabled=false;context.player.transform.position=entity.position;controller.enabled=true;}
                    if(!wasAlive&&context.player.State.IsAlive)context.player.EndShop();
                    var pose=context.player.GetComponent<ActorPresentation>();if(pose&&entity.animation!=pose.CurrentState&&!string.IsNullOrEmpty(entity.animation))pose.Play(entity.animation,.1f);
                    continue;
                }
                found.Add(entity.id);bool fresh=!ghosts.TryGetValue(entity.id,out var ghost)||!ghost;
                if(!fresh&&entity.key=="Weapon"&&JsonUtility.ToJson(ghost.snapshot.weapon)!=JsonUtility.ToJson(entity.weapon)){Destroy(ghost.gameObject);fresh=true;}
                if(fresh){var go=Visual(entity);ghost=go.AddComponent<LanGhost>();ghosts[entity.id]=ghost;}
                ghost.Apply(entity,fresh);
            }
            foreach(var id in ghosts.Keys.Where(id=>!found.Contains(id)).ToArray()){if(ghosts[id])Destroy(ghosts[id].gameObject);ghosts.Remove(id);}
            if(context.dungeon&&context.dungeon.merchant)context.dungeon.merchant.ApplyLan(msg.shop,msg.offers,msg.customer==localId?context.player:null);
        }
        GameObject Visual(LanEntity entity)
        {
            GameObject prefab=null;
            if(entity.key.StartsWith("Player:"))
            {
                int index=int.Parse(entity.key.Substring(7));var visuals=context.player.classVisuals;
                prefab=index<visuals.Length?visuals[index]:Resources.Load<GameObject>(index==3?"MageVisual":"ShinobiVisual");
            }
            else if(entity.key.StartsWith("Projectile:"))prefab=entity.key=="Projectile:Magic"?PresentationLibrary.Load().magicProjectile:Resources.Load<GameObject>(entity.key=="Projectile:Star"?"Shuriken":"PlayerArrow");
            else if(entity.key=="Weapon")prefab=WeaponVisualCatalog.Load()?.Get(entity.weapon);
            else if(entity.key=="Gold")prefab=Resources.Load<GameObject>("GoldCoin");
            else if(entity.key=="Loot")prefab=entity.kind==(int)LootKind.Arrows?Resources.Load<GameObject>("PlayerArrow"):entity.kind==(int)LootKind.Shuriken?Resources.Load<GameObject>("Shuriken"):PresentationLibrary.Load().ammunitionModel;
            else prefab=catalog?catalog.Find(entity.key):null;
            var holder=new GameObject("Network visual");holder.SetActive(false);
            GameObject go=prefab?Instantiate(prefab,holder.transform):GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(holder.transform,false);go.name=entity.name??entity.key;
            foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))DestroyImmediate(script);
            foreach(var nav in go.GetComponentsInChildren<NavMeshAgent>(true))DestroyImmediate(nav);
            foreach(var body in go.GetComponentsInChildren<Rigidbody>(true))DestroyImmediate(body);
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))DestroyImmediate(collider);
            if(entity.key=="Shot")go.transform.localScale=Vector3.one*.18f;
            if(entity.key=="Beam"){foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.enabled=false;var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Resources.Load<Material>("SpecialGlow");line.startColor=line.endColor=Color.cyan;line.startWidth=line.endWidth=.22f;line.positionCount=2;}
            foreach(var animator in go.GetComponentsInChildren<Animator>(true)){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;if(entity.key.StartsWith("Player:"))animator.runtimeAnimatorController=PresentationLibrary.Load().medium;}
            go.SetActive(true);go.transform.SetParent(null,false);Destroy(holder);return go;
        }
        void OnGUI()
        {
            if(started&&context)
            {
                string text="LAN • "+(host?"Anfitrión":nickname)+" • "+members.Length+"/4";
                if(!context.player.State.IsAlive&&RunDirector.Instance&&RunDirector.Instance.Phase!=RunPhase.Defeated)text+=" • Caído: vuelves en la siguiente ronda";
                GUI.Box(new Rect(Screen.width/2-230,115,460,28),text);return;
            }
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/960f,Screen.height/650f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-900*scale)/2,20,0),Quaternion.identity,Vector3.one*scale);
            GUI.ModalWindow(26767,new Rect(0,0,900,600),id=>
            {
                GUILayout.Space(20);GUILayout.Label("COOPERATIVO EN RED LOCAL • HASTA 4 JUGADORES");GUILayout.Space(12);
                if(!host&&!client&&!connecting)
                {
                    GUILayout.Label("Tu nombre");nickname=GUILayout.TextField(nickname,20);
                    GUILayout.Label("Clase inicial");chosenClass=GUILayout.SelectionGrid(chosenClass,new[]{"Caballero","Tanque","Arquero","Mago","Shinobi"},5);
                    if(GUILayout.Button("CREAR PARTIDA",GUILayout.Height(50)))CreateHost();
                    GUILayout.Label("IP local del anfitrión");address=GUILayout.TextField(address,40);
                    if(GUILayout.Button("UNIRSE",GUILayout.Height(50)))Join();
                }
                else
                {
                    if(host){GUILayout.Label("IP local: "+string.Join(" / ",Dns.GetHostAddresses(Dns.GetHostName()).Where(a=>a.AddressFamily==AddressFamily.InterNetwork).Select(a=>a.ToString())));}
                    foreach(var member in members)GUILayout.Label(member.name+" • "+((PlayerClass)member.playerClass));
                    if(host&&GUILayout.Button("INICIAR PARTIDA",GUILayout.Height(55)))StartMatch();
                }
                GUILayout.Space(15);GUILayout.Label(status);
                GUILayout.Label("Misma versión del juego en todos los equipos. La partida termina si sale el anfitrión.");
                GUILayout.Label("El anfitrión inicia la partida y avanza por los portales. Los caídos vuelven en la siguiente ronda.");
                if(GUILayout.Button("VOLVER",GUILayout.Height(45)))Leave();
            },"PARTIDA LAN");GUI.matrix=old;
        }
    }
}

