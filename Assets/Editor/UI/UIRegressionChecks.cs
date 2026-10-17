using System;
using System.Linq;
using Codice.Mechanics;
using Codice.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// SessionState survives domain reloads between suites. All scene fixtures are unsaved.
[InitializeOnLoad]
public static class UIRegressionChecks
{
    const string Key="UIRegression.";
    static bool launching;
    static double deadline,nextPrepare;
    public static string Result=>SessionState.GetString(Key+"result","Not run");
    static UIRegressionChecks()
    {
        EditorApplication.playModeStateChanged+=Changed;
        EditorApplication.update+=Tick;
    }
    [MenuItem("Codice/UI/Run ALL checks (236 previous + UI)")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before running all checks.");
        if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save your scene edits before running all checks.");
        SessionState.SetString(Key+"original",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        var gameView=GameView();var size=gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        SessionState.SetInt(Key+"size",(int)size.GetValue(gameView));size.SetValue(gameView,0);
        SessionState.SetString(Key+"result","Running");SessionState.SetInt(Key+"stage",0);SessionState.SetBool(Key+"active",true);
        try{UISetup.Install();CharacterSetup.ValidateTransactions();Prepare();}catch(Exception e){Fail(e.Message);}
    }
    static EditorWindow GameView()=>EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
    static void RestoreView(){var view=GameView();view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(view,SessionState.GetInt(Key+"size",0));}
    static void Prepare()
    {
        int stage=SessionState.GetInt(Key+"stage",0);
        if(stage>3){Finish("ALL_UI_REGRESSIONS_PASSED: 236 previous + 50 UI = 286");return;}
        EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/"+(stage==2?"Coliseo":"Calabozo")+".unity");
        if(stage==1||stage==2)
        {
            var scene=Object.FindFirstObjectByType<RunScene>();scene.gameObject.SetActive(false);
            if(stage==1)scene.dungeon.startOnPlay=true;
            else{scene.arena.managedByRun=false;scene.arena.startOnPlay=true;}
        }
        // Start suites only after real simulation frames have warmed up.
        SessionState.SetBool(Key+"started",false);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange change)
    {
        if(!SessionState.GetBool(Key+"active",false))return;
        if(change==PlayModeStateChange.EnteredPlayMode){deadline=EditorApplication.timeSinceStartup+120;launching=true;}
        if(change==PlayModeStateChange.EnteredEditMode)nextPrepare=EditorApplication.timeSinceStartup+.5;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key+"active",false))return;
        if(!EditorApplication.isPlaying)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&EditorApplication.timeSinceStartup>=nextPrepare)
            {nextPrepare=EditorApplication.timeSinceStartup+2;try{Prepare();}catch(Exception e){Fail(e.ToString());}}
            return;
        }
        try
        {
            int stage=SessionState.GetInt(Key+"stage",0);
            if(EditorApplication.timeSinceStartup>deadline){Fail("Timed out waiting for stage "+stage);return;}
            if(launching)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                Application.runInBackground=true;
                if(Time.time<2)return;
                // Wait for scene Start and the run singleton without sleeping the editor thread.
                var scene=Object.FindFirstObjectByType<RunScene>(FindObjectsInactive.Include);
                if(!scene||!scene.player||(stage==2&&!scene.arena.ActiveBoss)||(stage==1&&Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Length==0))return;
                Application.runInBackground=true;
                if(stage==1||stage==2)RunHUD.Install(scene);
                if(stage==0)MechanicsChecks.Begin();else if(stage==1)EnemyPlayChecks.Begin();else if(stage==2)BossPlayChecks.Begin();else UIChecks.Begin();
                launching=false;SessionState.SetBool(Key+"started",true);
            }
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline){Fail("Timed out waiting for stage "+stage);return;}
            if(!SessionState.GetBool(Key+"started",false))return;
            string result=stage==0?MechanicsChecks.Result:stage==1?EnemyPlayChecks.Result:stage==2?BossPlayChecks.Result:UIChecks.Result;
            if(result=="Not run"||result=="Running")return;
            if(!result.Contains("_PASSED:")){Fail(result);return;}
            if(stage==0)CharacterPlayChecks.Run();
            Debug.Log("UI regression stage "+stage+": "+result);
            SessionState.SetBool(Key+"started",false);SessionState.SetInt(Key+"stage",stage+1);EditorApplication.isPlaying=false;
        }
        catch(Exception e){Fail(e.ToString());}
    }
    static void Fail(string message)
    {
        RestoreView();
        SessionState.SetString(Key+"result","FAILED: "+message);SessionState.SetBool(Key+"active",false);Debug.LogError(Result);
        if(RunHUD.Instance)RunHUD.Instance.SetPaused(false);
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
    }
    static void Finish(string result)
    {
        RestoreView();
        SessionState.SetBool(Key+"active",false);SessionState.SetString(Key+"result",result);
        string original=SessionState.GetString(Key+"original","");if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Debug.Log(result);
    }
}
