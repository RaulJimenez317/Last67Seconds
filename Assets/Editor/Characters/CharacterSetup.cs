using System;
using System.IO;
using System.Linq;
using Codice.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CharacterSetup
{
    const string Pack = "Assets/03_Models/Characters/KayKit_Adventurers_2.0_FREE/";
    const string Output = "Assets/01_Prefabs/Characters";

    [MenuItem("Codice/Characters/Integrate player and merchant into existing maps")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        var player = BuildPlayer();
        var merchant = BuildMerchant();
        IntegrateScene("Calabozo", player, merchant);
        IntegrateScene("Coliseo", player, null);
        string[] maps = { "Assets/00_Scenes/Maps/Calabozo.unity", "Assets/00_Scenes/Maps/Coliseo.unity" };
        EditorBuildSettings.scenes = maps.Select(p => new EditorBuildSettingsScene(p, true))
            .Concat(EditorBuildSettings.scenes.Where(s => !maps.Contains(s.path))).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("CHARACTERS_INTEGRATED: existing maps and reusable prefabs saved.");
    }
    static GameObject BuildPlayer()
    {
        var root = new GameObject("Player");
        try
        {
            var capsule = root.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .35f; capsule.center = Vector3.up * .9f;
            capsule.stepOffset = .3f; capsule.slopeLimit = 50; capsule.skinWidth = .04f;
            var player = root.AddComponent<PlayerCharacter>();
            var camera = new GameObject("First person camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false); camera.transform.localPosition = Vector3.up * 1.6f;
            camera.nearClipPlane = .05f; camera.farClipPlane = 200; camera.fieldOfView = 75; camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>(); player.view = camera;
            string[] classes = { "Knight", "Barbarian", "Ranger" };
            player.classVisuals = classes.Select(n => Model(Pack + "Characters/fbx/" + n + ".fbx", root.transform, 1.8f, n.ToLowerInvariant())).ToArray();
            for (int i = 0; i < player.classVisuals.Length; i++)
            {
                foreach (var renderer in player.classVisuals[i].GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                player.classVisuals[i].SetActive(i == 0);
            }
            var combat = root.AddComponent<PlayerCombat>();
            const string blink = "Assets/03_Models/Weapons/Blink/Art/Weapons/Stylized/";
            string[] weapons = {
                blink + "Swords/_PrefabsSwords/Sword1_1_3.prefab",
                blink + "Axes/PrefabsAxes/AxeBasic1_2.prefab",
                Pack + "Assets/fbx/bow.fbx",
                blink + "Daggers/_PrefabsDaggers/Dagger1_3_5.prefab",
                blink + "Hammers/_PrefabsHammers/Hammer1_1_3.prefab",
                blink + "Polearms/_Prefabs_Polearms/Polearm2_2_2.prefab",
                blink + "Scythes/_Prefabs_Scythes/Scythe1_3_2.prefab",
                blink + "Shields/_PrefabsShields/Shield2_1_2.prefab",
                blink + "Staves/_PrefabsStaves/Staff2_2_6.prefab",
                blink + "Musket/_Prefabs_Musket/Musket1_2_1.prefab" };
            combat.weaponModels = weapons.Select(n => Model(n, camera.transform, .7f, n.Contains("bow.fbx") ? "ranger" : null)).ToArray();
            for (int i = 0; i < combat.weaponModels.Length; i++)
            {
                combat.weaponModels[i].name = ((WeaponKind)i).ToString();
                combat.weaponModels[i].transform.localPosition = new Vector3(.3f, -.43f, .6f);
                combat.weaponModels[i].SetActive(i == 0);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, Output + "/Player.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static GameObject BuildMerchant()
    {
        var root = new GameObject("Merchant");
        try
        {
            var merchant = root.AddComponent<Merchant>();
            merchant.visual = Model(Pack + "Characters/fbx/Mage.fbx", root.transform, 1.8f, "mage");
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.height = 1.8f; capsule.radius = .4f; capsule.center = Vector3.up * .9f;
            merchant.interactionCollider = capsule;
            return PrefabUtility.SaveAsPrefabAsset(root, Output + "/Merchant.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static GameObject Model(string path, Transform parent, float height, string texture)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!source) throw new InvalidOperationException("Missing model " + path);
        var root = new GameObject(source.name); root.transform.SetParent(parent, false);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false);
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        float scale = height / bounds.size.y; model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = -new Vector3(bounds.center.x - root.transform.position.x, bounds.min.y - root.transform.position.y, bounds.center.z - root.transform.position.z) * scale;
        if (texture != null)
        {
            string materialPath = Output + "/" + texture + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "Textures/" + texture + "_texture.png"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }
        else foreach (var renderer in renderers)
            renderer.sharedMaterials = renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
        foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        var animator = model.GetComponent<Animator>();
        if (!animator && path.Contains("Characters/fbx/")) animator = model.AddComponent<Animator>();
        if (animator)
        {
            string controllerPath = Output + "/CharacterIdle.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller)
            {
                var sourceClip = AssetDatabase.LoadAllAssetsAtPath(Pack + "Animations/fbx/Rig_Medium/Rig_Medium_General.fbx").OfType<AnimationClip>().Single(c => c.name == "Idle_A");
                var idle = UnityEngine.Object.Instantiate(sourceClip); idle.name = "CharacterIdle";
                var settings = AnimationUtility.GetAnimationClipSettings(idle); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(idle, settings);
                AssetDatabase.CreateAsset(idle, Output + "/CharacterIdle.anim");
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                controller.AddMotion(idle);
            }
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
        }
        return root;
    }
    static Material ConvertMaterial(Material original)
    {
        if (!original) throw new InvalidOperationException("Weapon has a missing material.");
        if (original.shader.name.StartsWith("Universal Render Pipeline")) return original;
        string path = Output + "/Weapon_" + original.name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (original.HasProperty("_MainTex")) material.SetTexture("_BaseMap", original.GetTexture("_MainTex"));
        if (original.HasProperty("_Color")) material.SetColor("_BaseColor", original.GetColor("_Color"));
        AssetDatabase.CreateAsset(material, path); return material;
    }
    static void IntegrateScene(string name, GameObject player, GameObject merchant)
    {
        string path = "Assets/00_Scenes/Maps/" + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = scene.isLoaded;
        if (wasLoaded && scene.isDirty) throw new InvalidOperationException("Save manual changes before integration: " + path);
        if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            if (objects.Any(t => t.name == "Characters (Phase 2)")) return;
            var root = new GameObject("Characters (Phase 2)"); SceneManager.MoveGameObjectToScene(root, scene);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(player, scene);
            instance.transform.SetParent(root.transform); instance.transform.position = objects.Single(t => t.name == "Player_Entry").position;
            if (merchant)
            {
                var npc = (GameObject)PrefabUtility.InstantiatePrefab(merchant, scene);
                npc.transform.SetParent(root.transform); npc.transform.position = objects.Single(t => t.name == "Merchant_Center").position;
                npc.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            foreach (var t in objects.Where(t => t.name == "Overview camera (replace on integration)")) t.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Codice/Characters/Validate character transactions")]
    public static void ValidateTransactions()
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
        var state = new CharacterState();
        foreach (PlayerClass c in Enum.GetValues(typeof(PlayerClass)))
        { state.Initialize(c); Check(state.Health == state.MaxHealth && state.IsAlive, "Class initialization"); }
        state.Initialize(PlayerClass.Warrior);
        Check(state.MaxHealth == 120 && state.MoveSpeed == 6.5f, "Warrior stats");
        Check(!state.SpendGold(1) && !state.AddGold(-1), "Invalid currency transaction");
        state.AddGold(300); Check(state.SpendGold(80) && state.Gold == 220, "Payment");
        state.Damage(100); Check(state.Heal(60) && state.Health == 80, "Potion is half maximum health");
        Check(state.Equip(new WeaponInstance(WeaponKind.Bow)) == null && state.ActiveSlot == 1, "Second slot");
        var replaced = state.Equip(new WeaponInstance(WeaponKind.Axe, 2));
        Check(replaced.kind == WeaponKind.Bow && state.GetWeapon(0).kind == WeaponKind.Sword, "Replace active slot only");
        Check(!state.SelectWeapon(2), "Only two weapon slots");
        Check(state.EquipPassive(PassiveKind.Frenzy) && state.EquipPassive(PassiveKind.Overload) && state.EquipPassive(PassiveKind.ThornArmor), "Three passive slots");
        Check(!state.EquipPassive(PassiveKind.MercuryBoots), "Fourth passive needs explicit replacement");
        Check(!state.EquipPassive(PassiveKind.Frenzy, 0), "Duplicate passive rejected");
        Check(state.EquipPassive(PassiveKind.MercuryBoots, 1) && state.PassiveCount == 3, "Explicit passive replacement");
        state.Initialize(PlayerClass.Archer);
        for (int i = 0; i < 8; i++) Check(state.ConsumeAmmo(), "Arrow use");
        Check(!state.ConsumeAmmo(), "Empty quiver"); state.TickAmmunition(1.49f);
        Check(state.ActiveWeapon.ammunition == 0, "Reload interval"); state.TickAmmunition(.02f);
        Check(state.ActiveWeapon.ammunition == 1, "Arrow regeneration");
        int deaths = 0; state.Died += () => deaths++; state.Damage(1000); state.Damage(10);
        Check(deaths == 1 && !state.Heal(100) && !state.SpendGold(0), "Death is terminal");
        Debug.Log("CHARACTER_DATA_TESTS_OK: " + checks + " assertions.");
    }
}
