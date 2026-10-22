using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Codice.Characters;
using Codice.Enemies;
using Codice.Presentation;
using Object=UnityEngine.Object;
public static class DeflectEnemySetup
{
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists("Library/DeflectEnemy.request")||Application.isPlaying)return;var mode=File.ReadAllText("Library/DeflectEnemy.request").Trim();File.Delete("Library/DeflectEnemy.request");try{if(mode=="icons")BindIcons();else Build();File.WriteAllText("Library/DeflectEnemy.txt","BUILD PASSED");}catch(Exception e){File.WriteAllText("Library/DeflectEnemy.txt",e.ToString());Debug.LogException(e);}};}
 static void Build(){Icons();Enemies();Animations();AssetDatabase.SaveAssets();}
 static void BindIcons(){var catalog=WeaponVisualCatalog.Load();for(int i=0;i<42;i++){var model=i<40?catalog.tiers[i]:i==40?catalog.katana:catalog.shuriken;string path="Assets/Resources/WeaponIcons/"+model.name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();catalog.icons[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!catalog.icons[i])throw new Exception("Icon missing "+path);}EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();}
 static void Icons(){
  const string folder="Assets/Resources/WeaponIcons";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var catalog=WeaponVisualCatalog.Load();catalog.icons=new Sprite[42];
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  try{
   var go=new GameObject("Icon camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
   var lightGo=new GameObject("Icon light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(25,-25,0);
   for(int i=0;i<42;i++){
    var prefab=i<40?catalog.tiers[i]:i==40?catalog.katana:catalog.shuriken;var model=Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
    if(i<40&&i/4==2)model.transform.rotation=Quaternion.Euler(0,65,0);
    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.62f;camera.transform.position=bounds.center-Vector3.forward*5;
    var rt=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(192,192,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,192,192),0,0);texture.Apply();string path=folder+"/"+prefab.name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(model);
    AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();catalog.icons[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   }
   EditorUtility.SetDirty(catalog);
  }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 static void Enemies(){
  Directory.CreateDirectory("Assets/Resources/EnemyVariants");AssetDatabase.Refresh();
  string[] models={"Rogue_Hooded","Ranger","Mage","Barbarian"},names={"Saqueador de la cripta","Cazador renegado","Hechicero del abismo","Guardián bárbaro"},textures={"rogue","ranger","mage","barbarian"};
  float[] hp={.8f,1.1f,1.2f,1.15f},speed={1.3f,1.15f,.85f,1.1f};var catalog=WeaponVisualCatalog.Load();int[] weapon={3*4+1,2*4+2,8*4+2,4*4+2};
  for(int i=0;i<4;i++){
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Enemies/"+(EnemyKind)i+".prefab"));try{
    foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
    root.name=names[i];var enemy=root.GetComponent<BasicEnemy>();enemy.variantName=names[i];enemy.healthMultiplier=hp[i];enemy.speedMultiplier=speed[i];enemy.damageMultiplier=i==0?.85f:1.1f;enemy.shield=null;
    var visual=CharacterSetup.Model("Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/Characters/fbx/"+models[i]+".fbx",root.transform,i==3?1.95f:1.8f,textures[i]);enemy.animator=visual.GetComponentInChildren<Animator>();if(!enemy.animator)enemy.animator=visual.AddComponent<Animator>();enemy.animator.runtimeAnimatorController=PresentationLibrary.Load().medium;enemy.animator.applyRootMotion=false;
    var hand=visual.GetComponentsInChildren<Transform>().First(t=>t.name.Equals(i==1?"handslot.l":"handslot.r",StringComparison.OrdinalIgnoreCase));var item=Object.Instantiate(catalog.tiers[weapon[i]],hand);item.transform.localPosition=Vector3.zero;item.transform.localRotation=Quaternion.identity;item.transform.localScale=Vector3.one/hand.lossyScale.x;enemy.weaponPivot=item.transform;
    if(i==3){var left=visual.GetComponentsInChildren<Transform>().First(t=>t.name.Equals("handslot.l",StringComparison.OrdinalIgnoreCase));enemy.shield=Object.Instantiate(catalog.tiers[7*4],left);enemy.shield.transform.localScale=Vector3.one/left.lossyScale.x;}
    PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/EnemyVariants/"+(EnemyKind)i+".prefab");
   }finally{Object.DestroyImmediate(root);}
  }
 }
 static void Animations(){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Presentation/Animations/KayKit.controller");var machine=controller.layers[0].stateMachine;
  string[] names={"ShinobiGuard","ShinobiDeflect","ShinobiGuardHit"},clips={"Melee_Block","Melee_Block_Attack","Melee_Block_Hit"};
  var source=AssetDatabase.LoadAllAssetsAtPath("Assets/07_Animacion/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatMelee.fbx").OfType<AnimationClip>().ToArray();
  for(int i=0;i<3;i++){var clip=Object.Instantiate(source.First(c=>c.name==clips[i]));clip.name=names[i];AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=i==0;AnimationUtility.SetAnimationClipSettings(clip,settings);string path="Assets/Presentation/Animations/"+names[i]+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);clip=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(clip,path);var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==names[i]);if(!state)state=machine.AddState(names[i]);state.motion=clip;state.speedParameter="ActionSpeed";state.speedParameterActive=i>0;}
  EditorUtility.SetDirty(controller);
 }
}
