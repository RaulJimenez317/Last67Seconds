using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Codice.Multiplayer;
using Codice.Characters;
using Codice.Mechanics;
using Codice.UI;
using Object=UnityEngine.Object;
public static class LanClientChecks
{
 const string Report="Library/LanClientChecks.txt";static string error;static LanWire server;static LanMessage world;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Report,"PASS "+message+"\n");}
 static void Log(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){error=condition;File.AppendAllText(Report,"RUNTIME "+condition+"\n"+trace+"\n");}}
 static float next;
 static void Pump(){EditorApplication.QueuePlayerLoopUpdate();if(server!=null&&world!=null&&Time.realtimeSinceStartup>next){next=Time.realtimeSinceStartup+.1f;server.Send(JsonUtility.ToJson(world));while(server.TryRead(out _)){}}}
 static async Task Until(Func<bool> test){double end=EditorApplication.timeSinceStartup+50;while(!test()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(30);}}
 [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("LanClientChecks",false)){SessionState.SetBool("LanClientChecks",false);Run();}};EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/LanClientChecks.request"))return;File.Delete("Library/LanClientChecks.request");File.WriteAllText(Report,"");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.AppendAllText(Report,"Save scene first");return;}UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("LanClientChecks",true);EditorApplication.isPlaying=true;};}
 static async void Run(){error=null;world=null;server=null;next=0;TcpListener listener=null;Application.logMessageReceived+=Log;EditorApplication.update+=Pump;try{
  Application.runInBackground=true;await Until(()=>Object.FindFirstObjectByType<MainMenuUI>());listener=new TcpListener(IPAddress.Loopback,LanWire.Port);listener.Start();LanSession.Open();typeof(LanSession).GetField("address",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(LanSession.Instance,"127.0.0.1");typeof(LanSession).GetField("chosenClass",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(LanSession.Instance,3);LanSession.Instance.Join();var socket=await listener.AcceptTcpClientAsync();server=new LanWire(socket);await Until(()=>LanSession.Client);await Task.Delay(100);
  var state=new CharacterState();state.Initialize(PlayerClass.Mage);state.AddGold(250);
  var members=new[]{new LanMember{id=0,name="Host",playerClass=0},new LanMember{id=1,name="Invitado",playerClass=3}};
  server.Send(JsonUtility.ToJson(new LanMessage{type="lobby",id=1,members=members}));
  world=new LanMessage{type="world",id=1,scene="Calabozo",level=1,phase=(int)RunPhase.Combat,total=5,members=members,entities=new[]{
   new LanEntity{id=1,owner=1,key="Player:3",name="Invitado",state=state.Capture(),position=new Vector3(0,1,0),rotation=Quaternion.identity,health=100,maxHealth=100},
   new LanEntity{id=0,owner=0,key="Player:0",name="Host",position=new Vector3(1,1,2),rotation=Quaternion.identity,health=120,maxHealth=120,animation="Idle"},
   new LanEntity{id=100,key="Enemy:Swordsman:Baba de mazmorra",name="Slime",position=new Vector3(0,1,3),rotation=Quaternion.identity,health=65,maxHealth=65,animation="Idle"},
   new LanEntity{id=101,key="Boss:OrkBerserker",name="Ork",position=new Vector3(-3,1,4),rotation=Quaternion.identity,health=1000,maxHealth=1000,animation="Idle"},
   new LanEntity{id=102,key="Weapon",weapon=new WeaponInstance(WeaponKind.Bow),position=new Vector3(2,1,3),rotation=Quaternion.identity}}};
  await Until(()=>RunDirector.Instance&&RunDirector.Instance.Player&&RunHUD.Instance&&RunDirector.Instance.Level==1);await Task.Delay(800);
  var player=RunDirector.Instance.Player;player.enabled=false;
  Check(player.State.Class==PlayerClass.Mage&&player.State.Gold==250,"client receives own class and inventory");Check(Object.FindObjectsByType<LanGhost>(FindObjectsSortMode.None).Length==4,"client displays host, enemies, boss and loot");
  Check(Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Length==0,"client does not run duplicate enemy AI");
  var remote=Object.FindObjectsByType<LanGhost>(FindObjectsSortMode.None).First(g=>g.snapshot.id==100);Check(remote.GetComponentsInChildren<Renderer>().Length>0,"network enemy visual exists");
  world.level=2;world.defeated=3;world.entities[2].health=30;await Task.Delay(250);Check(RunDirector.Instance.Level==2&&RunHUD.Instance.Context.dungeon.Defeated==3&&remote.snapshot.health==30,"round and health synchronization");
  world.shop=true;world.customer=1;world.offers=new[]{new MerchantOffer{kind=OfferKind.Healing,price=80}};await Task.Delay(250);Check(RunHUD.Instance.Context.dungeon.merchant.Customer==player&&player.ControlsBlocked,"network merchant opens for correct client");
  world.shop=false;world.customer=-1;await Task.Delay(200);Check(!player.ControlsBlocked,"network merchant closes cleanly");
  server.Send(JsonUtility.ToJson(new LanMessage{type="effect",name="Spell:Heal",scene="Calabozo",point=player.transform.position,scale=.35f,lifetime=2}));await Task.Delay(150);Check(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.transform.root.name=="Heal(Clone)"),"spell VFX relayed to client");
  world.scene="Coliseo";world.level=6;await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Coliseo"&&RunDirector.Instance&&RunDirector.Instance.Level==6);await Task.Delay(300);Check(RunDirector.Instance.Player.State.Gold==250,"inventory preserved across shared map transition");
  Check(string.IsNullOrEmpty(error),"no client runtime exceptions");File.AppendAllText(Report,"LAN_CLIENT_CHECKS_PASSED\n");
 }catch(Exception e){File.AppendAllText(Report,"FAILED "+e);Debug.LogException(e);}finally{world=null;server?.Dispose();server=null;listener?.Stop();if(LanSession.Instance)LanSession.Instance.Leave();Application.logMessageReceived-=Log;EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}}
}
