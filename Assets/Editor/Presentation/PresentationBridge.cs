using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class PresentationBridge
{
    static double next;
    static PresentationBridge(){EditorApplication.update+=Tick;}
    static void Tick(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling)return;next=EditorApplication.timeSinceStartup+1;
        string file="MapReview/Presentation/request.txt";if(!File.Exists(file))return;string cmd=File.ReadAllText(file).Trim();File.Delete(file);
        try{if(cmd=="audit")Audit();else EditorApplication.ExecuteMenuItem(cmd);File.WriteAllText("MapReview/Presentation/result.txt","OK "+cmd);}catch(Exception e){File.WriteAllText("MapReview/Presentation/result.txt",e.ToString());Debug.LogException(e);}}
    static void Audit(){var s=new StringBuilder();
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&(p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".glb")))){
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            if(clips.Length==0)continue;s.AppendLine(path);foreach(var c in clips)s.AppendLine("  "+c.name+" | "+c.length+"s | human="+c.humanMotion+" | "+string.Join(",",AnimationUtility.GetCurveBindings(c).Take(3).Select(b=>b.path+":"+b.propertyName)));}
        File.WriteAllText("MapReview/Presentation/animations.txt",s.ToString());s.Clear();
        foreach(string path in new[]{"Assets/01_Prefabs/Characters/Player.prefab","Assets/01_Prefabs/Enemies/Swordsman.prefab","Assets/01_Prefabs/Bosses/GoblinKing.prefab","Assets/01_Prefabs/Bosses/DemonLord.prefab"}){
            s.AppendLine(path);var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!root)continue;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))s.AppendLine(AnimationUtility.CalculateTransformPath(t,root.transform)+" scale="+t.localScale+" pos="+t.localPosition+" rot="+t.localEulerAngles);
        }File.WriteAllText("MapReview/Presentation/hierarchy.txt",s.ToString());
    }
}
