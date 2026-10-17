using System;
using System.IO;
using System.Linq;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Mechanics;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MechanicsSetup
{
    const string Output = "Assets/01_Prefabs/Mechanics";
    [MenuItem("Codice/Mechanics/Integrate mechanics into existing maps")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save scene edits before integrating mechanics.");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh();
        string playerPath = "Assets/01_Prefabs/Characters/Player.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(playerPath);
        try { if (!prefab.GetComponent<PlayerMechanics>()) prefab.AddComponent<PlayerMechanics>(); PrefabUtility.SaveAsPrefabAsset(prefab, playerPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (string name in new[] { "Calabozo", "Coliseo" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/" + name + ".unity");
            if (UnityEngine.Object.FindFirstObjectByType<RunScene>()) continue;
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var root = new GameObject("Mechanics (Phase 5)"); var run = root.AddComponent<RunScene>();
            run.player = UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();
            run.dungeon = UnityEngine.Object.FindFirstObjectByType<BasicEnemyEncounter>();
            run.arena = UnityEngine.Object.FindFirstObjectByType<BossEncounter>();
            if (run.dungeon) run.dungeon.startOnPlay = false;
            if (run.arena) { run.arena.startOnPlay = false; run.arena.managedByRun = true; }
            run.lootMarkers = transforms.Where(t => t.name.StartsWith("Loot_Crypt_")).ToArray();
            var portalMarker = transforms.Single(t => t.name == (run.arena ? "Portal_Return" : "Portal_ToColiseum"));
            var portal = GameObject.CreatePrimitive(PrimitiveType.Capsule); portal.name = "Portal (E)";
            portal.transform.SetParent(root.transform); portal.transform.position = portalMarker.position + Vector3.up;
            portal.transform.localScale = new Vector3(1, 1, .3f); portal.GetComponent<Collider>().isTrigger = true;
            var portalMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/Portal.mat");
            if (!portalMaterial)
            {
                portalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); portalMaterial.color = new Color(.15f, .65f, 1);
                AssetDatabase.CreateAsset(portalMaterial, Output + "/Portal.mat");
            }
            portal.GetComponent<Renderer>().sharedMaterial = portalMaterial;
            run.portal = portal.AddComponent<RunPortal>();
            // Visual is separate so disabling it does not disable the interaction component.
            var visual = new GameObject("Portal visual"); visual.transform.SetParent(portal.transform, false);
            var filter = visual.AddComponent<MeshFilter>(); filter.sharedMesh = portal.GetComponent<MeshFilter>().sharedMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = portalMaterial;
            UnityEngine.Object.DestroyImmediate(portal.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(portal.GetComponent<MeshFilter>());
            run.portal.visual = visual; run.portal.SetAvailable(false);
            if (run.arena)
            {
                var pillars = transforms.Where(t => t.name.StartsWith("Cover_Pillar_") && t.name.Contains("static geometry")).ToArray();
                foreach (var pillar in pillars) { pillar.gameObject.AddComponent<DestructiblePillar>(); pillar.gameObject.SetActive(false); }
                run.player.gameObject.SetActive(false);
                var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                try
                {
                    surface.BuildNavMesh(); AssetDatabase.CreateAsset(surface.navMeshData, Output + "/ColiseoNavigation.asset");
                }
                finally { run.player.gameObject.SetActive(true); foreach (var pillar in pillars) pillar.gameObject.SetActive(true); }
                foreach (var pillar in pillars)
                {
                    var renderers = pillar.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    var obstacle = new GameObject("Dynamic pillar obstacle"); obstacle.transform.position = bounds.center;
                    obstacle.transform.SetParent(pillar, true);
                    var nav = obstacle.AddComponent<NavMeshObstacle>(); nav.shape = NavMeshObstacleShape.Box; nav.size = bounds.size; nav.carving = true;
                }
                foreach (var marker in transforms.Where(t => t.name.StartsWith("Arrow_Supply_")))
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name = "Suministro de flechas (E / 15s)";
                    obj.transform.SetParent(root.transform); obj.transform.position = marker.position; obj.transform.localScale = Vector3.one * .5f;
                    obj.GetComponent<Collider>().isTrigger = true; obj.GetComponent<Renderer>().sharedMaterial = portalMaterial;
                    var pickup = obj.AddComponent<LootPickup>(); pickup.kind = LootKind.Ammunition; pickup.amount = 8; pickup.respawns = true;
                    // Keep the script active while its visible mesh waits for regeneration.
                    var mesh = new GameObject("Supply visual"); mesh.transform.SetParent(obj.transform, false);
                    mesh.AddComponent<MeshFilter>().sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                    mesh.AddComponent<MeshRenderer>().sharedMaterial = portalMaterial; pickup.visual = mesh;
                    UnityEngine.Object.DestroyImmediate(obj.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(obj.GetComponent<MeshFilter>());
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Calabozo.unity");
        Debug.Log("MECHANICS_INTEGRATED");
    }
}
