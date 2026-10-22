using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Codice.Multiplayer;
using Codice.Enemies;
using Codice.Bosses;
public static class LanSetup
{
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(Application.isPlaying||!File.Exists("Library/LanSetup.request"))return;File.Delete("Library/LanSetup.request");try{Build();File.WriteAllText("Library/LanSetup.txt","LAN_SETUP_PASSED");}catch(Exception e){File.WriteAllText("Library/LanSetup.txt",e.ToString());Debug.LogException(e);}};}
 public static void Build()
 {
  var entries=new Dictionary<string,GameObject>();
  foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/01_Prefabs/Enemies","Assets/01_Prefabs/Bosses","Assets/Resources/EnemyVariants"}))
  {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));var enemy=prefab.GetComponent<BasicEnemy>();if(enemy)entries["Enemy:"+enemy.kind+":"+enemy.variantName]=prefab;var boss=prefab.GetComponent<BossCharacter>();if(boss)entries["Boss:"+boss.kind]=prefab;}
  var library=Codice.Presentation.PresentationLibrary.Load();foreach(var field in typeof(Codice.Presentation.PresentationLibrary).GetFields())if(field.FieldType==typeof(GameObject)){var effect=field.GetValue(library) as GameObject;if(effect)entries["Vfx:"+effect.name]=effect;}
  var catalog=AssetDatabase.LoadAssetAtPath<LanCatalog>("Assets/Resources/LanCatalog.asset");if(!catalog){catalog=ScriptableObject.CreateInstance<LanCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/LanCatalog.asset");}
  var list=new List<LanCatalog.Entry>();foreach(var pair in entries)list.Add(new LanCatalog.Entry{key=pair.Key,prefab=pair.Value});catalog.entries=list.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 }
}
