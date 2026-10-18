using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codice.Mechanics;
using Codice.Characters;
using Codice.Enemies;
using Codice.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class FinalArenaChecks
{
    const string Report = "Library/FinalArenaChecks.txt";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("FinalArenaChecks.Play", false))
            { SessionState.SetBool("FinalArenaChecks.Play", false); CheckTravel(); }
        };
    }
    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Library/FinalArenaChecks.request")) return;
        File.Delete("Library/FinalArenaChecks.request");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        { File.WriteAllText(Report, "FAILED: save scene edits before checks"); return; }
        File.WriteAllText(Report, "Final arena regression\n");
        Require(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/00_Scenes/Maps/FinalArena.unity"), "final scene enabled in build settings");
        EditorSceneManager.OpenScene("Assets/00_Scenes/Maps/Coliseo.unity");
        PlayerPrefs.SetInt("Codice.TestRound", 19);
        SessionState.SetBool("FinalArenaChecks.Play", true);
        EditorApplication.isPlaying = true;
    }
    static void Log(string s) => File.AppendAllText(Report, s + "\n");
    static void Require(bool value, string message) { if (!value) throw new Exception(message); Log("PASS " + message); }
    static async Task Until(Func<bool> predicate)
    {
        double end = EditorApplication.timeSinceStartup + 40;
        while (!predicate()) { if (EditorApplication.timeSinceStartup > end) throw new Exception("Timed out"); await Task.Delay(30); }
    }
    static void Pump() => EditorApplication.QueuePlayerLoopUpdate();
    static async void CheckTravel()
    {
        EditorApplication.update += Pump;
        try
        {
            await Until(() => RunDirector.Instance && RunDirector.Instance.CombatActive && RunDirector.Instance.Player);
            var director = RunDirector.Instance;
            var context = Object.FindFirstObjectByType<RunScene>();
            director.Player.enabled = false;
            director.Player.GetComponent<PlayerCombat>().enabled = false;
            double end = EditorApplication.timeSinceStartup + 40;
            while (director.Phase == RunPhase.Combat || director.Phase == RunPhase.Shop)
            {
                if (EditorApplication.timeSinceStartup > end) throw new Exception("Round 19 stuck");
                foreach (var enemy in context.dungeon.GetComponentsInChildren<BasicEnemy>())
                    if (enemy.IsAlive) enemy.ReceiveDamage(new DamageInfo { amount = 999999, source = director.Player.gameObject, ignoreGuard = true });
                if (director.Phase == RunPhase.Shop) context.dungeon.merchant.ContinueRound();
                await Task.Delay(30);
            }
            Require(context.portal.Available, "round 19 portal available");
            string inventory = director.Player.State.Capture();
            Require(director.Travel(), "travel accepted");
            Require(!director.Travel(), "duplicate travel rejected");
            await Until(() => SceneManager.GetActiveScene().name == "FinalArena" && director.Player && director.CombatActive);
            context = Object.FindFirstObjectByType<RunScene>();
            Require(director.Level == 20 && context.arena.ActiveBoss, "round 20 boss spawned");
            Require(director.Player.State.Capture() == inventory, "player state preserved");
            context.arena.ActiveBoss.SetPaused(true);
            director.Player.enabled = false;
            director.Player.GetComponent<PlayerCombat>().enabled = false;
            Require(Vector3.Distance(director.Player.transform.position, context.arena.spawnPoint.position) > 17, "18 metres of initial combat separation");
            var cc = director.Player.GetComponent<CharacterController>();
            // Exercise the actual capsule at every perimeter seam, with jump + dash motion.
            for (int side = 0; side < 64; side++)
            {
                var direction = Quaternion.Euler(0, (side + .5f) * 360f / 64, 0) * Vector3.forward;
                cc.enabled = false;
                director.Player.transform.position = direction * 12 + Vector3.up * .6f;
                cc.enabled = true;
                Require(UnityEngine.AI.NavMesh.SamplePosition(director.Player.transform.position, out var floor, 1, UnityEngine.AI.NavMesh.AllAreas), "combat floor navigable " + side);
                var route = new UnityEngine.AI.NavMeshPath();
                Require(UnityEngine.AI.NavMesh.CalculatePath(context.arena.spawnPoint.position, floor.position, UnityEngine.AI.NavMesh.AllAreas, route) && route.status == UnityEngine.AI.NavMeshPathStatus.PathComplete, "boss can reach perimeter " + side);
                float speed = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y * 2) * director.Player.jumpHeight);
                for (int frame = 0; frame < 90; frame++)
                {
                    speed += Physics.gravity.y * 2 / 60;
                    cc.Move((direction * 22 + Vector3.up * speed) / 60);
                    if (director.Player.transform.position.y <= -.2f || new Vector2(director.Player.transform.position.x, director.Player.transform.position.z).magnitude >= 16.1f)
                        throw new Exception("Escaped perimeter at " + side + "/" + frame);
                }
                Log("PASS jump/dash contained at perimeter seam " + side);
            }
            var preview = new GameObject("Arena overview").AddComponent<Camera>();
            preview.transform.position = new Vector3(32, 38, -43); preview.transform.LookAt(Vector3.zero);
            var target = new RenderTexture(1200, 900, 24); preview.targetTexture = target;
            await Task.Delay(500);
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1200, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); texture.Apply();
            File.WriteAllBytes("Library/FinalArenaPreview.png", texture.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(preview.gameObject); Object.Destroy(texture); Object.Destroy(target);
            context.arena.ActiveBoss.SetPaused(false);
            context.arena.ActiveBoss.ReceiveDamage(new DamageInfo { amount = 999999, source = director.Player.gameObject });
            Require(director.Phase == RunPhase.Victory, "final boss defeat ends run");
            Log("ALL_CHECKS_PASSED");
        }
        catch (Exception e) { Log("FAILED " + e); Debug.LogException(e); }
        finally { EditorApplication.update -= Pump; Time.timeScale = 1; EditorApplication.isPlaying = false; }
    }
}
