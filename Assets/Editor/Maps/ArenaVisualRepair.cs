using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Codice.Mechanics;
using Codice.Bosses;
using Object=UnityEngine.Object;

// Explicit authoring pass; never runs in builds or without a local request.
public static class ArenaVisualRepair
{
    const string Report="Library/ArenaVisualReport.txt";
    const string Materials="Assets/01_Prefabs/Bosses/CompatibleMaterials";
    [InitializeOnLoadMethod] static void Boot(){EditorApplication.playModeStateChanged+=state=>{
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ArenaVisual.Test",false)){SessionState.SetBool("ArenaVisual.Test",false);PlayCheck();}
    };EditorApplication.delayCall+=()=>{
        if(!EditorApplication.isPlaying&&File.Exists("Library/ArenaTest.request")){File.Delete("Library/ArenaTest.request");EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/FinalArena.unity");PlayerPrefs.SetInt("Codice.TestRound",20);SessionState.SetBool("ArenaVisual.Test",true);EditorApplication.isPlaying=true;}
        else if(!EditorApplication.isPlaying&&File.Exists("Library/ArenaVisual.request")){File.Delete("Library/ArenaVisual.request");Repair();}
        else if(EditorApplication.isPlaying&&SessionState.GetBool("ArenaVisual.Test",false)){SessionState.SetBool("ArenaVisual.Test",false);PlayCheck();}
    };}
    static void Log(string s){File.AppendAllText(Report,s+"\n");}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static void ConvertMaterials(GameObject root)
    {
        if(!AssetDatabase.IsValidFolder(Materials))AssetDatabase.CreateFolder("Assets/01_Prefabs/Bosses","CompatibleMaterials");
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];
                if(!source)
                {
                    string fallbackPath=Materials+"/MissingSlot.mat";var fallback=AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
                    if(!fallback){fallback=new Material(Shader.Find("Universal Render Pipeline/Lit"));fallback.SetColor("_BaseColor",new Color(.32f,.28f,.22f));fallback.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(fallback,fallbackPath);}
                    materials[i]=fallback;continue;
                }
                if(source.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
                string path=Materials+"/"+guid+"_"+id+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name=source.name+" URP";
                    if(source.HasProperty("_Color"))material.SetColor("_BaseColor",source.GetColor("_Color"));
                    string[] from={"_MainTex","_BumpMap","_MetallicGlossMap","_OcclusionMap","_EmissionMap"};
                    string[] to={"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap","_EmissionMap"};
                    for(int j=0;j<from.Length;j++)if(source.HasProperty(from[j])){material.SetTexture(to[j],source.GetTexture(from[j]));material.SetTextureScale(to[j],source.GetTextureScale(from[j]));material.SetTextureOffset(to[j],source.GetTextureOffset(from[j]));}
                    foreach(string p in new[]{"_Metallic","_BumpScale","_OcclusionStrength","_Cutoff"})if(source.HasProperty(p))material.SetFloat(p,source.GetFloat(p));
                    material.SetFloat("_Smoothness",source.HasProperty("_Glossiness")?source.GetFloat("_Glossiness"):.2f);
                    if(material.GetTexture("_BumpMap"))material.EnableKeyword("_NORMALMAP");
                    if(material.GetTexture("_MetallicGlossMap"))material.EnableKeyword("_METALLICSPECGLOSSMAP");
                    if(material.GetTexture("_OcclusionMap"))material.EnableKeyword("_OCCLUSIONMAP");
                    if(source.HasProperty("_EmissionColor")){material.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));if(source.IsKeywordEnabled("_EMISSION"))material.EnableKeyword("_EMISSION");}
                    if(source.HasProperty("_Mode")&&source.GetFloat("_Mode")==1){material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
                    material.SetFloat("_Cull",0);
                    AssetDatabase.CreateAsset(material,path);
                }
                materials[i]=material;
            }
            r.sharedMaterials=materials;
        }
    }
    static Bounds Geometry(GameObject root)
    {
        Bounds bounds=default;bool first=true;
        foreach(var r in root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
        {
            Mesh mesh;bool temporary=r is SkinnedMeshRenderer;
            if(r is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh,true);}
            else {var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;mesh=filter.sharedMesh;}
            foreach(var vertex in mesh.vertices){var p=r.transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            if(temporary)Object.DestroyImmediate(mesh);
        }
        return bounds;
    }
    static void RepairBoss(string name)
    {
        string path="Assets/01_Prefabs/Bosses/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var boss=root.GetComponent<BossCharacter>();
            string source=name=="MonsterOrc"?"Assets/03_Models/Bosses/Monster_Orc (Troll)/Prefabs/monster_orc.prefab":name=="MonsterMutant"?"Assets/03_Models/Bosses/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin1.prefab":"Assets/03_Models/Bosses/Ork berserker/prefab/ork berserk prefab.prefab";
            Object.DestroyImmediate(boss.visual.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),root.transform);
            visual.name="Native animated visual";boss.visual=visual.transform;
            foreach(var c in visual.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            var renderers=visual.GetComponentsInChildren<Renderer>();var initial=renderers[0].bounds;foreach(var r in renderers)initial.Encapsulate(r.bounds);
            visual.transform.localScale*=3.2f/initial.size.y;
            initial=renderers[0].bounds;foreach(var r in renderers)initial.Encapsulate(r.bounds);
            visual.transform.position-=new Vector3(initial.center.x,initial.min.y,initial.center.z);
            ConvertMaterials(root);
            var a=boss.visual.GetComponentInChildren<Animator>();a.applyRootMotion=false;
            Log(name+" REST geometry="+Geometry(a.gameObject));
            var sourceClips=a.runtimeAnimatorController.animationClips.Distinct().ToArray();
            var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/01_Prefabs/Bosses/"+name+"Combat.controller");
            if(name=="OrkBerserker")
            {
                // The vendor's demo controller has empty attack states. Bind the actual FBX clips explicitly.
                BindAttack(controller,"Melee","atack1");BindAttack(controller,"Shoot","atack1");BindAttack(controller,"Slam","atack2");
            }
            a.runtimeAnimatorController=controller;
            var parameters=controller.parameters;for(int i=0;i<parameters.Length;i++)if(parameters[i].name=="ActionSpeed")parameters[i].defaultFloat=1;controller.parameters=parameters;EditorUtility.SetDirty(controller);
            var clips=a.runtimeAnimatorController.animationClips.Distinct().ToArray();
            var transforms=visual.GetComponentsInChildren<Transform>(true);
            var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
            clips.First(c=>c.name.EndsWith("Idle")).SampleAnimation(a.gameObject,0);
            float footHeight=Geometry(a.gameObject).min.y;
            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
            visual.transform.position-=Vector3.up*footHeight;
            PrefabUtility.SaveAsPrefabAsset(root,path);
            foreach(var clip in clips)
            {
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopBlendOrientation=true;settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                EditorUtility.SetDirty(clip);
                foreach(float fraction in new[]{0f,.5f,.9f}){clip.SampleAnimation(a.gameObject,clip.length*fraction);var b=Geometry(a.gameObject);Log(name+" "+clip.name+" @"+fraction+" min="+b.min+" max="+b.max);}
            }
            foreach(var clip in sourceClips.Where(c=>c.name.ToLowerInvariant().Contains("idle"))){clip.SampleAnimation(a.gameObject,0);Log(name+" SOURCE "+clip.name+" geometry="+Geometry(a.gameObject));}
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BindAttack(UnityEditor.Animations.AnimatorController controller,string stateName,string sourceName)
    {
        string path="Assets/03_Models/Bosses/Ork berserker/animation/ork@"+sourceName+".fbx";
        var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
        string output="Assets/01_Prefabs/Bosses/OrkBerserker"+stateName+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
        if(clip)EditorUtility.CopySerialized(source,clip);else{clip=Object.Instantiate(source);AssetDatabase.CreateAsset(clip,output);}
        clip.name="OrkBerserker"+stateName;
        AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlendOrientation=true;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==stateName)??machine.AddState(stateName);
        state.motion=clip;state.speedParameter="ActionSpeed";state.speedParameterActive=true;EditorUtility.SetDirty(clip);EditorUtility.SetDirty(controller);
        Log("BOUND "+stateName+" to "+path);
    }
    static Vector3 Floor(Vector3 p)
    {
        Require(Physics.Raycast(new Vector3(p.x,10,p.z),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore),"physical floor at "+p);
        return hit.point;
    }
    public static void Repair()
    {
        try
        {
            File.WriteAllText(Report,"");
            Require(!Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty),"open scenes saved");
            foreach(string name in new[]{"MonsterOrc","MonsterMutant","OrkBerserker"})RepairBoss(name);
            var scene=EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/FinalArena.unity");var run=Object.FindFirstObjectByType<RunScene>();
            run.player.gameObject.SetActive(false);
            var arena=scene.GetRootGameObjects().First(g=>g.name.StartsWith("FST"));ConvertMaterials(arena);
            foreach(var filter in arena.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh||filter.sharedMesh.vertexCount==0)continue;
                var collider=filter.GetComponent<MeshCollider>();if(!collider)collider=filter.gameObject.AddComponent<MeshCollider>();
                // A convex hull seals the open arena above its floor, trapping spawns inside it.
                collider.convex=false;collider.isTrigger=false;collider.enabled=true;collider.sharedMesh=filter.sharedMesh;
            }
            Physics.SyncTransforms();
            var playerFloor=Floor(new Vector3(0,0,-2.8f));var bossFloor=Floor(new Vector3(0,0,2.2f));
            Log("FLOORS player="+playerFloor+" boss="+bossFloor);
            var surfaces=Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);foreach(var s in surfaces)s.RemoveData();
            var surface=surfaces.First(s=>s.enabled);surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
            string navPath="Assets/01_Prefabs/Bosses/FinalArenaRunNavigation.asset";var saved=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(saved){var built=surface.navMeshData;surface.RemoveData();EditorUtility.CopySerialized(built,saved);surface.navMeshData=saved;surface.AddData();EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            Require(NavMesh.SamplePosition(playerFloor,out var start,.5f,NavMesh.AllAreas),"player floor has navmesh");
            Require(NavMesh.SamplePosition(bossFloor,out var spawn,.5f,NavMesh.AllAreas),"boss floor has navmesh");
            var route=new NavMeshPath();Require(NavMesh.CalculatePath(start.position,spawn.position,NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete,"boss can reach player");
            run.player.transform.position=playerFloor+Vector3.up*.08f;run.arena.spawnPoint.position=spawn.position;
            for(int i=0;i<run.lootMarkers.Length;i++){var direction=Quaternion.Euler(0,i*360f/run.lootMarkers.Length,0)*Vector3.forward*3;run.lootMarkers[i].position=Floor(direction)+Vector3.up*.1f;}
            run.player.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Log("REPAIR_DONE");
            PlayerPrefs.SetInt("Codice.TestRound",20);SessionState.SetBool("ArenaVisual.Test",true);EditorApplication.isPlaying=true;
        }
        catch(Exception e){Log("FAILED "+e);Debug.LogException(e);}
    }
    static async System.Threading.Tasks.Task Until(Func<bool> condition)
    {
        double deadline=EditorApplication.timeSinceStartup+30;
        while(!condition()){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Runtime loading timeout");await System.Threading.Tasks.Task.Delay(50);}
    }
    static async void PlayCheck()
    {
        try
        {
            Application.runInBackground=true;
            await System.Threading.Tasks.Task.Delay(1000);
            EditorApplication.update+=Pump;
            await Until(()=>RunDirector.Instance&&RunDirector.Instance.CombatActive&&RunDirector.Instance.Level==20);
            var run=Object.FindFirstObjectByType<RunScene>();var player=run.player;var boss=run.arena.ActiveBoss;
            Require(boss&&boss.kind==BossKind.OrkBerserker,"final boss spawns on round 20");
            boss.SetPaused(true);player.enabled=false;player.GetComponent<Codice.Characters.PlayerCombat>().enabled=false;
            var cc=player.GetComponent<CharacterController>();var start=player.transform.position;
            // Move the real player capsule under gravity across the combat floor.
            for(int side=0;side<4;side++)
            {
                var direction=new[]{Vector3.right,Vector3.forward,Vector3.left,Vector3.back}[side];
                for(int frame=0;frame<90;frame++){cc.Move((direction*1.5f+Vector3.down*3)/60f);await System.Threading.Tasks.Task.Delay(16);Require(player.transform.position.y>start.y-.4f,"floor supports player step "+side+"/"+frame);}
            }
            Require(cc.isGrounded,"player remains grounded after walking");
            boss.gameObject.SetActive(false);
            foreach(string name in new[]{"OrkBerserker","MonsterMutant","MonsterOrc"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<BossCharacter>("Assets/01_Prefabs/Bosses/"+name+".prefab");
                var actor=Object.Instantiate(prefab,run.arena.spawnPoint.position,Quaternion.identity);actor.Initialize(player,20);actor.SetPaused(true);
                await System.Threading.Tasks.Task.Delay(100);
                var a=actor.visual.GetComponentInChildren<Animator>();var presentation=actor.GetComponent<Codice.Presentation.ActorPresentation>();presentation.enabled=false;
                foreach(var state in new[]{"Idle","Chase","Melee","Shoot","Hit","Death"}.Concat(name=="OrkBerserker"?new[]{"Slam"}:Array.Empty<string>()))
                {
                    a.speed=1;a.SetFloat("ActionSpeed",1);a.Play(state,0,.35f);a.Update(0);
                    var b=Geometry(a.gameObject);Log("RUNTIME "+name+" "+state+" feet="+(b.min.y-actor.transform.position.y)+" center="+(b.center-actor.transform.position));
                    Capture(actor,name+state,b);
                    foreach(var r in actor.visual.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader.isSupported||!m.shader.name.StartsWith("Universal Render Pipeline/"))Log("INVALID MATERIAL "+name+" "+r.name+" "+(m?m.name+" / "+m.shader.name:"NULL"));
                    Require(actor.visual.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.isSupported&&m.shader.name.StartsWith("Universal Render Pipeline/"))),name+" materials "+state);
                    if(state!="Death")Require(Mathf.Abs(b.min.y-actor.transform.position.y)<.2f,name+" grounded "+state);
                    if(name=="OrkBerserker"&&(state=="Melee"||state=="Slam"))foreach(float frame in new[]{0f,.2f,.5f,.75f,.95f}){a.Play(state,0,frame);a.Update(0);Capture(actor,name+state+Mathf.RoundToInt(frame*100),Geometry(a.gameObject));}
                }
                Object.Destroy(actor.gameObject);
            }
            Log("PHYSICS_AND_MATERIALS_PASSED");
        }
        catch(Exception e){Log("FAILED "+e);Debug.LogException(e);}
        finally{EditorApplication.update-=Pump;Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
    static void Pump(){EditorApplication.QueuePlayerLoopUpdate();}
    static void Capture(BossCharacter actor,string name,Bounds bounds)
    {
        var go=new GameObject("Diagnostic camera");var camera=go.AddComponent<Camera>();
        camera.transform.position=bounds.center+new Vector3(0,bounds.size.y*.15f,-bounds.size.y*1.7f);camera.transform.LookAt(bounds.center);
        var rt=new RenderTexture(800,800,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(800,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,800,800),0,0);texture.Apply();File.WriteAllBytes("Library/"+name+"Check.png",texture.EncodeToPNG());
        RenderTexture.active=old;camera.targetTexture=null;Object.Destroy(rt);Object.Destroy(texture);Object.Destroy(go);
    }
}
