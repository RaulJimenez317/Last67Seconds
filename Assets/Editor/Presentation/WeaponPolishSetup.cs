using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Codice.Presentation;
using Object=UnityEngine.Object;
public static class WeaponPolishSetup
{
    const string Output="Assets/Resources/WeaponTiers";
    const string N="Assets/03_Models/nappin/WeaponStylizedPack/Prefabs/(Prb)";
    const string P="Assets/03_Models/nappin/PolyRonin/Samurai Warrior Weapons Pack/Prefabs/";
    const string K="Assets/03_Models/Weapons/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/";
    static string Blink(string name)=>AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/03_Models/Weapons/Blink"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==name);
    static string Kay(string name)=>K+name+".fbx";
    [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists("Library/WeaponPolish.request")||Application.isPlaying)return;var mode=File.ReadAllText("Library/WeaponPolish.request").Trim();File.Delete("Library/WeaponPolish.request");try{if(mode=="laser")Laser();else Build();File.WriteAllText("Library/WeaponPolish.txt","BUILD PASSED");}catch(Exception e){File.WriteAllText("Library/WeaponPolish.txt",e.ToString());Debug.LogException(e);}};}
    [MenuItem("Codice/Presentation/Build weapon tiers")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();
        string[][] sources={
            new[]{N+"ShortSword.prefab",Blink("Sword1_1_3"),Blink("Sword3_1_3"),Blink("Sword5_3_2")},
            new[]{Kay("axe_A"),Blink("AxeBasic1_2"),Blink("AxeBasic2_1"),Blink("AxeEvolving3_3_2")},
            new[]{Kay("bow_A_withString"),N+"Bow.prefab",P+"Yumi.prefab",Kay("bow_B_withString")},
            new[]{N+"Dagger.prefab",P+"Kunai.prefab",P+"Tanto.prefab",Blink("Dagger4_1_3")},
            new[]{Kay("hammer_A"),Kay("hammer_B"),Kay("hammer_C"),Blink("Hammer1_1_3")},
            new[]{Kay("spear_A"),P+"Yari.prefab",P+"Naginata.prefab",Blink("Polearm2_2_2")},
            new[]{Blink("Scythe1_3_2"),Blink("Scythe1_3_2"),Blink("Scythe1_3_2"),Blink("Scythe1_3_2")},
            new[]{Blink("Shield3_1_1"),Kay("shield_A"),Blink("Shield2_1_2"),Kay("shield_C")},
            new[]{Kay("staff_A"),Blink("Staff2_2_6"),Blink("Staff4_1_1"),Blink("Staff5_1_1")},
            new[]{Blink("Musket1_2_1"),Blink("Musket1_2_1"),Blink("Musket1_2_1"),Blink("Musket1_2_1")}
        };
        float[] sizes={1.15f,1.25f,1.1f,.48f,1.4f,1.65f,1.65f,.65f,1.55f,1.05f};
        var catalog=AssetDatabase.LoadAssetAtPath<WeaponVisualCatalog>("Assets/Resources/WeaponVisualCatalog.asset");if(!catalog){catalog=ScriptableObject.CreateInstance<WeaponVisualCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/WeaponVisualCatalog.asset");}
        for(int kind=0;kind<10;kind++)for(int tier=1;tier<=4;tier++)catalog.tiers[kind*4+tier-1]=Make(sources[kind][tier-1],((Codice.Characters.WeaponKind)kind)+"_T"+tier,sizes[kind],kind,tier);
        catalog.katana=Make(P+"Katana.prefab","Katana",1.2f,0,4);catalog.shuriken=Make(N+"Shuriken.prefab","Shuriken",.18f,-1,1);
        // Keep stable resource paths used by projectiles and the initial Shinobi loadout.
        var blade=Object.Instantiate(catalog.katana);PrefabUtility.SaveAsPrefabAsset(blade,"Assets/Resources/Katana.prefab");Object.DestroyImmediate(blade);
        var temp=Object.Instantiate(catalog.shuriken);PrefabUtility.SaveAsPrefabAsset(temp,"Assets/Resources/Shuriken.prefab");Object.DestroyImmediate(temp);
        EditorUtility.SetDirty(catalog);
        Laser();
        var paths=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/07_Animacion"}).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        File.WriteAllLines("Library/AvailableWeaponClips.txt",paths.SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>p+" | "+c.name)));
        Animations();
        AssetDatabase.SaveAssets();
        Review();
    }
    static void Laser()
    {
        const string texturePath="Assets/Resources/StaffLaserProfile.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(!texture){texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Staff laser soft profile";texture.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).sqrMagnitude;float value=Mathf.Max(0,(Mathf.Exp(-5*r)-Mathf.Exp(-5))/(1-Mathf.Exp(-5)));texture.SetPixel(x,y,new Color(value,value,value,value));}texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);}
        var laser=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/StaffLaser.mat");if(!laser){laser=new Material(Shader.Find("VolumetricLine/SingleLine-LightSaber"));AssetDatabase.CreateAsset(laser,"Assets/Resources/StaffLaser.mat");}laser.mainTexture=texture;laser.SetColor("_Color",new Color(.1f,.55f,1));laser.SetFloat("_LineWidth",.28f);laser.SetFloat("_LightSaberFactor",.8f);EditorUtility.SetDirty(laser);AssetDatabase.SaveAssets();
    }
    static void Animations()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Presentation/Animations/KayKit.controller");
        var sm=controller.layers[0].stateMachine;
        string[,] entries={{"ThrowShuriken","General","Throw"},{"StaffIdle","CombatMelee","Melee_2H_Idle"},{"StaffCast","CombatRanged","Ranged_Magic_Spellcasting"},{"StaffChannel","CombatRanged","Ranged_Magic_Spellcasting_Long"},{"KatanaSlashA","CombatMelee","Melee_1H_Attack_Slice_Diagonal"},{"KatanaSlashB","CombatMelee","Melee_2H_Attack_Slice"}};
        for(int i=0;i<entries.GetLength(0);i++){
            string name=entries[i,0],path="Assets/07_Animacion/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_"+entries[i,1]+".fbx";
            var clip=Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name==entries[i,2]));clip.name=name;AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=name=="StaffIdle";settings.loopBlend=settings.loopTime;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string output="Assets/Presentation/Animations/KayKit_"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
            if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,output);
            var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name);if(!state)state=sm.AddState(name);state.motion=clip;state.speedParameter="ActionSpeed";state.speedParameterActive=name!="StaffIdle";
        }
        EditorUtility.SetDirty(controller);
    }
    static void Review()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        try{
            var cameraObject=new GameObject("Review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=10.7f;camera.transform.position=new Vector3(0,0,-20);camera.backgroundColor=new Color(.08f,.09f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;
            var lightObject=new GameObject("Review light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(30,-30,0);
            var catalog=WeaponVisualCatalog.Load();for(int i=0;i<42;i++){var asset=i<40?catalog.tiers[i]:i==40?catalog.katana:catalog.shuriken;var obj=Object.Instantiate(asset);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);obj.transform.position=new Vector3((i%4-1.5f)*2.5f,9.4f-i/4*1.9f,0);obj.transform.rotation=Quaternion.Euler(0,-20,0);foreach(var r in obj.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader.isSupported)throw new Exception("Unsupported material "+asset.name);}
            var rt=new RenderTexture(1200,2200,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1200,2200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,2200),0,0);image.Apply();File.WriteAllBytes("Library/WeaponTierReview.png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
        }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }
    static GameObject Make(string source,string name,float length,int kind,int tier)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!asset)throw new Exception("Missing "+source);
        var root=new GameObject(name);try{
            var model=Object.Instantiate(asset,root.transform);model.name=Path.GetFileNameWithoutExtension(source);model.transform.localPosition=Vector3.zero;
            foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            var renderers=model.GetComponentsInChildren<Renderer>();
            Bounds Bounds(){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
            var b=Bounds();var axis=b.size.x>b.size.y&&b.size.x>b.size.z?Vector3.right:b.size.y>b.size.z?Vector3.up:Vector3.forward;
            if(kind>=0)model.transform.rotation=Quaternion.FromToRotation(axis,Vector3.up)*model.transform.rotation;
            if(source.StartsWith(P))model.transform.rotation=Quaternion.AngleAxis(90,Vector3.up)*model.transform.rotation;
            if(source.Contains("Blink")&&(kind==3||kind==9))model.transform.rotation=Quaternion.AngleAxis(180,Vector3.forward)*model.transform.rotation;
            if(kind==7)model.transform.rotation=Quaternion.AngleAxis(180,Vector3.up)*model.transform.rotation;
            if(kind==-1){b=Bounds();var normal=b.size.x<b.size.y&&b.size.x<b.size.z?Vector3.right:b.size.y<b.size.z?Vector3.up:Vector3.forward;model.transform.rotation=Quaternion.FromToRotation(normal,Vector3.forward)*model.transform.rotation;}
            b=Bounds();model.transform.localScale*=length/Mathf.Max(b.size.x,b.size.y,b.size.z);b=Bounds();
            bool centered=kind==2||kind==7||kind==-1;
            model.transform.position-=centered?b.center:new Vector3(b.center.x,b.min.y+(kind==9?.25f:kind==8?.5f:kind==5||kind==6?.42f:.12f)*length,b.center.z);
            if(kind==8||kind==9){var muzzle=new GameObject("Muzzle");muzzle.transform.SetParent(root.transform,false);b=Bounds();muzzle.transform.localPosition=new Vector3(b.center.x,b.max.y,b.center.z);}
            foreach(var r in renderers){r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            // A single scythe mesh exists: tier silhouettes gain a pommel and successive blade ornaments.
            if((kind==6&&tier>1))for(int i=0;i<tier-1;i++){
                var gem=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(gem.GetComponent<Collider>());gem.name="Tier ornament";gem.transform.SetParent(root.transform,false);gem.transform.localScale=new Vector3(.07f,.12f,.07f);gem.transform.localRotation=Quaternion.Euler(0,0,45);gem.transform.localPosition=new Vector3(.07f*(i%2==0?1:-1),length*(.35f+i*.085f),0);gem.GetComponent<Renderer>().sharedMaterial=Accent(tier);}
            return PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+name+".prefab");
        }finally{Object.DestroyImmediate(root);}
    }
    static Material Accent(int tier){string path=Output+"/Rune"+tier+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit"));Color c=tier==4?new Color(1,.45f,.08f):tier==3?new Color(.65f,.12f,1):Color.cyan;m.color=c;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.5f);AssetDatabase.CreateAsset(m,path);return m;}
    static Material Convert(Material original)
    {
        if(!original)return Accent(2);if(original.shader.name.StartsWith("Universal Render Pipeline/"))return original;
        string path=Output+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=original.name+" URP";m.color=original.HasProperty("_Color")?original.color:Color.white;m.mainTexture=original.mainTexture;m.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(m,path);return m;
    }
}
