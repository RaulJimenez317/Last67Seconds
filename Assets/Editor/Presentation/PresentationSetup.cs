using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Codice.Presentation;
using Codice.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class PresentationSetup
{
    const string Output="Assets/Presentation";
    const string Pack="Assets/07_Animacion/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/";
    static readonly Dictionary<string,string[]> states=new Dictionary<string,string[]>{
        {"Idle",new[]{"General","Idle_A"}},{"Ready",new[]{"CombatMelee","Melee_2H_Idle"}},
        {"Movement",new[]{"MovementBasic","Walking_A"}},{"Patrol",new[]{"MovementBasic","Walking_B"}},{"Chase",new[]{"MovementBasic","Running_A"}},
        {"Melee",new[]{"CombatMelee","Melee_1H_Attack_Slice_Horizontal"}},{"HeavyAttack",new[]{"CombatMelee","Melee_2H_Attack_Chop"}},{"Thrust",new[]{"CombatMelee","Melee_1H_Attack_Stab"}},
        {"Shoot",new[]{"CombatRanged","Ranged_2H_Shoot"}},{"ShootBow",new[]{"CombatRanged","Ranged_Bow_Release"}},{"ShootMagic",new[]{"CombatRanged","Ranged_Magic_Shoot"}},
        {"BowIdle",new[]{"CombatRanged","Ranged_Bow_Aiming_Idle"}},{"RifleIdle",new[]{"CombatRanged","Ranged_2H_Aiming"}},{"BlockIdle",new[]{"CombatMelee","Melee_Blocking"}},
        {"Hit",new[]{"General","Hit_A"}},{"Death",new[]{"General","Death_B"}},{"Dash",new[]{"MovementAdvanced","Dodge_Forward"}}
    };
    [MenuItem("Codice/Presentation/Install animations effects and menus")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene first.");
        Directory.CreateDirectory(Output+"/Animations");Directory.CreateDirectory(Output+"/Effects");Directory.CreateDirectory("Assets/Resources/Presentation");AssetDatabase.Refresh();
        var library=AssetDatabase.LoadAssetAtPath<PresentationLibrary>("Assets/Resources/Presentation/Library.asset");
        if(!library){library=ScriptableObject.CreateInstance<PresentationLibrary>();AssetDatabase.CreateAsset(library,"Assets/Resources/Presentation/Library.asset");}
        library.medium=Controller("KayKit",null);
        library.goblin=Controller("Goblin","Puglin");library.demon=Controller("Demon","Imp");
        library.impact=Effect("Hits and explosions/Stones hit","Impact");library.dust=Effect("Smoke effects/Dust puff","Footstep dust");library.explosion=Effect("Hits and explosions/Explosion","Explosion");library.muzzle=Effect("Sparks/Sparks explode yellow","Muzzle flash");
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        string current=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string menu="Assets/00_Scenes/MainMenu.unity";
        if(!File.Exists(menu)){
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Main Menu").AddComponent<MainMenuUI>();
            var camera=new GameObject("Menu camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ProceduralUISprites.Abyss;camera.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene,menu);
            if(!string.IsNullOrEmpty(current))EditorSceneManager.OpenScene(current);
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(menu,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=menu)).ToArray();
        Debug.Log("PRESENTATION_INSTALLED");
    }
    static RuntimeAnimatorController Controller(string profile,string boss)
    {
        string path=Output+"/Animations/"+profile+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        if(!controller.parameters.Any(p=>p.name=="ActionSpeed"))controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
        var sm=controller.layers[0].stateMachine;
        foreach(var entry in states){
            string category=entry.Value[0],clipName=entry.Value[1],rig="Medium";
            if(boss!=null){
                // Large-body melee and locomotion; Medium contains the ranged motions absent from Large.
                if(category=="MovementBasic"){rig="Large";if(clipName=="Walking_B")clipName="Walking_A";}
                if(category=="General"){rig="Large";if(clipName=="Death_B")clipName="Death_A";}
                if(category=="CombatMelee"){rig="Large";if(clipName=="Melee_1H_Attack_Slice_Horizontal")clipName="Melee_1H_Slash";if(clipName=="Melee_2H_Attack_Chop")clipName="Melee_2H_Slam";if(clipName=="Melee_1H_Attack_Stab")clipName="Melee_1H_Stab";}
                if(boss=="Puglin"&&entry.Key=="Shoot"){category="General";clipName="Throw";rig="Medium";}
            }
            string sourcePath=Pack+"Rig_"+rig+"/Rig_"+rig+"_"+category+".fbx";
            var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().FirstOrDefault(c=>c.name==clipName);
            if(!source)throw new Exception("Missing animation "+sourcePath+" / "+clipName);
            var clip=boss==null?Object.Instantiate(source):Retarget(source,sourcePath,boss);
            clip.name=entry.Key;AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
            bool loop=new[]{"Idle","Ready","Movement","Patrol","Chase","BowIdle","RifleIdle","BlockIdle"}.Contains(entry.Key);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string clipPath=Output+"/Animations/"+profile+"_"+entry.Key+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}
            else AssetDatabase.CreateAsset(clip,clipPath);
            var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==entry.Key);
            if(!state)state=sm.AddState(entry.Key);
            state.motion=clip;state.speedParameterActive=!loop&&entry.Key!="Death";state.speedParameter="ActionSpeed";if(entry.Key=="Idle")sm.defaultState=state;
        }
        EditorUtility.SetDirty(controller);return controller;
    }
    static AnimationClip Retarget(AnimationClip clip,string sourcePath,string boss)
    {
        var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
        var target=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Models/Bosses/Exports/FBX (Unity)/"+boss+".fbx"));
        try{
            source.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);target.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            string[] from={"hips","spine","chest","head","upperarm.l","lowerarm.l","hand.l","upperarm.r","lowerarm.r","hand.r","upperleg.l","lowerleg.l","foot.l","upperleg.r","lowerleg.r","foot.r"};
            string[] to={"pelvis","spine_01","spine_03","head","upperarm_l","lowerarm_l","hand_l","upperarm_r","lowerarm_r","hand_r","thigh_l","calf_l","foot_l","thigh_r","calf_r","foot_r"};
            var a=from.Select(n=>source.GetComponentsInChildren<Transform>().First(t=>string.Equals(t.name,n,StringComparison.OrdinalIgnoreCase))).ToArray();var b=to.Select(n=>target.GetComponentsInChildren<Transform>().First(t=>string.Equals(t.name,n,StringComparison.OrdinalIgnoreCase))).ToArray();
            var tpose=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().FirstOrDefault(c=>c.name=="T-Pose");if(tpose)tpose.SampleAnimation(source,0);
            var ar=a.Select(t=>t.rotation).ToArray();var br=b.Select(t=>t.rotation).ToArray();Vector3 hipA=a[0].position,hipB=b[0].position;float ratio=Mathf.Abs(hipB.y)/Mathf.Max(.01f,Mathf.Abs(hipA.y));
            var curves=new AnimationCurve[b.Length,4];for(int i=0;i<b.Length;i++)for(int j=0;j<4;j++)curves[i,j]=new AnimationCurve();var pos=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            int count=Mathf.CeilToInt(clip.length*30);var previous=new Quaternion[b.Length];
            for(int frame=0;frame<=count;frame++){
                float time=Mathf.Min(clip.length,frame/30f);clip.SampleAnimation(source,time);
                for(int i=0;i<b.Length;i++){
                    b[i].rotation=a[i].rotation*Quaternion.Inverse(ar[i])*br[i];
                    var q=b[i].localRotation;if(frame>0&&Quaternion.Dot(q,previous[i])<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);previous[i]=q;
                    for(int j=0;j<4;j++)curves[i,j].AddKey(time,q[j]);
                }
                b[0].position=hipB+(a[0].position-hipA)*ratio;var p=b[0].localPosition;for(int j=0;j<3;j++)pos[j].AddKey(time,p[j]);
            }
            var output=new AnimationClip();output.frameRate=30;
            for(int i=0;i<b.Length;i++)for(int j=0;j<4;j++)output.SetCurve(AnimationUtility.CalculateTransformPath(b[i],target.transform),typeof(Transform),"localRotation."+"xyzw"[j],curves[i,j]);
            for(int j=0;j<3;j++)output.SetCurve(AnimationUtility.CalculateTransformPath(b[0],target.transform),typeof(Transform),"localPosition."+"xyz"[j],pos[j]);
            output.EnsureQuaternionContinuity();return output;
        }finally{Object.DestroyImmediate(source);Object.DestroyImmediate(target);}
    }
    public static GameObject Effect(string sourceName,string name)
    {
        string path=Output+"/Effects/"+name+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/06_Effects/Magic effects pack/Prefabs/"+sourceName+".prefab"));root.name=name;
        try{
            foreach(var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true)){
                renderer.sharedMaterials=renderer.sharedMaterials.Select(Material).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;
                if(renderer.trailMaterial)renderer.trailMaterial=Material(renderer.trailMaterial);
            }
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=false;main.maxParticles=Mathf.Min(main.maxParticles,150);var collision=ps.collision;collision.enabled=false;}
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{Object.DestroyImmediate(root);}
    }
    static Material Material(Material original)
    {
        if(!original)return null;string path=Output+"/Effects/"+original.name+"_URP.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
        var result=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        result.SetTexture("_BaseMap",original.HasProperty("_MainTex")?original.GetTexture("_MainTex"):null);
        result.SetColor("_BaseColor",original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);
        result.SetFloat("_Surface",1);result.SetFloat("_Blend",0);result.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);result.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);result.SetFloat("_ZWrite",0);result.SetFloat("_Cull",0);result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");result.renderQueue=3000;
        AssetDatabase.CreateAsset(result,path);return result;
    }
}
