using System;
using System.IO;
using System.Linq;
using Codice.Bosses;
using Codice.Characters;
using Codice.Enemies;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class BossSetup
{
    const string Output = "Assets/01_Prefabs/Bosses";
    const string Pack = "Assets/03_Models/Bosses/";
    [MenuItem("Codice/Bosses/Integrate bosses into Coliseo")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Save open scenes before integrating.");
        var scene = EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Coliseo.unity", OpenSceneMode.Single);
        if (UnityEngine.Object.FindFirstObjectByType<BossEncounter>()) throw new Exception("Boss encounter already exists.");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh();
        var rock = Material("Rock", null, new Color(.35f, .3f, .25f));
        var mud = Material("Mud", null, new Color(.25f, .32f, .08f));
        var fire = Material("Fire", null, new Color(1, .25f, .03f));
        var enemies = Enum.GetNames(typeof(EnemyKind)).Select(n => AssetDatabase.LoadAssetAtPath<BasicEnemy>("Assets/01_Prefabs/Enemies/" + n + ".prefab")).ToArray();
        BossCharacter[] prefabs = new BossCharacter[2];
        for (int i = 0; i < 2; i++)
        {
            string modelName = i == 0 ? "Puglin" : "Imp";
            var normal = Material(modelName + "Base", Pack + "Textures/T_" + modelName + "_BaseColor_1.png", Color.white);
            var evolved = Material(modelName + "Evolved", Pack + "Textures/T_" + modelName + "_BaseColor_3.png", Color.white);
            var root = new GameObject(i == 0 ? "GoblinKing" : "DemonLord");
            var collider = root.AddComponent<CapsuleCollider>(); collider.height = 3; collider.radius = .85f; collider.center = Vector3.up * 1.5f;
            var agent = root.AddComponent<NavMeshAgent>(); agent.height = 3; agent.radius = .85f; agent.acceleration = 12; agent.angularSpeed = 180; agent.stoppingDistance = 2.8f;
            var boss = root.AddComponent<BossCharacter>(); boss.kind = (BossKind)i; boss.baseHealth = i == 0 ? 1200 : 1800;
            boss.goldReward = i == 0 ? 300 : 450; boss.baseMaterial = normal; boss.evolvedMaterial = evolved;
            boss.rockMaterial = rock; boss.mudMaterial = mud; boss.fireMaterial = fire; boss.summons = enemies;
            var pivot = new GameObject("Visual").transform; pivot.SetParent(root.transform, false); boss.visual = pivot;
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Exports/FBX (Unity)/" + modelName + ".fbx"), pivot);
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale *= 3 / Mathf.Max(.01f, bounds.size.y);
            bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(normal, renderer.sharedMaterials.Length).ToArray();
            foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            if (i == 1)
            {
                boss.meleeWeapon = Equipment(pivot, "Skeleton_Axe", new Vector3(1, 1.4f, .6f));
                boss.bowWeapon = Equipment(pivot, "bow_A_withString", new Vector3(0, 1.5f, 1));
                boss.bowWeapon.SetActive(false);
            }
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + root.name + ".prefab").GetComponent<BossCharacter>();
            UnityEngine.Object.DestroyImmediate(root);
        }
        var group = new GameObject("Bosses (Phase 4)");
        var encounter = group.AddComponent<BossEncounter>(); encounter.goblinPrefab = prefabs[0]; encounter.demonPrefab = prefabs[1];
        encounter.player = UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();
        encounter.spawnPoint = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Boss_Center");
        encounter.player.gameObject.SetActive(false);
        try
        {
            var surface = group.AddComponent<NavMeshSurface>(); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.collectObjects = CollectObjects.Volume; surface.size = new Vector3(140, 30, 140);
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new Exception("Coliseo navigation failed.");
            AssetDatabase.CreateAsset(surface.navMeshData, Output + "/ColiseoNavigation.asset");
        }
        finally { encounter.player.gameObject.SetActive(true); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("BOSSES_INTEGRATED");
    }
    static Material Material(string name, string texture, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
        if (texture != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        AssetDatabase.CreateAsset(material, Output + "/" + name + ".mat"); return material;
    }
    static GameObject Equipment(Transform parent, string name, Vector3 position)
    {
        string path = "Assets/03_Models/NormalEnemies/KayKit_Skeletons_1.1_FREE/KayKit_Skeletons_1.1_FREE/assets/fbx(unity)/" + name + ".fbx";
        if (name.StartsWith("bow_")) path = "Assets/03_Models/Weapons/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/" + name + ".fbx";
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        model.transform.localPosition = position; model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * 1.5f;
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/01_Prefabs/Enemies/Skeleton.mat");
        if (name.StartsWith("bow_"))
        {
            material = AssetDatabase.LoadAssetAtPath<Material>(Output + "/Bow.mat");
            if (!material) material = Material("Bow", "Assets/03_Models/Weapons/KayKit_FantasyWeaponsBits_1.0_FREE/Textures/weapons_bits_texture.png", Color.white);
        }
        foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
        return model;
    }
    [MenuItem("Codice/Bosses/Update demon bow")]
    public static void UpdateBow()
    {
        string path = Output + "/DemonLord.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var boss = root.GetComponent<BossCharacter>();
            UnityEngine.Object.DestroyImmediate(boss.bowWeapon);
            boss.bowWeapon = Equipment(boss.visual, "bow_A_withString", new Vector3(0, 1.5f, 1));
            boss.bowWeapon.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}
