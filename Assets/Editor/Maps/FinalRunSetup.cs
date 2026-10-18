using System;
using System.IO;
using System.Linq;
using Codice.Bosses;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// One-off authoring tool. It saves ordinary scenes and prefabs; nothing here runs in a build.
public static class FinalRunSetup
{
    const string Maps = "Assets/00_Scenes/Maps/";
    const string BossOutput = "Assets/01_Prefabs/Bosses/";
    const string OrcSource = "Assets/03_Models/Bosses/Monster_Orc (Troll)/Prefabs/monster_orc.prefab";
    const string MutantSource = "Assets/03_Models/Bosses/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin1.prefab";
    const string BerserkerSource = "Assets/03_Models/Bosses/Ork berserker/prefab/ork berserk prefab.prefab";
    const string FstSource = "Assets/03_Models/Map/FST/Gladiators Low Poly Arena/Prefabs/Gladiator Low Poly Arena.prefab";
    static string step;

    [MenuItem("Codice/Maps/Install final round, FST arena and Coliseo trading")]
    public static void Install()
    {
        try
        {
            step="preparing"; if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before installing the final run.");
            // Opening each target scene below writes it directly. Avoid Unity Version Control's
            // save prompt here: it throws on this project when the active menu scene is clean.
            Directory.CreateDirectory(BossOutput);
            step="checking imported assets"; Require(OrcSource); Require(MutantSource); Require(BerserkerSource); Require(FstSource);
            step="creating Monster Orc"; var orc = CreateBoss("MonsterOrc", OrcSource, BossKind.MonsterOrc, 1650, 380);
            step="creating Monster Mutant"; var mutant = CreateBoss("MonsterMutant", MutantSource, BossKind.MonsterMutant, 1900, 420);
            step="creating Ork Berserker"; var berserker = CreateBoss("OrkBerserker", BerserkerSource, BossKind.OrkBerserker, 3200, 850);
            step="configuring Calabozo"; ConfigureMap("Calabozo", false, orc, mutant, berserker);
            step="configuring Coliseo"; ConfigureMap("Coliseo", true, orc, mutant, berserker);
            step="creating FST final arena"; CreateFinalArena(berserker);
            step="adding build scene"; AddBuildScene(Maps + "FinalArena.unity");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("FINAL_RUN_INSTALLED: Calabozo/Coliseo alternate through round 19; FST is round 20.");
        }
        catch (Exception exception) { throw new InvalidOperationException("Final-run installation failed while " + step + ".", exception); }
    }

