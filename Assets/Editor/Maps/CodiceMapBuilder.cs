using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only authoring: the saved scenes contain no runtime generator or gameplay.
public static class CodiceMapBuilder
{
    const string Pack = "Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/";
    const string Output = "Assets/00_Scenes/Maps";
    static Material atlas, stone, sand, trim;
    static Transform architecture, props, markers;

    [MenuItem("Codice/Maps/Rebuild both maps (overwrites map scenes)")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Output + "/Materials");
        AssetDatabase.Refresh();
        atlas = Material("DungeonAtlas", Color.white);
        atlas.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "textures/dungeon_texture.png");
        EditorUtility.SetDirty(atlas);
        stone = Material("AshStone", new Color(.24f,.27f,.30f));
        sand = Material("ArenaSand", new Color(.48f,.35f,.23f));
        trim = Material("ImperialTrim", new Color(.46f,.32f,.12f));
        Dungeon();
        Coliseum();
        AssetDatabase.SaveAssets();
        Debug.Log("MAP_BUILD_OK: Calabozo and Coliseo saved and validated.");
    }

    static Material Material(string name, Color color)
    {
        string path = Output + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.color = color;
        m.SetTexture("_BaseMap", Texture2D.whiteTexture);
        EditorUtility.SetDirty(m);
        m.SetFloat("_Smoothness", .12f);
        return m;
    }

    static Transform Group(string name) => new GameObject(name).transform;
    static void StartScene(string name, bool dungeon)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        architecture = Group(name + " / Architecture"); props = Group("Set dressing"); markers = Group("Integration markers (no behaviour)");
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = dungeon ? new Color(.30f,.34f,.43f) : new Color(.55f,.53f,.49f);
        RenderSettings.fog = false;
        var sun = new GameObject("Architectural light").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = dungeon ? 1.1f : 2f;
        sun.color = dungeon ? new Color(.72f,.81f,1) : new Color(1,.87f,.69f);
        sun.transform.rotation = Quaternion.Euler(48,-30,0); sun.shadows = LightShadows.Soft;
        var camera = new GameObject("Overview camera (replace on integration)").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(44,58,-52); camera.transform.LookAt(Vector3.zero);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.055f);
        camera.farClipPlane = 200;
    }

    // Fit the imported mesh bounds, rather than assuming the asset pack's pivots or units.
    static GameObject Model(string asset, Vector3 position, Vector3 size, float yaw = 0, bool solid = true, Transform parent = null)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "fbx(unity)/" + asset + ".fbx");
        if (!source) throw new InvalidOperationException("Missing StructuresItemsInMap model: " + asset);
        var root = new GameObject(asset); root.transform.SetParent(parent ? parent : architecture);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false);
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("No mesh: " + asset);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        Vector3 scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        model.transform.localScale = Vector3.Scale(model.transform.localScale,scale);
        model.transform.localPosition = -Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
        foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(atlas,r.sharedMaterials.Length).ToArray();
        foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        if (solid) { var c = root.AddComponent<BoxCollider>(); c.center = new Vector3(0,size.y/2,0); c.size = size; }
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0)); root.isStatic = true;
        return root;
    }

    static GameObject Block(string name, Vector3 position, Vector3 size, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
        g.transform.SetParent(architecture); g.transform.position = position; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = mat; g.isStatic = true; return g;
    }
    static void Marker(string name, Vector3 position)
    { var g = new GameObject(name); g.transform.SetParent(markers); g.transform.position = position; }
    static Vector3 Rotate(Vector3 p, float angle) => Quaternion.Euler(0,angle,0) * p;
    static void Torch(Vector3 p)
    {
        Model("torch_mounted",p,new Vector3(.35f,1.2f,.4f),0,false,props);
        var l = new GameObject("Warm architectural lamp").AddComponent<Light>(); l.transform.SetParent(props);
        l.transform.position = p + Vector3.up; l.type = LightType.Point; l.range = 9; l.intensity = 3;
        l.color = new Color(1,.57f,.23f); l.shadows = LightShadows.None;
    }

    static void Dungeon()
    {
        StartScene("Calabozo",true);
        var cells = new HashSet<Vector2Int>();
        for (int x=-7;x<=7;x++) for(int z=-7;z<=7;z++)
        {
            bool rotunda = x*x+z*z <= 10;
            bool passage = (Math.Abs(x)<=1 && Math.Abs(z)<=6) || (Math.Abs(z)<=1 && Math.Abs(x)<=6);
            bool crypt = (Math.Abs(x)>=5 && Math.Abs(x)<=7 && Math.Abs(z)<=2) || (Math.Abs(z)>=5 && Math.Abs(z)<=7 && Math.Abs(x)<=2);
            if (rotunda || passage || crypt) cells.Add(new Vector2Int(x,z));
        }
        var directions = new [] {Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down};
        foreach(var c in cells.OrderBy(c=>c.x).ThenBy(c=>c.y))
        {
            bool bridge = (Math.Abs(c.x)==4 && Math.Abs(c.y)<=1) || (Math.Abs(c.y)==4 && Math.Abs(c.x)<=1);
            Model(bridge ? "floor_wood_large" : "floor_tile_large",new Vector3(c.x*4,-.35f,c.y*4),new Vector3(4,.35f,4));
            foreach(var d in directions) if(!cells.Contains(c+d))
            {
                Vector3 p = new Vector3(c.x*4+d.x*2,0,c.y*4+d.y*2);
                Model(bridge ? "barrier" : "wall",p,new Vector3(4,bridge?1.2f:5,.6f),d.x!=0?90:0);
            }
        }
        // A clear central trading area, four radial routes and one crypt per wing.
        Marker("Merchant_Center",new Vector3(0,.05f,0)); Marker("Player_Entry",new Vector3(0,.1f,-8));
        Marker("Portal_ToColiseum",new Vector3(0,.1f,7));
        for(int wing=0;wing<4;wing++)
        {
            float a=wing*90;
            foreach(float side in new[]{-4.5f,4.5f})
                Model("pillar",Rotate(new Vector3(side,0,12),a),new Vector3(1,5,1),a);
            var lintel = Block("Open stone gateway",Rotate(new Vector3(0,4.7f,12),a),new Vector3(10,.6f,1),stone);
            lintel.transform.rotation = Quaternion.Euler(0,a,0);
            foreach(float x in new[]{-7f,7f})
            {
                Model("pillar_decorated",Rotate(new Vector3(x,0,8),a),new Vector3(1.2f,5,1.2f),a);
                Torch(Rotate(new Vector3(x,2,7.2f),a));
            }
            // Raised rear balcony and a gentle ramp: 2 m rise over 8 m run.
            var deck = Block("Crypt balcony",Rotate(new Vector3(0,1.8f,27.5f),a),new Vector3(16,.4f,4),stone);
            deck.transform.rotation = Quaternion.Euler(0,a,0);
            var ramp = Block("Walkable balcony ramp",Rotate(new Vector3(-4.5f, .9f,21.5f),a),new Vector3(3,.25f,8.25f),stone);
            ramp.transform.rotation = Quaternion.Euler(-14,a,0);
            for(int i=0;i<4;i++) Model("barrier",Rotate(new Vector3(-2+i*3,2,25.5f),a),new Vector3(3,1,.35f),a);
            Model("chest_gold",Rotate(new Vector3(4,0,22),a),new Vector3(1.5f,1,1),a+180,true,props);
            Model("column",Rotate(new Vector3(6,0,24),a),new Vector3(1.8f,1,1.8f),a,true,props);
            Model("banner_patternA_red",Rotate(new Vector3(0,2.5f,29.5f),a),new Vector3(2,2,.15f),a,false,props);
            Model("crates_stacked",Rotate(new Vector3(7,0,20),a),new Vector3(1.6f,1.7f,1.5f),a,true,props);
            Torch(Rotate(new Vector3(0,2.2f,28.8f),a));
            Marker("Loot_Crypt_"+wing,Rotate(new Vector3(6,1.1f,24),a));
            Marker("Ranged_Balcony_"+wing,Rotate(new Vector3(0,2.1f,27),a));
            Marker("Enemy_Entry_"+wing,Rotate(new Vector3(0,.1f,21),a));
        }
        Save("Calabozo");
    }

    static void Coliseum()
    {
        StartScene("Coliseo",false);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name="Circular arena / 40 m diameter";
        floor.transform.SetParent(architecture); floor.transform.position=new Vector3(0,-.3f,0); floor.transform.localScale=new Vector3(40,.3f,40);
        UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.AddComponent<MeshCollider>().sharedMesh=floor.GetComponent<MeshFilter>().sharedMesh;
        floor.GetComponent<Renderer>().sharedMaterial=sand; floor.isStatic=true;
        // Closed retaining ring, stepped imperial stands and a higher exterior arcade.
        const int sectors=48;
        for(int i=0;i<sectors;i++)
        {
            float a=i*360f/sectors;
            Model("wall",Rotate(new Vector3(0,0,20.6f),a),new Vector3(2.85f,2,.9f),a);
            for(int tier=0;tier<4;tier++)
            {
                float radius=22+tier*1.8f;
                var stand=Block("Imperial seating tier "+tier,Rotate(new Vector3(0,.6f+tier*.65f,radius),a),new Vector3(radius*.134f,1.2f+tier*1.3f,1.9f),stone);
                stand.transform.rotation=Quaternion.Euler(0,a,0);
            }
            Model("wall_arched",Rotate(new Vector3(0,0,29),a),new Vector3(3.9f,7,.8f),a);
            if(i%4==0)
            {
                Model("pillar_decorated",Rotate(new Vector3(0,0,28.4f),a),new Vector3(1.3f,8,1.3f),a);
                Model("banner_patternB_red",Rotate(new Vector3(0,4,27.8f),a),new Vector3(1.5f,2.8f,.15f),a,false,props);
            }
        }
        for(int i=0;i<4;i++)
        {
            Vector3 p=Rotate(new Vector3(7,0,7),i*90);
            var pillar=Model("pillar_decorated",p,new Vector3(2,5,2)); pillar.name="Cover_Pillar_"+i+" (static geometry)";
            Marker("Cover_Pillar_"+i,p);
        }
        for(int i=0;i<3;i++)
        {
            Vector3 p=Rotate(new Vector3(0,0,17),60+i*120);
            Model("column",p,new Vector3(1.7f,.7f,1.7f),0,true,props);
            Model("chest",p+Vector3.up*.7f,new Vector3(1.1f,.7f,.8f),0,true,props);
            Marker("Arrow_Supply_"+i,p+Vector3.up*1.5f);
        }
        Model("wall_doorway",new Vector3(0,0,-19.3f),new Vector3(7,6,1),0,false);
        Marker("Player_Entry",new Vector3(0,.1f,-15)); Marker("Boss_Center",new Vector3(0,.1f,0));
        Marker("Portal_Return",new Vector3(4,.1f,-16));
        Save("Coliseo");
    }

    static void ValidatePaths()
    {
        var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
        UnityEngine.AI.NavMeshBuilder.CollectSources(new Bounds(Vector3.zero, new Vector3(90,30,90)), ~0,
            UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0, new List<UnityEngine.AI.NavMeshBuildMarkup>(), sources);
        var settings = UnityEngine.AI.NavMesh.GetSettingsByID(0);
        settings.agentRadius = .4f; settings.agentHeight = 1.8f; settings.agentClimb = .35f; settings.agentSlope = 45;
        var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(90,30,90)),Vector3.zero,Quaternion.identity);
        if(!data) throw new InvalidOperationException("Navigation validation could not build geometry");
        var instance = UnityEngine.AI.NavMesh.AddNavMeshData(data);
        try
        {
            var entry = markers.Find("Player_Entry").position;
            if(!UnityEngine.AI.NavMesh.SamplePosition(entry,out var start,1,UnityEngine.AI.NavMesh.AllAreas))
                throw new InvalidOperationException("Player entry is not walkable");
            int count=0;
            foreach(Transform marker in markers)
            {
                if(!(marker.name.StartsWith("Enemy_") || marker.name.StartsWith("Ranged_") || marker.name.StartsWith("Merchant_") || marker.name.StartsWith("Portal_") || marker.name.StartsWith("Boss_"))) continue;
                var path = new UnityEngine.AI.NavMeshPath();
                if(!UnityEngine.AI.NavMesh.SamplePosition(marker.position,out var target,1,UnityEngine.AI.NavMesh.AllAreas) ||
                    !UnityEngine.AI.NavMesh.CalculatePath(start.position,target.position,UnityEngine.AI.NavMesh.AllAreas,path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Unreachable map location: "+marker.name+" target="+target.position+" start="+start.position+" path="+path.status);
                count++;
            }
            Debug.Log("MAP_ROUTES_OK: "+count+" paths for a 1.8 m tall, 0.8 m wide agent");
        }
        finally { instance.Remove(); UnityEngine.Object.DestroyImmediate(data); }
    }
    static void Save(string name)
    {
        Physics.SyncTransforms();
        foreach(Transform marker in markers)
        {
            if(!Physics.Raycast(marker.position+Vector3.up*1,Vector3.down,5))
                throw new InvalidOperationException("Marker has no supporting surface: "+marker.name);
        }
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
            if(renderer.sharedMaterials.Any(m=>!m || !m.shader)) throw new InvalidOperationException("Missing material: "+renderer.name);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Output+"/"+name+".unity");
        // Review images are generated outside Assets and do not ship as game content.
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("MapReview");
        var cam=Camera.main;
        var rt=new RenderTexture(1400,1000,24); cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
        var image=new Texture2D(1400,1000,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1400,1000),0,0); image.Apply();
        File.WriteAllBytes("MapReview/"+name+".png",image.EncodeToPNG());
        var originalPosition=cam.transform.position; var originalRotation=cam.transform.rotation;
        cam.transform.position=markers.Find("Player_Entry").position+Vector3.up*1.6f; cam.transform.LookAt(new Vector3(0,2.5f,4));
        cam.Render(); image.ReadPixels(new Rect(0,0,1400,1000),0,0); image.Apply(); File.WriteAllBytes("MapReview/"+name+"_Entry.png",image.EncodeToPNG());
        cam.transform.SetPositionAndRotation(originalPosition,originalRotation);
        cam.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        ValidatePaths();
        Debug.Log("MAP_VALIDATED "+name+": "+architecture.childCount+" architectural objects, "+markers.childCount+" supported markers");
    }
}
