using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Codice.Enemies;
using Object = UnityEngine.Object;

public static class SpellMonsterSetup
{
    const string Root="Assets/Resources/EnemyVariants/NewMonsters";
    const string Duo="Assets/03_Models/RPG Monster DUO PBR Polyart/";
    const string Skeleton="Assets/03_Models/DungeonCharacters/Skeletons_demo/";
    const string Report="Library/SpellMonsterSetup.txt";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.delayCall+=()=>
        {
            if(Application.isPlaying||!File.Exists("Library/SpellMonsterSetup.request"))return;
            File.Delete("Library/SpellMonsterSetup.request"); File.WriteAllText(Report,"");
            try { Build(); AssetDatabase.SaveAssets(); File.AppendAllText(Report,"SETUP_PASSED\n"); }
            catch(Exception e){File.AppendAllText(Report,e.ToString());Debug.LogException(e);}
        };
    }
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    static void Build()
    {
        Folder(Root);Folder("Assets/Resources/SpellEffects");
        Monster("Slime",Duo+"Prefabs/PolyartDefault/SlimePolyart.prefab",1.1f,EnemyKind.Swordsman,.95f,.75f,1,
            Duo+"Animations/Slime/",new[]{"IdleNormal_Slime_Anim.fbx","Walk_Slime_Anim.fbx","Attack01_Slime_Anim.fbx","GetHit_Slime_Anim.fbx","Die_Slime_Anim.fbx"});
        Monster("TurtleShell",Duo+"Prefabs/PolyartDefault/TurtleShellPolyart.prefab",1.5f,EnemyKind.Heavy,.9f,1.1f,.8f,
            Duo+"Animations/TurtleShell/",new[]{"IdleNormal_TurtleShell_Anim.fbx","Walk_TurtleShell_Anim.fbx","Attack01_TurtleShell_Anim.fbx","GetHit_TurtleShell_Anim.fbx","Die_TurtleShell_Anim.fbx"});
        Monster("DungeonSkeleton",Skeleton+"DungeonSkeleton_demoPrefab.prefab",2.6f,EnemyKind.Heavy,2.4f,1.3f,1.2f,
            Skeleton+"animation/",new[]{"DS_onehand_idle_A.FBX","DS_onehand_walk.FBX","DS_onehand_attack_A.FBX","DS_onehand_idle_A.FBX","DS_onehand_idle_A.FBX"});
        string magic="Assets/06_Effects/Magic effects pack/Prefabs/";
        Effect("Ice",magic+"Hits and explosions/Snow hit.prefab");
        Effect("Lightning",magic+"Hits and explosions/Electro hit.prefab");
        Effect("Heal",magic+"Magic circles/Healing circle.prefab");
        Effect("Impact","Assets/06_Effects/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/0_Classic/VFX_Classic_01.prefab");
        CheckBerserker();
    }
    static AnimationClip Clip(string file,string name,bool loop)
    {
        var source=AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
        var clone=Object.Instantiate(source);clone.name=name;
        AnimationUtility.SetAnimationEvents(clone,Array.Empty<AnimationEvent>());
        var settings=AnimationUtility.GetAnimationClipSettings(clone);settings.loopTime=loop;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;settings.loopBlendOrientation=true;AnimationUtility.SetAnimationClipSettings(clone,settings);
        string path=Root+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing){EditorUtility.CopySerialized(clone,existing);Object.DestroyImmediate(clone);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(clone,path);return clone;
    }
    static void Monster(string name,string source,float height,EnemyKind kind,float hp,float speed,float damage,string animations,string[] files)
    {
        var root=new GameObject(name);
        try
        {
            var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source),root.transform);visual.name="Visual";
            foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
            foreach(var collider in visual.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var body in visual.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
            foreach(var legacy in visual.GetComponentsInChildren<Animation>(true))Object.DestroyImmediate(legacy);
            var animator=visual.GetComponentInChildren<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
            var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            visual.transform.localScale*=height/Mathf.Max(.01f,bounds.size.y);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            Materials(visual,false);
            string controllerPath=Root+"/"+name+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if(!controller.parameters.Any(p=>p.name=="ActionSpeed"))controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
            if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var parameters=controller.parameters;for(int i=0;i<parameters.Length;i++)if(parameters[i].name=="ActionSpeed")parameters[i].defaultFloat=1;controller.parameters=parameters;
            string[] states={"Idle","BlockIdle","RifleIdle","Movement","Patrol","Chase","Melee","HeavyAttack","Shoot","ShootMagic","Hit","Death"};
            int[] sources={0,0,0,1,1,1,2,2,2,2,3,4};
            var machine=controller.layers[0].stateMachine;
            for(int i=0;i<states.Length;i++)
            {
                var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==states[i])??machine.AddState(states[i]);
                state.motion=Clip(animations+files[sources[i]],name+states[i],i<6);
                state.speedParameter="ActionSpeed";state.speedParameterActive=i>=6&&i!=11;
                if(i==0)machine.defaultState=state;
            }
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var enemy=root.AddComponent<BasicEnemy>();enemy.kind=kind;enemy.animator=animator;enemy.useNativeAnimator=true;
            enemy.miniboss=name=="DungeonSkeleton";enemy.variantName=enemy.miniboss?"MINIJEFE • Guardián de la cripta":name=="Slime"?"Baba de mazmorra":"Tortuga acorazada";
            enemy.healthMultiplier=hp;enemy.speedMultiplier=speed;enemy.damageMultiplier=damage;
            var capsule=root.GetComponent<CapsuleCollider>();capsule.height=height;capsule.center=Vector3.up*height*.5f;capsule.radius=name=="Slime"?.5f:.55f;
            var agent=root.GetComponent<NavMeshAgent>();agent.height=height;agent.radius=.5f;agent.angularSpeed=360;agent.acceleration=12;agent.baseOffset=0;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/EnemyVariants/"+name+".prefab");EditorUtility.SetDirty(controller);
            File.AppendAllText(Report,"BUILT "+name+" native animation, URP materials, height="+height+"\n");
        }
        finally{Object.DestroyImmediate(root);}
    }
    static void Effect(string name,string source)
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source));
        try
        {
            root.name=name;Materials(root,true);
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/SpellEffects/"+name+".prefab");
        }
        finally{Object.DestroyImmediate(root);}
    }
    static void Materials(GameObject root,bool particles)
    {
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(!source)continue;
                if(source.shader&&source.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
                string path=Root+"/"+guid+"_"+id+(particles?"_VFX":"")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material)
                {
                    material=new Material(Shader.Find(particles?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));
                    material.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):source.HasProperty("_TintColor")?source.GetColor("_TintColor"):Color.white);
                    string texture=source.HasProperty("_MainTex")?"_MainTex":"_BaseMap";
                    if(source.HasProperty(texture)){material.SetTexture("_BaseMap",source.GetTexture(texture));material.SetTextureScale("_BaseMap",source.GetTextureScale(texture));material.SetTextureOffset("_BaseMap",source.GetTextureOffset(texture));}
                    if(particles){material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;}
                    else {material.SetFloat("_Smoothness",.2f);if(source.HasProperty("_BumpMap")){material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));if(material.GetTexture("_BumpMap"))material.EnableKeyword("_NORMALMAP");}}
                    AssetDatabase.CreateAsset(material,path);
                }
                materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
    }
    static void CheckBerserker()
    {
        var model=(ModelImporter)AssetImporter.GetAtPath("Assets/03_Models/Bosses/Ork berserker/Base mesh/Base mesh.fbx");
        var pack=(ModelImporter)AssetImporter.GetAtPath("Assets/DoubleL/Model/Armature.fbx");
        File.AppendAllText(Report,"RIGS Berserker="+model.animationType+" DoubleL="+pack.animationType+"; keep native attack bindings\n");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/01_Prefabs/Bosses/OrkBerserkerCombat.controller");
        foreach(var state in controller.layers[0].stateMachine.states.Select(s=>s.state))
        {
            if(!state.motion)throw new Exception("Empty berserker state: "+state.name);
            var clip=state.motion as AnimationClip;
            if(clip&&new[]{"Melee","Shoot","Slam"}.Contains(state.name))
            {
                if(AnimationUtility.GetCurveBindings(clip).Length==0)throw new Exception("Unbound berserker clip: "+state.name);
                File.AppendAllText(Report,"BERSERKER "+state.name+" = "+clip.name+" ("+clip.length+"s)\n");
            }
        }
    }
}
