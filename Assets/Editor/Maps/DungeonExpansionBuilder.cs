using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Authors two ordinary scenes; never regenerates the player's existing maps.
public static class DungeonExpansionBuilder
{
    const string Maps="Assets/00_Scenes/Maps/";
    const string Pack="Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string Effects="Assets/06_Effects/Combat/";
    static Transform architecture,props;
    static Material atlas;
    [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(File.Exists("Library/ExpansionPolish.request")&&!EditorApplication.isPlaying){File.Delete("Library/ExpansionPolish.request");Polish();}else if(File.Exists("Library/ExpansionBuild.request")&&!EditorApplication.isPlaying){File.Delete("Library/ExpansionBuild.request");Build();}};}
    static void Polish()
    {
        try{
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before polish");
            foreach(string name in new[]{"Criptas","Bastion"}){
                var scene=EditorSceneManager.OpenScene(Maps+name+".unity");
                architecture=scene.GetRootGameObjects().First(g=>g.name==name+" / Architecture").transform;
                props=scene.GetRootGameObjects().First(g=>g.name=="Set dressing").transform;
                atlas=AssetDatabase.LoadAssetAtPath<Material>(Maps+"Materials/DungeonAtlas.mat");
                if(!GameObject.Find("Expansion details")){
                    var details=new GameObject("Expansion details").transform;details.SetParent(props);props=details;
                    foreach(int sign in new[]{-1,1}){
                        foreach(int x in new[]{-30,-14,14,30}){
                            Model("wall_pillar",new Vector3(x,0,sign*33),new Vector3(.8f,5,.8f),0,true,props);
                            Model(name=="Criptas"?"banner_patternB_blue":"banner_patternB_red",new Vector3(x,2,sign*32.45f),new Vector3(1.3f,2,.15f),sign>0?0:180,false,props);
                        }
                        foreach(int z in new[]{-26,-10,10,26})Model("crates_stacked",new Vector3(sign*32.7f,0,z),new Vector3(1.3f,1.6f,1.3f),0,true,props);
                        Model("table_long_decorated_A",new Vector3(sign*9,0,0),new Vector3(1.4f,1.15f,3),90,true,props);
                    }
                    Model("table_medium_decorated_A",new Vector3(-6,0,-8),new Vector3(2,1,1),0,true,props);
                    if(name=="Criptas")foreach(int x in new[]{-10,10})foreach(int z in new[]{-10,10})Model("pillar_decorated",new Vector3(x,0,z),new Vector3(1,5,1),0,true,props);
                }
                Bake(Object.FindFirstObjectByType<RunScene>(),name);EditorSceneManager.SaveScene(scene);Screenshot(name);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");Log("POLISH_AND_NAVIGATION_PASSED");
        }catch(Exception e){Log("FAILED POLISH: "+e);Debug.LogException(e);}
    }
    [MenuItem("Codice/Maps/Create dungeon expansion")]
    public static void Build()
    {
        try{
            if(EditorApplication.isPlaying||Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save scene edits and exit Play before building.");
            File.WriteAllText("Library/ExpansionBuild.txt", "EXPANSION\n");
            if(File.Exists(Maps+"Criptas.unity")||File.Exists(Maps+"Bastion.unity"))throw new Exception("Expansion scenes already exist; edit them directly instead of regenerating.");
            BuildEffects();
            BuildMap("Criptas",false);BuildMap("Bastion",true);Polish();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/00_Scenes/MainMenu.unity");
            Log("EXPANSION_BUILD_PASSED");
        }catch(Exception e){Log("FAILED: "+e);Debug.LogException(e);}
    }
    static void Log(string s){File.AppendAllText("Library/ExpansionBuild.txt",s+"\n");Debug.Log(s);}
    static Transform Marker(string name,Vector3 p,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent);t.position=p;return t;}
    static void BuildMap(string name,bool bastion)
    {
        string output=Maps+name+".unity";
        if(File.Exists(output))throw new Exception("Scene already exists; refusing to overwrite "+output);
        var scene=EditorSceneManager.OpenScene(Maps+"Calabozo.unity");
        // All gameplay objects and references are copied from the established dungeon.
        EditorSceneManager.SaveScene(scene,output);
        var run=Object.FindFirstObjectByType<RunScene>();
        foreach(var root in scene.GetRootGameObjects())if(root.name=="Calabozo / Architecture"||root.name=="Set dressing")Object.DestroyImmediate(root);
        architecture=new GameObject(name+" / Architecture").transform;props=new GameObject("Set dressing").transform;
        atlas=AssetDatabase.LoadAssetAtPath<Material>(Maps+"Materials/DungeonAtlas.mat");
        var cells=new HashSet<Vector2Int>();
        for(int x=-9;x<=9;x++)for(int z=-9;z<=9;z++){
            int ax=Math.Abs(x),az=Math.Abs(z);
            // Crypts: four chambers joined by an outer loop and broad cross.
            // Bastion: central great hall, two side galleries and north/south links.
            bool floor=bastion ? (ax<=3&&az<=6)||(ax>=7&&ax<=8&&az<=8)||(az>=7&&az<=8&&ax<=8)||(az<=1&&ax<=8)
                : (ax<=3&&az<=3)||(ax<=1&&az<=8)||(az<=1&&ax<=8)||(ax>=5&&ax<=8&&az>=5&&az<=8)||(ax>=7&&ax<=8&&az<=8)||(az>=7&&az<=8&&ax<=8);
            if(floor)cells.Add(new Vector2Int(x,z));
        }
        Vector2Int[] sides={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        foreach(var cell in cells.OrderBy(c=>c.x).ThenBy(c=>c.y)){
            var p=new Vector3(cell.x*4,0,cell.y*4);
            Model(bastion&&Math.Abs(cell.x)>=7?"floor_wood_large":"floor_tile_large",p-Vector3.up*.35f,new Vector3(4,.35f,4));
            for(int side=0;side<4;side++)if(!cells.Contains(cell+sides[side])){
                Vector3 delta=new Vector3(sides[side].x,0,sides[side].y);
                Model("wall",p+delta*2,new Vector3(4,4.5f,.55f),side*90);
                if((cell.x+cell.y)%4==0){var t=Model("torch_mounted",p+delta*1.65f+Vector3.up*1.7f,new Vector3(.3f,1.1f,.35f),side*90,false,props);var light=new GameObject("Torch glow").AddComponent<Light>();light.transform.SetParent(t.transform);light.transform.position=t.transform.position+Vector3.up*.8f-delta*.3f;light.type=LightType.Point;light.range=9;light.intensity=2.5f;light.color=bastion?new Color(1,.36f,.09f):new Color(.25f,.65f,1);}
            }
        }
        if(bastion){foreach(int x in new[]{-8,8})foreach(int z in new[]{-16,0,16})Model("pillar_decorated",new Vector3(x,0,z),new Vector3(1.2f,5,1.2f));}
        else{foreach(int x in new[]{-24,24})foreach(int z in new[]{-24,24}){Model("column",new Vector3(x,0,z),new Vector3(1.8f,1.1f,2.7f));Model("chest",new Vector3(x,1.1f,z),new Vector3(1.2f,.8f,1),0,true,props);}}
        var points=new GameObject(name+" gameplay points").transform;
        run.player.transform.position=new Vector3(0,.1f,-8);run.player.transform.rotation=Quaternion.identity;
        run.dungeon.merchant.transform.position=new Vector3(-6,.05f,-6);
        run.arena.spawnPoint=Marker("Boss center",new Vector3(0,.1f,6),points);
        run.portal.transform.position=new Vector3(6,.05f,-6);
        Vector3[] positions=bastion?new[]{new Vector3(-30,0,-20),new Vector3(30,0,-20),new Vector3(-30,0,20),new Vector3(30,0,20),new Vector3(0,0,30),new Vector3(0,0,-30),new Vector3(-24,0,0),new Vector3(24,0,0)}:new[]{new Vector3(-24,0,-28),new Vector3(24,0,-28),new Vector3(-24,0,28),new Vector3(24,0,28),new Vector3(0,0,28),new Vector3(0,0,-28),new Vector3(-28,0,0),new Vector3(28,0,0)};
        run.dungeon.spawnPoints=positions.Select((p,i)=>Marker("Enemy entry "+i,p,points)).ToArray();
        run.lootMarkers=positions.Concat(new[]{new Vector3(-8,0,8),new Vector3(8,0,8),new Vector3(4,0,-8),new Vector3(-4,0,-8)}).Select((p,i)=>Marker("Loot "+i,p+Vector3.up*.15f,points)).ToArray();
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=bastion?new Color(.33f,.29f,.25f):new Color(.28f,.34f,.43f);
        RenderSettings.fog=false;
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.color=bastion?new Color(1,.81f,.61f):new Color(.66f,.8f,1);light.intensity=1.1f;}
        Bake(run,name);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(!EditorBuildSettings.scenes.Any(s=>s.path==output))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(output,true)}).ToArray();
        Screenshot(name);
        Log(name+": "+cells.Count+" floor cells, 8 connected enemy entries, merchant, boss, portal and 12 loot points.");
    }
    static GameObject Model(string asset,Vector3 position,Vector3 size,float yaw=0,bool solid=true,Transform parent=null)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+asset+".fbx");if(!source)throw new Exception(asset);
        var root=new GameObject(asset);root.transform.SetParent(parent?parent:architecture);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
        var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);model.transform.localScale=scale;model.transform.localPosition=-Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
        foreach(var r in renderers)r.sharedMaterials=Enumerable.Repeat(atlas,r.sharedMaterials.Length).ToArray();foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        if(solid){var collider=root.AddComponent<BoxCollider>();collider.size=size;collider.center=Vector3.up*size.y/2;}
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));root.isStatic=true;return root;
    }
    static void Bake(RunScene run,string name)
    {
        foreach(var old in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include,FindObjectsSortMode.None)){old.RemoveData();Object.DestroyImmediate(old);}
        var surface=new GameObject(name+" navigation").AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        run.player.gameObject.SetActive(false);run.dungeon.merchant.gameObject.SetActive(false);Physics.SyncTransforms();surface.BuildNavMesh();
        string navPath=Maps+name+"Navigation.asset";var saved=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if(saved){EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
        run.player.gameObject.SetActive(true);run.dungeon.merchant.gameObject.SetActive(true);
        if(!NavMesh.SamplePosition(run.player.transform.position,out var start,1,NavMesh.AllAreas))throw new Exception(name+" player off navmesh");
        foreach(var t in run.dungeon.spawnPoints.Concat(run.lootMarkers).Concat(new[]{run.arena.spawnPoint,run.portal.transform,run.dungeon.merchant.transform})){
            if(!NavMesh.SamplePosition(t.position,out var hit,1,NavMesh.AllAreas))throw new Exception(name+" off navmesh: "+t.name);
            var route=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)throw new Exception(name+" unreachable: "+t.name);
            if(!Physics.Raycast(t.position+Vector3.up,Vector3.down,2,~0,QueryTriggerInteraction.Ignore))throw new Exception(name+" missing floor: "+t.name);
        }
    }
    static void Screenshot(string name)
    {
        var go=new GameObject("Review camera");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(48,72,-65);camera.transform.LookAt(Vector3.zero);camera.farClipPlane=220;camera.backgroundColor=new Color(.025f,.03f,.05f);camera.clearFlags=CameraClearFlags.SolidColor;
        var rt=new RenderTexture(1200,900,24);var tex=new Texture2D(1200,900,TextureFormat.RGB24,false);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,900),0,0);tex.Apply();File.WriteAllBytes("Library/"+name+"Overview.png",tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(go);Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
    }
    static void BuildEffects()
    {
        Directory.CreateDirectory(Effects);AssetDatabase.Refresh();var library=AssetDatabase.LoadAssetAtPath<PresentationLibrary>("Assets/Resources/Presentation/Library.asset");
        string bloodSource="Assets/06_Effects/Vefects/Free Blood VFX/VFX/Particles/Once/VFX_Splat_01_Floor_Rot_Once.prefab";
        var blood=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(bloodSource));blood.name="Blood impact";
        // Preserve the package URP shader and its packed texture/vertex-stream animation.
        var converted=new Dictionary<Material,Material>();
        foreach(var renderer in blood.GetComponentsInChildren<ParticleSystemRenderer>(true)){
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                if(!m)return m;if(converted.TryGetValue(m,out var saved))return saved;
                var material=new Material(m);material.name=m.name+" Combat";
                AssetDatabase.CreateAsset(material,Effects+material.name+".mat");converted[m]=material;return material;
            }).ToArray();
        }
        foreach(var ps in blood.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.loop=false;main.playOnAwake=true;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.stopAction=ParticleSystemStopAction.None;}
        foreach(var ps in blood.GetComponentsInChildren<ParticleSystem>()){var r=ps.GetComponent<ParticleSystemRenderer>();if(r.renderMode==ParticleSystemRenderMode.None)continue;r.alignment=ParticleSystemRenderSpace.View;ps.transform.localRotation=Quaternion.identity;var main=ps.main;main.startRotation3D=false;main.startRotation=0;main.startLifetime=.45f;var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;}
        library.blood=PrefabUtility.SaveAsPrefabAsset(blood,Effects+"BloodImpact.prefab");Object.DestroyImmediate(blood);
        // Reuse the project's smoke texture with a short directional gunpowder burst.
        var smokeMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));smokeMat.name="Gunpowder";Transparent(smokeMat);smokeMat.SetColor("_BaseColor",Color.white);
        var dust=library.dust.GetComponentInChildren<ParticleSystemRenderer>();if(dust&&dust.sharedMaterial){var m=dust.sharedMaterial;smokeMat.SetTexture("_BaseMap",m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):m.mainTexture);}
        AssetDatabase.CreateAsset(smokeMat,Effects+"Gunpowder.mat");
        library.gunSmoke=ParticlePrefab("GunSmoke",smokeMat,new Color(.6f,.57f,.52f,.35f),.16f,.65f,1.6f,12,false);
        var magicMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));magicMat.name="Arcane ember";Transparent(magicMat);magicMat.SetTexture("_BaseMap",smokeMat.GetTexture("_BaseMap"));magicMat.SetColor("_BaseColor",new Color(2,.45f,.08f,1));AssetDatabase.CreateAsset(magicMat,Effects+"ArcaneEmber.mat");
        library.magicProjectile=ParticlePrefab("StaffProjectile",magicMat,new Color(1,.45f,.1f,1),.16f,.28f,.12f,0,true);
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Log("Blood, gunpowder smoke and staff projectile saved with URP materials.");
    }
    static void Transparent(Material m){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");}
    static GameObject ParticlePrefab(string name,Material material,Color color,float size,float lifetime,float speed,int count,bool loop)
    {
        var go=new GameObject(name);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.duration=.2f;main.loop=loop;main.startLifetime=lifetime;main.startSpeed=speed;main.startSize=size;main.startColor=color;main.maxParticles=64;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=loop?60:0;if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18;shape.radius=.04f;
        var alpha=ps.colorOverLifetime;alpha.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});alpha.color=gradient;
        var grow=ps.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,loop?.1f:5));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        var saved=PrefabUtility.SaveAsPrefabAsset(go,Effects+name+".prefab");Object.DestroyImmediate(go);return saved;
    }
}
