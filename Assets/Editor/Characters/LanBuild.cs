using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class LanBuild
{
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/LanBuild.request"))return;File.Delete("Library/LanBuild.request");try{LanSetup.Build();Directory.CreateDirectory("Builds/LAN");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName="Builds/LAN/Last67Seconds.exe",options=BuildOptions.Development});File.WriteAllText("Library/LanBuild.txt",result.summary.result+" errors="+result.summary.totalErrors+" bytes="+result.summary.totalSize);if(result.summary.result!=BuildResult.Succeeded)Debug.LogError("LAN build failed");}catch(Exception e){File.WriteAllText("Library/LanBuild.txt",e.ToString());Debug.LogException(e);}};}
}
