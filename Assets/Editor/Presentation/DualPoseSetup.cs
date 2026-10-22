using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class DualPoseSetup
{
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists("Library/DualPose.request")||Application.isPlaying)return;File.Delete("Library/DualPose.request");try{Build();File.WriteAllText("Library/DualPose.txt","PASSED");}catch(Exception e){File.WriteAllText("Library/DualPose.txt",e.ToString());Debug.LogException(e);}};}
 static void Build(){
  var source=AssetDatabase.LoadAllAssetsAtPath("Assets/07_Animacion/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatMelee.fbx").OfType<AnimationClip>().ToArray();
  string[] names={"DualIdle","DualSlice","DualStab"},clips={"Melee_Dualwield_Attack_Slice","Melee_Dualwield_Attack_Slice","Melee_Dualwield_Attack_Stab"};
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Presentation/Animations/KayKit.controller");var sm=controller.layers[0].stateMachine;
  for(int i=0;i<3;i++){
   var clip=UnityEngine.Object.Instantiate(source.First(c=>c.name==clips[i]));clip.name=names[i];AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
   if(i==0)foreach(var binding in AnimationUtility.GetCurveBindings(clip)){var curve=AnimationUtility.GetEditorCurve(clip,binding);float v=curve.Evaluate(0);AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,1,v));}
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=i==0;AnimationUtility.SetAnimationClipSettings(clip,settings);string path="Assets/Presentation/Animations/"+names[i]+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(existing);}else AssetDatabase.CreateAsset(clip,path);
   var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==names[i]);if(!state)state=sm.AddState(names[i]);state.motion=clip;state.speedParameter="ActionSpeed";state.speedParameterActive=i>0;
  }
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
 }
}
