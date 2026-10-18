using System;
using System.IO;
using System.Linq;
using Codice.Mechanics;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit scene authoring; the saved scene and navigation data also work in builds.
public static class FinalArenaLayout
{
    const string ScenePath = "Assets/00_Scenes/Maps/FinalArena.unity";
    [InitializeOnLoadMethod] static void Init() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Library/FinalArenaLayout.request")) return;
        File.Delete("Library/FinalArenaLayout.request");
        try { Apply(); File.WriteAllText("Library/FinalArenaLayout.txt", "LAYOUT_PASSED"); }
        catch (Exception e) { File.WriteAllText("Library/FinalArenaLayout.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Codice/Maps/Expand final arena")]
    public static void Apply()
    {
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene edits before rebuilding the final arena.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var run = Object.FindFirstObjectByType<RunScene>();
        var arena = scene.GetRootGameObjects().First(g => g.name.StartsWith("FST"));
        arena.transform.localScale = new Vector3(4, 2, 4);
        run.player.gameObject.SetActive(false);
        var existing = GameObject.Find("Final arena perimeter");
        if (existing) Object.DestroyImmediate(existing);
        var perimeter = new GameObject("Final arena perimeter");
        // Continuous vertical collision above the arena rim, including the entrance.
        const int segments = 64;
        const float radius = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 360f / segments;
            var wall = new GameObject("Boundary " + i);
            wall.transform.SetParent(perimeter.transform);
            wall.transform.position = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius + Vector3.up * 9;
            wall.transform.rotation = Quaternion.Euler(0, angle, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(2 * radius * Mathf.Tan(Mathf.PI / segments) + .1f, 22, .6f);
        }
        Physics.SyncTransforms();
        run.player.transform.position = Floor(new Vector3(0, 0, -10)) + Vector3.up * .08f;
        run.arena.spawnPoint.position = Floor(new Vector3(0, 0, 8));
        run.portal.transform.position = Floor(new Vector3(0, 0, -12)) + Vector3.up * .1f;
        for (int i = 0; i < run.lootMarkers.Length; i++)
            run.lootMarkers[i].position = Floor(Quaternion.Euler(0, i * 360f / run.lootMarkers.Length, 0) * Vector3.forward * 11) + Vector3.up * .1f;
        foreach (var s in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) s.RemoveData();
        var surface = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).First(s => s.enabled);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        var data = surface.navMeshData;
        var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/01_Prefabs/Bosses/FinalArenaRunNavigation.asset");
        surface.RemoveData(); EditorUtility.CopySerialized(data, saved); surface.navMeshData = saved; surface.AddData(); EditorUtility.SetDirty(saved);
        var route = new NavMeshPath();
        if (!NavMesh.CalculatePath(run.arena.spawnPoint.position, run.player.transform.position, NavMesh.AllAreas, route) || route.status != NavMeshPathStatus.PathComplete)
            throw new Exception("Boss cannot reach player");
        run.player.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        // Reapply through the editor API so an already-open editor drops stale scene lists.
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(Math.Min(3, scenes.Count), new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }
    static Vector3 Floor(Vector3 point)
    {
        if (!Physics.Raycast(point + Vector3.up * 6, Vector3.down, out var hit, 12, ~0, QueryTriggerInteraction.Ignore) || hit.normal.y < .9f)
            throw new Exception("Missing combat floor at " + point);
        return hit.point;
    }
}
