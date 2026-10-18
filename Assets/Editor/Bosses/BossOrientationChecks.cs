using System;
using System.Reflection;
using Codice.Bosses;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class BossOrientationChecks
{
    public static string Run()
    {
        var sceneBoss = Object.FindFirstObjectByType<BossCharacter>();
        if (!Application.isPlaying || !sceneBoss || !sceneBoss.Target) throw new Exception("Run in the Coliseo with an active boss.");
        var player = new GameObject("Orientation test player").AddComponent<Codice.Characters.PlayerCharacter>(); player.enabled = false; var controller = player.GetComponent<CharacterController>();
        Vector3 originalPosition = player.transform.position; bool enabled = controller.enabled;
        var prefab = AssetDatabase.LoadAssetAtPath<BossCharacter>("Assets/01_Prefabs/Bosses/GoblinKing.prefab");
        var boss = Object.Instantiate(prefab, sceneBoss.transform.position, Quaternion.identity); boss.enabled = false;
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            controller.enabled = false;
            player.transform.position = boss.transform.position + Vector3.back * 7;
            boss.Initialize(player, 5);
            Check(Vector3.Dot(boss.transform.forward, Vector3.back) > .999f, "Faces player immediately on spawn");
            Check(!boss.GetComponent<NavMeshAgent>().updateRotation, "Navigation cannot override facing");
            foreach (var state in new[] { BossState.Idle, BossState.Windup, BossState.Recovery })
            foreach (var direction in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                player.transform.position = boss.transform.position + direction * 7;
                typeof(BossCharacter).GetField("<State>k__BackingField", flags).SetValue(boss, state);
                typeof(BossCharacter).GetField("windup", flags).SetValue(boss, 10f);
                typeof(BossCharacter).GetField("attackTimer", flags).SetValue(boss, 10f);
                boss.SendMessage("Update");
                Check(Vector3.Dot(boss.transform.forward, direction) > .999f, "Logical facing " + state + direction);
                Check(Vector3.Dot(-boss.visual.forward, direction) > .999f, "Mesh front " + state + direction);
            }
            boss.Initialize(player, 15);
            Check(boss.Evolved && Vector3.Dot(-boss.visual.forward, (player.transform.position - boss.transform.position).normalized) > .999f, "Evolved Goblin keeps correct visual axis");
            string result = "BOSS_ORIENTATION_CHECKS_PASSED: " + count; Debug.Log(result); return result;
        }
        finally { player.transform.position = originalPosition; controller.enabled = enabled; boss.gameObject.SetActive(false); Object.Destroy(boss.gameObject); Object.Destroy(player.gameObject); }
    }
}