    static void Require(string path)
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(path)) throw new FileNotFoundException("Required project asset was not imported: " + path);
    }

    static BossCharacter CreateBoss(string name, string sourcePath, BossKind kind, float health, int gold)
    {
        string path = BossOutput + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<BossCharacter>(path);
        if (existing) return existing;
        var root = new GameObject(name);
        try
        {
            var agent = root.AddComponent<NavMeshAgent>(); agent.acceleration = 16; agent.angularSpeed = 240; agent.stoppingDistance = 2.8f;
            var collider = root.AddComponent<CapsuleCollider>(); collider.radius = .9f; collider.height = 3; collider.center = Vector3.up * 1.5f;
            // BossCharacter caches its NavMeshAgent in Awake, so add required components first.
            var boss = root.AddComponent<BossCharacter>();
            boss.kind = kind; boss.baseHealth = health; boss.goldReward = gold; boss.useNativeAnimator = true;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
            visual.name = "Native animated visual"; visual.transform.SetParent(root.transform, false); boss.visual = visual.transform;
            foreach (var c in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 3.2f / Mathf.Max(.1f, bounds.size.y);
                visual.transform.localScale *= scale;
                bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                visual.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<BossCharacter>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ConfigureMap(string map, bool coliseo, BossCharacter orc, BossCharacter mutant, BossCharacter berserker)
    {
        var scene = EditorSceneManager.OpenScene(Maps + map + ".unity", OpenSceneMode.Single);
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();
        if (!player) throw new InvalidOperationException(map + " needs its Player instance before final-run setup.");
        var run = UnityEngine.Object.FindFirstObjectByType<RunScene>();
        if (!run) { var root = new GameObject("Run scene"); run = root.AddComponent<RunScene>(); }
        run.player = player;
        var markers = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var center = markers.FirstOrDefault(t => t.name == "Boss_Center") ?? markers.FirstOrDefault(t => t.name == "Merchant_Center") ?? player.transform;
        var portalMarker = markers.FirstOrDefault(t => t.name == "Portal_Return" || t.name == "Portal_ToColiseum") ?? center;
        run.portal = EnsurePortal(portalMarker.position, portalMarker.rotation);
        var encounter = UnityEngine.Object.FindFirstObjectByType<BossEncounter>();
        if (!encounter) encounter = new GameObject("Boss encounter").AddComponent<BossEncounter>();
        encounter.player = player; encounter.spawnPoint = center; encounter.managedByRun = true; encounter.startOnPlay = false;
        encounter.bossPrefabs = new[] { encounter.goblinPrefab, encounter.demonPrefab, orc, mutant }.Where(b => b).ToArray();
        encounter.finalBossPrefab = berserker;
        run.arena = encounter;
        if (coliseo)
        {
            run.dungeon = EnsureWaveEncounter(player, center);
            EnsureMerchant(run.dungeon, center.position + new Vector3(0, 0, -4));
            run.lootMarkers = EnsureLootMarkers("Coliseo loot markers", center.position, 10, 12);
        }
        else
        {
            if (!run.dungeon) run.dungeon = UnityEngine.Object.FindFirstObjectByType<BasicEnemyEncounter>();
            if (!run.dungeon) throw new InvalidOperationException("Calabozo needs BasicEnemyEncounter.");
            if (run.lootMarkers == null || run.lootMarkers.Length == 0) run.lootMarkers = EnsureLootMarkers("Calabozo loot markers", center.position, 10, 10);
        }
        EnsureNavigation(scene, map, 110);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static BasicEnemyEncounter EnsureWaveEncounter(PlayerCharacter player, Transform center)
    {
        var encounter = UnityEngine.Object.FindFirstObjectByType<BasicEnemyEncounter>();
        if (!encounter) encounter = new GameObject("Coliseo waves").AddComponent<BasicEnemyEncounter>();
        encounter.player = player; encounter.startOnPlay = false; encounter.maxConcurrent = 8;
        encounter.prefabs = new[] { "Swordsman", "Crossbowman", "Mage", "Heavy" }.Select(n => AssetDatabase.LoadAssetAtPath<BasicEnemy>("Assets/01_Prefabs/Enemies/" + n + ".prefab")).ToArray();
        if (encounter.prefabs.Any(p => !p)) throw new InvalidOperationException("Coliseo is missing an enemy prefab.");
        if (encounter.spawnPoints == null || encounter.spawnPoints.Length < 6)
        {
            var root = new GameObject("Coliseo wave spawns").transform;
            var points = new Transform[8];
            for (int i = 0; i < points.Length; i++)
            {
                var marker = new GameObject("Wave spawn " + (i + 1)).transform; marker.SetParent(root);
                marker.position = center.position + Quaternion.Euler(0, i * 45, 0) * Vector3.forward * 12;
                points[i] = marker;
            }
            encounter.spawnPoints = points;
        }
        return encounter;
    }

    static void EnsureMerchant(BasicEnemyEncounter encounter, Vector3 position)
    {
        var merchant = encounter.merchant ? encounter.merchant : UnityEngine.Object.FindFirstObjectByType<Merchant>();
        if (!merchant)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Merchant.prefab");
            merchant = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<Merchant>();
            merchant.name = "Mercader del Coliseo"; merchant.transform.position = position;
        }
        encounter.merchant = merchant;
    }

    static RunPortal EnsurePortal(Vector3 position, Quaternion rotation)
    {
        var portal = UnityEngine.Object.FindFirstObjectByType<RunPortal>();
        if (portal) return portal;
        var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder); root.name = "Portal de transición";
        root.transform.SetPositionAndRotation(position, rotation); root.transform.localScale = new Vector3(1.4f, .12f, 1.4f);
        var renderer = root.GetComponent<Renderer>(); renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); renderer.sharedMaterial.color = new Color(.15f, .75f, 1, 1);
        portal = root.AddComponent<RunPortal>(); portal.visual = root; portal.SetAvailable(false); return portal;
    }

    static Transform[] EnsureLootMarkers(string name, Vector3 center, int count, float radius)
    {
        var root = new GameObject(name).transform; var points = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var marker = new GameObject("Loot " + (i + 1)).transform; marker.SetParent(root);
            marker.position = center + Quaternion.Euler(0, i * 360f / count, 0) * Vector3.forward * radius;
            points[i] = marker;
        }
        return points;
    }

    static void CreateFinalArena(BossCharacter berserker)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var arena = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FstSource)); arena.name = "FST • Gladiators Low Poly Arena";
        var sun = new GameObject("Final arena light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Player.prefab");
        var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab)).GetComponent<PlayerCharacter>(); player.transform.position = new Vector3(0, .1f, -10);
        var runRoot = new GameObject("Final Run Scene"); var run = runRoot.AddComponent<RunScene>(); run.player = player;
        var boss = new GameObject("Final boss encounter").AddComponent<BossEncounter>(); boss.player = player; boss.spawnPoint = new GameObject("Final boss spawn").transform; boss.spawnPoint.position = new Vector3(0, .1f, 5); boss.managedByRun = true; boss.startOnPlay = false; boss.finalBossPrefab = berserker; run.arena = boss;
        var portal = EnsurePortal(new Vector3(0, .1f, -13), Quaternion.identity); run.portal = portal;
        run.lootMarkers = EnsureLootMarkers("Final arena weapons", Vector3.zero, 8, 8);
        EnsureNavigation(scene, "FinalArena", 160);
        EditorSceneManager.SaveScene(scene, Maps + "FinalArena.unity");
    }

    static void EnsureNavigation(Scene scene, string name, float size)
    {
        var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
        if (!surface) surface = new GameObject(name + " navigation").AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.size = Vector3.one * size;
        surface.BuildNavMesh();
        if (!surface.navMeshData) throw new InvalidOperationException(name + " navigation could not be baked.");
        string path = BossOutput + name + "Navigation.asset";
        if (!AssetDatabase.LoadAssetAtPath<NavMeshData>(path)) AssetDatabase.CreateAsset(surface.navMeshData, path);
    }

    static void AddBuildScene(string path)
    {
        if (EditorBuildSettings.scenes.Any(s => s.path == path)) return;
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
    }
}
