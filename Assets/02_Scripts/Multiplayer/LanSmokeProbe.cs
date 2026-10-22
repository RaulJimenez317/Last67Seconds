#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Codice.Mechanics;
using UnityEngine;
namespace Codice.Multiplayer
{
    // Explicit development-build test runner. Never starts in normal gameplay.
    public sealed class LanSmokeProbe : MonoBehaviour
    {
        string report;bool host;int index;string error;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"-lan-probe");
            if(flag<0||flag+2>=args.Length)return;
            var probe=new GameObject("LAN smoke probe").AddComponent<LanSmokeProbe>();probe.host=args[flag+1]=="host";int.TryParse(args[flag+1],out probe.index);probe.report=args[flag+2];DontDestroyOnLoad(probe.gameObject);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){error=message;File.AppendAllText(report,"ERROR "+message+"\n"+trace+"\n");}}
        IEnumerator Start()
        {
            File.WriteAllText(report,"START "+(host?"host":"client "+index)+"\n");Application.logMessageReceived+=Log;Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(1);
            LanSession.Open();var session=LanSession.Instance;
            typeof(LanSession).GetField("nickname",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,host?"Host":"Client "+index);
            typeof(LanSession).GetField("chosenClass",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,host?0:index+1);
            if(host)session.CreateHost();else{typeof(LanSession).GetField("address",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,"127.0.0.1");session.Join();}
            float end=Time.realtimeSinceStartup+100;bool entered=false;float gameAt=0;
            while(Time.realtimeSinceStartup<end)
            {
                if(host&&!entered){var members=(LanMember[])typeof(LanSession).GetField("members",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(session);if(members.Length==4)session.StartMatch();}
                if(RunDirector.Instance&&RunDirector.Instance.Player)
                {
                    if(!entered){entered=true;gameAt=Time.realtimeSinceStartup;File.AppendAllText(report,"ENTERED "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" class="+RunDirector.Instance.Player.State.Class+"\n");}
                    int count=host?session.Players().Count():FindObjectsByType<LanGhost>(FindObjectsSortMode.None).Count(g=>g.snapshot!=null&&g.snapshot.owner>=0)+1;
                    if(Time.realtimeSinceStartup-gameAt>8&&count==4){File.AppendAllText(report,string.IsNullOrEmpty(error)?"FOUR_PROCESS_LAN_PASSED\n":"FAILED_RUNTIME\n");break;}
                }
                if(!session){File.AppendAllText(report,"FAILED_DISCONNECTED\n");break;}
                yield return null;
            }
            if(!entered)File.AppendAllText(report,"FAILED_TIMEOUT\n");
            yield return new WaitForSecondsRealtime(host?6:1);Application.logMessageReceived-=Log;Application.Quit();
        }
    }
}
#endif
