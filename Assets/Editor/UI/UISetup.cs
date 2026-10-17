using System;
using System.Linq;
using Codice.Mechanics;
using UnityEditor;
using UnityEngine;

public static class UISetup
{
    [MenuItem("Codice/UI/Validate installation (no scene rebuild)")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before validating installation.");
        // RunScene installs one scene-owned HUD at runtime. No map or prefab serialization is needed.
        foreach(string scene in new[]{"Calabozo","Coliseo"})
        {
            string path="Assets/00_Scenes/Maps/"+scene+".unity";
            if(!System.IO.File.Exists(path))throw new Exception("Missing map: "+path);
            if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==path))throw new Exception("Map is not enabled in Build Settings: "+path);
            string data=System.IO.File.ReadAllText(path);
            string guid=AssetDatabase.AssetPathToGUID("Assets/02_Scripts/Mechanics/RunScene.cs");
            if(!data.Contains(guid))throw new Exception("Missing RunScene integration: "+scene);
        }
        Debug.Log("UI_INSTALLATION_OK: Calabozo + Coliseo, runtime installation; maps and player prefab unchanged.");
    }
}
