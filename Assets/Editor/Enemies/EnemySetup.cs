using System;
using System.IO;
using System.Linq;
using Codice.Characters;
using Codice.Enemies;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class EnemySetup
{
    const string Output = "Assets/01_Prefabs/Enemies";
    const string Pack = "Assets/03_Models/NormalEnemies/KayKit_Skeletons_1.1_FREE/KayKit_Skeletons_1.1_FREE/";
    [MenuItem("Codice/Enemies/Integrate basic enemies into Calabozo")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode first.");
        string scenePath = "Assets/00_Scenes/Maps/Calabozo.unity";
        var scene = SceneManager.GetSceneByPath(scenePath);
        if (scene.isLoaded && scene.isDirty) throw new Exception("Save scene edits before integrating enemies.");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (scene.GetRootGameObjects().Any(g => g.name == "Basic enemies (Phase 3)")) throw new Exception("Enemies are already integrated.");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "texture/skeleton_texture.png"));
        AssetDatabase.CreateAsset(mat, Output + "/Skeleton.mat");
        var bolt = Projectile("Bolt", new Color(.6f, .3f, .1f), new Vector3(.09f, .09f, .65f));
        var orb = Projectile("Orb", new Color(.5f, .15f, 1), Vector3.one * .25f);
        BasicEnemy[] prefabs = new BasicEnemy[4];
        string[] models = { "Skeleton_Warrior", "Skeleton_Rogue", "Skeleton_Mage", "Skeleton_Warrior" };
        string[] weapons = { "Skeleton_Blade", "Skeleton_Crossbow", "Skeleton_Staff", "Skeleton_Axe" };
        for (int i = 0; i < 4; i++)
        {
            var root = new GameObject(((EnemyKind)i).ToString());
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.height = 1.8f; capsule.radius = .35f; capsule.center = Vector3.up * .9f;
            var agent = root.AddComponent<NavMeshAgent>(); agent.height = 1.8f; agent.radius = .35f; agent.angularSpeed = 360; agent.acceleration = 12;
            var enemy = root.AddComponent<BasicEnemy>(); enemy.kind = (EnemyKind)i; enemy.projectilePrefab = i == 2 ? orb : bolt;
            var visual = Model(Pack + "characters/fbx/" + models[i] + ".fbx", root.transform, i == 3 ? 2 : 1.8f, mat);
            var animator = visual.GetComponentInChildren<Animator>();
            if (!animator) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/01_Prefabs/Characters/CharacterIdle.controller");
            animator.applyRootMotion = false; enemy.animator = animator;
            var pivot = new GameObject("Attack pose").transform; pivot.SetParent(root.transform, false); pivot.localPosition = new Vector3(.45f, .9f, .3f);
            enemy.weaponPivot = pivot;
            Model(Pack + "assets/fbx(unity)/" + weapons[i] + ".fbx", pivot, i == 2 ? 1.4f : .8f, mat);
            if (i == 3)
            {
                enemy.shield = Model(Pack + "assets/fbx(unity)/Skeleton_Shield_Large_A.fbx", root.transform, 1, mat);
                enemy.shield.transform.localPosition = new Vector3(-.4f, .65f, .4f);
            }
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + root.name + ".prefab").GetComponent<BasicEnemy>();
            UnityEngine.Object.DestroyImmediate(root);
        }
        var group = new GameObject("Basic enemies (Phase 3)"); SceneManager.MoveGameObjectToScene(group, scene);
        var encounter = group.AddComponent<BasicEnemyEncounter>(); encounter.prefabs = prefabs;
        var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        encounter.player = objects.Select(t => t.GetComponent<PlayerCharacter>()).First(p => p);
        encounter.merchant = objects.Select(t => t.GetComponent<Merchant>()).First(p => p);
        encounter.spawnPoints = objects.Where(t => t.name.StartsWith("Enemy_Entry_")).OrderBy(t => t.name).ToArray();
        // Bake static collider geometry without including the player or merchant in the mesh.
        var excluded = new[] { encounter.player.gameObject, encounter.merchant.gameObject };
        foreach (var obj in excluded) obj.SetActive(false);
        try
        {
            var surface = group.AddComponent<NavMeshSurface>(); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.collectObjects = CollectObjects.Volume; surface.size = new Vector3(120, 30, 120); surface.center = Vector3.zero;
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new Exception("Navigation bake failed.");
            AssetDatabase.CreateAsset(surface.navMeshData, Output + "/CalabozoNavigation.asset");
        }
        finally { foreach (var obj in excluded) obj.SetActive(true); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        UpdatePresentation();
        Debug.Log("ENEMIES_INTEGRATED: four prefabs, navigation and initial encounter.");
    }

    [MenuItem("Codice/Enemies/Update enemy locomotion and equipment")]
    public static void UpdatePresentation()
    {
        string controllerPath = Output + "/EnemyLocomotion.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var idleSource = AssetDatabase.LoadAllAssetsAtPath(Pack + "Animations/fbx/Rig_Medium/Rig_Medium_General.fbx").OfType<AnimationClip>().First(c => c.name == "Idle_A");
            var walkSource = AssetDatabase.LoadAllAssetsAtPath(Pack + "Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx").OfType<AnimationClip>().First(c => c.name == "Walking_A");
            AnimationClip Clip(AnimationClip source, string name)
            {
                var clip = UnityEngine.Object.Instantiate(source); clip.name = name;
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings); AssetDatabase.CreateAsset(clip, Output + "/" + name + ".anim"); return clip;
            }
            var state = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(idleSource, "EnemyIdle"), 0); tree.AddChild(Clip(walkSource, "EnemyWalk"), 2);
            controller.layers[0].stateMachine.defaultState = state;
        }
        foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
        {
            string path = Output + "/" + kind + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var enemy = root.GetComponent<BasicEnemy>();
                enemy.animator.runtimeAnimatorController = controller;
                var right = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "handslot.r");
                UnityEngine.Object.DestroyImmediate(enemy.weaponPivot.gameObject);
                var pivot = new GameObject("Attack pose").transform; pivot.SetParent(right, false);
                enemy.weaponPivot = pivot;
                string[] weapons = { "Skeleton_Blade", "Skeleton_Crossbow", "Skeleton_Staff", "Skeleton_Axe" };
                Equipment(weapons[(int)kind], pivot, kind == EnemyKind.Mage ? 1.4f : .8f);
                if (enemy.shield)
                {
                    var left = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "handslot.l");
                    UnityEngine.Object.DestroyImmediate(enemy.shield);
                    enemy.shield = Equipment("Skeleton_Shield_Large_A", left, 1);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    static GameObject Equipment(string name, Transform hand, float length)
    {
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "assets/fbx(unity)/" + name + ".fbx"), hand);
        model.name = name; model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.localScale *= length / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        var material = AssetDatabase.LoadAssetAtPath<Material>(Output + "/Skeleton.mat");
        foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        return model;
    }

    static GameObject Model(string path, Transform parent, float height, Material material)
    {
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        model.name = Path.GetFileNameWithoutExtension(path); model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        model.transform.localScale *= height / Mathf.Max(bounds.size.y, .01f);
        bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        model.transform.position -= new Vector3(bounds.center.x - parent.position.x, bounds.min.y - parent.position.y, bounds.center.z - parent.position.z);
        foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
        foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        return model;
    }

    static EnemyProjectile Projectile(string name, Color color, Vector3 scale)
    {
        var obj = GameObject.CreatePrimitive(name == "Orb" ? PrimitiveType.Sphere : PrimitiveType.Cube); obj.name = name;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.transform.localScale = scale;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color;
        AssetDatabase.CreateAsset(mat, Output + "/" + name + ".mat"); obj.GetComponent<Renderer>().sharedMaterial = mat;
        obj.AddComponent<EnemyProjectile>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, Output + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(obj); return prefab.GetComponent<EnemyProjectile>();
    }
}
