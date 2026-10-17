using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class EnemyPlayChecks
{
    public static string Result { get; private set; } = "Not run";
    public static async void Begin()
    {
        if (Result == "Running") return;
        Result = "Running";
        try { Result = await Run(); Debug.Log(Result); }
        catch (Exception error) { Result = error.ToString(); Debug.LogError(Result); }
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play mode with Calabozo open.");
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); checks++; }
        var encounter = Object.FindFirstObjectByType<BasicEnemyEncounter>();
        var scenePlayer = encounter.player;
        foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
        var playerObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Player.prefab"));
        var player = playerObject.GetComponent<PlayerCharacter>(); player.enabled = false;
        playerObject.GetComponent<PlayerCombat>().enabled = false;
        playerObject.GetComponent<CharacterController>().enabled = false;
        foreach (var camera in playerObject.GetComponentsInChildren<Camera>()) camera.enabled = false;
        foreach (var listener in playerObject.GetComponentsInChildren<AudioListener>()) listener.enabled = false;
        playerObject.transform.position = new Vector3(0, .1f, -4);
        playerObject.GetComponent<CharacterController>().enabled = true;
        var created = new System.Collections.Generic.List<GameObject>();
        BasicEnemy Spawn(EnemyKind kind, Vector3 position)
        {
            var enemy = Object.Instantiate(encounter.prefabs[(int)kind], position, Quaternion.identity);
            enemy.Initialize(player); enemy.enabled = false; created.Add(enemy.gameObject); return enemy;
        }
        try
        {
            float[] health = { 65, 55, 80, 220 }; int[] gold = { 10, 15, 20, 35 };
            for (int i = 0; i < 4; i++)
            {
                var enemy = Spawn((EnemyKind)i, new Vector3(i * 2 - 3, .1f, 0));
                Check(enemy.Health == health[i], "base HP " + i);
                Check(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "spawn on navmesh " + i);
                int before = player.State.Gold, deaths = 0; enemy.Died += _ => deaths++;
                enemy.ReceiveDamage(new DamageInfo { amount = 10000, breaksGuard = true, source = playerObject });
                enemy.ReceiveDamage(new DamageInfo { amount = 10000, source = playerObject });
                Check(deaths == 1 && player.State.Gold == before + gold[i], "one death and reward " + i);
            }
            var heavy = Spawn(EnemyKind.Heavy, Vector3.zero);
            heavy.ReceiveDamage(new DamageInfo { amount = 50, direction = Vector3.back });
            Check(heavy.Health == 210, "80 percent frontal block");
            heavy.ReceiveDamage(new DamageInfo { amount = 50, direction = Vector3.forward });
            Check(heavy.Health == 160, "rear damage bypass");
            heavy.ReceiveDamage(new DamageInfo { amount = 20, direction = Vector3.back, breaksGuard = true, knockback = 5 });
            Check(heavy.GuardBroken && heavy.Health == 140 && !heavy.shield.activeSelf, "guard break");
            heavy.ReceiveDamage(new DamageInfo { amount = 50, direction = Vector3.back });
            Check(heavy.Health == 90, "broken guard full damage");
            heavy.ReceiveDamage(new DamageInfo { amount = float.NaN });
            Check(heavy.Health == 90, "reject invalid damage");
            heavy.Initialize(player, 16); Check(heavy.Health == 330, "level HP scaling");
            Object.Destroy(heavy.gameObject);

            var shooter = Spawn(EnemyKind.Crossbowman, new Vector3(0, .1f, 4));
            EnemyProjectile Shot()
            {
                var shot = Object.Instantiate(shooter.projectilePrefab, new Vector3(0, 1, 2), Quaternion.identity);
                shot.Launch(shooter, player, Vector3.back, 24, 18); created.Add(shot.gameObject); return shot;
            }
            int frame = Time.frameCount;
            float hp = player.State.Health;
            Shot(); await Task.Delay(500);
            Check(Time.frameCount > frame + 2, "live simulation advances");
            Check(player.State.Health == hp - 18, "projectile hits player");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); created.Add(wall);
            wall.transform.position = new Vector3(0, 1, -1); wall.transform.localScale = new Vector3(4, 3, .3f); Physics.SyncTransforms();
            hp = player.State.Health; Shot(); await Task.Delay(500);
            Check(player.State.Health == hp, "wall blocks projectile");
            Object.Destroy(wall); await Task.Delay(100);
            player.TryDash(Vector3.forward);
            player.ReceiveDamage(new DamageInfo { amount = 18 });
            Check(player.State.Health == hp, "dash damage immunity");
            Object.Destroy(shooter.gameObject);

            var chaser = Spawn(EnemyKind.Swordsman, new Vector3(0, .1f, 5)); chaser.enabled = true;
            float start = Vector3.Distance(chaser.transform.position, player.transform.position);
            await Task.Delay(1000);
            Check(Vector3.Distance(chaser.transform.position, player.transform.position) < start - 1, "chase movement");
            chaser.SetPaused(true); Vector3 stopped = chaser.transform.position; await Task.Delay(300);
            Check(Vector3.Distance(stopped, chaser.transform.position) < .1f, "pause stops movement");
            Object.Destroy(chaser.gameObject);

            await Task.Delay(100);
            var melee = Spawn(EnemyKind.Swordsman, player.transform.position + Vector3.forward * 1.6f);
            melee.enabled = true; hp = player.State.Health;
            await Task.Delay(150); Check(player.State.Health == hp, "melee telegraph before damage");
            await Task.Delay(500); Check(player.State.Health == hp - 12, "melee strike damage");
            Object.Destroy(melee.gameObject);
            await Task.Delay(100);
            var mage = Spawn(EnemyKind.Mage, new Vector3(0, .1f, 5)); mage.enabled = true;
            await Task.Delay(1150);
            Check(Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length == 3, "mage three orb fan");
            Object.Destroy(mage.gameObject);
            foreach (var shot in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(shot.gameObject);

            int total = encounter.Total;
            Check(total == 5, "initial encounter count");
            foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).Where(e => e.transform.IsChildOf(encounter.transform)))
            { enemy.SetPaused(false); enemy.ReceiveDamage(new DamageInfo { amount = 10000, breaksGuard = true, source = scenePlayer.gameObject }); }
            Check(encounter.Defeated == 3 && encounter.merchant.Available, "merchant after third kill");
            await Task.Delay(100);
            Check(!Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).Any(e => e.transform.IsChildOf(encounter.transform)), "safe intermission");
            encounter.merchant.ContinueRound();
            Check(Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).Count(e => e.transform.IsChildOf(encounter.transform)) == 2, "remaining two spawn");
            int completed = 0; encounter.Completed += () => completed++;
            foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).Where(e => e.transform.IsChildOf(encounter.transform)))
                enemy.ReceiveDamage(new DamageInfo { amount = 10000, breaksGuard = true, source = scenePlayer.gameObject });
            Check(encounter.Defeated == 5 && completed == 1, "encounter completes once");
            return "ENEMY_CHECKS_PASSED: " + checks;
        }
        finally
        {
            foreach (var obj in created) if (obj) Object.Destroy(obj);
            Object.Destroy(playerObject);
        }
    }
}
