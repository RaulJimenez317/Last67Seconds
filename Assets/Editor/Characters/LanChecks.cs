using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Codice.Multiplayer;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using Object=UnityEngine.Object;
public static class LanChecks
{
 const string Report="Library/LanChecks.txt";static readonly List<LanWire> clients=new List<LanWire>();static string error;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Report,"PASS "+message+"\n");}
 static void Log(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){error=condition;File.AppendAllText(Report,"RUNTIME "+condition+"\n"+trace+"\n");}}
 static void Pump(){EditorApplication.QueuePlayerLoopUpdate();foreach(var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))e.SetPaused(true);}
 static async Task Until(Func<bool> test){double end=EditorApplication.timeSinceStartup+50;while(!test()){if(EditorApplication.timeSinceStartup>end)throw new Exception("Timeout");await Task.Delay(30);}}
 [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("LanChecks",false)){SessionState.SetBool("LanChecks",false);Run();}};EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/LanChecks.request"))return;File.Delete("Library/LanChecks.request");File.WriteAllText(Report,"");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){File.AppendAllText(Report,"Save scene first");return;}UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");SessionState.SetBool("LanChecks",true);EditorApplication.isPlaying=true;};}
 static LanMessage Latest(LanWire wire){LanMessage last=null;while(wire.TryRead(out var json))last=JsonUtility.FromJson<LanMessage>(json);return last;}
 static async void Run(){clients.Clear();error=null;Application.logMessageReceived+=Log;EditorApplication.update+=Pump;try{
  Application.runInBackground=true;await Until(()=>Object.FindFirstObjectByType<MainMenuUI>());LanSession.Open();var session=LanSession.Instance;session.CreateHost();Check(LanSession.Host,"host listens on LAN port");
  for(int i=0;i<3;i++){var client=await LanWire.Connect("127.0.0.1");clients.Add(client);client.Send(JsonUtility.ToJson(new LanMessage{type="hello",version="CodiceLAN-1",name="Test "+i,playerClass=i+2}));}
  await Task.Delay(500);for(int i=0;i<3;i++){var lobby=Latest(clients[i]);Check(lobby!=null&&lobby.members.Length==4,"four members visible to client "+i);}
  using(var extra=await LanWire.Connect("127.0.0.1")){await Task.Delay(200);Check(extra.Closed,"fifth player rejected");}
  session.StartMatch();await Until(()=>RunDirector.Instance&&RunDirector.Instance.CombatActive&&session.Players().Count()==4);await Task.Delay(300);
  Check(session.Players().Select(p=>p.State.Class).Distinct().Count()==4,"independent classes for four players");
  var remote=session.Players().First(p=>p.GetComponent<LanAvatar>()?.id==1);Check(!remote.view.enabled,"remote camera disabled on host");
  var local=RunDirector.Instance.Player;var localPosition=local.transform.position;var start=remote.transform.position;
  for(int i=0;i<6;i++){clients[0].Send(JsonUtility.ToJson(new LanMessage{type="input",scene="Calabozo",move=Vector2.up,yaw=90}));foreach(var c in clients)Latest(c);await Task.Delay(70);}
  Check(Vector3.Distance(start,remote.transform.position)>.2f,"remote input moves its own actor");Check(Vector3.Distance(localPosition,local.transform.position)<.15f,"remote input does not move host");
  clients[0].Send(JsonUtility.ToJson(new LanMessage{type="input",scene="Calabozo"}));await Task.Delay(150);
  RunHUD.Instance.SetPaused(true);Check(Time.timeScale==1,"host menu does not freeze shared world");RunHUD.Instance.SetPaused(false);
  float hostHealth=local.State.Health;local.ReceiveDamage(new DamageInfo{amount=50,source=remote.gameObject});Check(local.State.Health==hostHealth,"friendly fire disabled");
  remote.State.Damage(100000);Check(RunDirector.Instance.Phase==RunPhase.Combat,"one death does not end cooperative run");session.ReviveTeam();Check(remote.State.IsAlive,"fallen teammate revived");
  await Task.Delay(200);foreach(var client in clients){var world=Latest(client);Check(world!=null&&world.type=="world"&&world.entities.Count(e=>e.owner>=0)==4,"four authoritative player snapshots");}
  clients[2].Dispose();await Until(()=>session.Players().Count()==3);Check(session.AnyAlive,"disconnect leaves remaining players running");
  Check(string.IsNullOrEmpty(error),"no runtime exceptions");File.AppendAllText(Report,"LAN_CHECKS_PASSED\n");
 }catch(Exception e){File.AppendAllText(Report,"FAILED "+e);Debug.LogException(e);}finally{foreach(var c in clients)c.Dispose();clients.Clear();if(LanSession.Instance)LanSession.Instance.Leave();Application.logMessageReceived-=Log;EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}}
}
