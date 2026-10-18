using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
using Codice.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RoundBalancePlayChecks
{
    public static string Result { get; private set; } = "Not run";
    static int count;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
    static async Task Until(Func<bool> ready)
    {
        float end = Time.realtimeSinceStartup + 30;
        while (!ready()) { if (Time.realtimeSinceStartup > end) throw new Exception("Timed out waiting for wave/scene"); await Task.Delay(50); }
    }
    [MenuItem("Codice/Mechanics/Validate wave progression (Play)")]
    public static async void Run()
    {
        if (!Application.isPlaying || Result == "Running") return;
        Result = "Running"; count = 0; Application.runInBackground = true;
        GameObject fixture = null;
        try
        {
            RunNavigation.StartGame(PlayerClass.Warrior);
            await Until(() => !RunNavigation.Loading && RunDirector.Instance && RunDirector.Instance.Level == 1 && RunDirector.Instance.Player);
            var run = RunDirector.Instance;
            run.Player.enabled = false; run.Player.GetComponent<PlayerCombat>().enabled = false;
            for (int round = 1; round <= 4; round++)
            {
                var wave = Object.FindFirstObjectByType<BasicEnemyEncounter>();
                Check(wave.Total == RoundRules.EnemyCount(round), "actual total round " + round);
                Check(wave.composition.All(k => k == EnemyKind.Swordsman), "only swords before six");
                bool shopChecked = false; float end = Time.realtimeSinceStartup + 15;
                while (wave.Defeated < wave.Total)
                {
                    if (Time.realtimeSinceStartup > end) throw new Exception("Wave did not complete");
                    foreach (var enemy in wave.GetComponentsInChildren<BasicEnemy>())
                    {
                        if (!enemy.IsAlive) continue;
                        enemy.SetPaused(false); enemy.ReceiveDamage(new DamageInfo { amount = 100000, source = run.Player.gameObject, ignoreGuard = true });
                    }
                    if (wave.merchant.Available)
                    {
                        Check(!shopChecked && wave.Defeated == (wave.Total + 1) / 2 && run.Phase == RunPhase.Shop, "merchant midpoint " + round);
                        shopChecked = true; wave.merchant.ContinueRound();
                    }
                    await Task.Delay(50);
                }
                Check(shopChecked && run.Phase == RunPhase.Cleared, "merchant and completion round " + round);
                if (round < 4) await Until(() => run.Level == round + 1);
            }
            Check(Object.FindFirstObjectByType<RunPortal>().Available && run.Travel(), "portal to unchanged boss round five");
            await Until(() => !RunNavigation.Loading && RunHUD.Instance && RunHUD.Instance.Context.IsArena && RunHUD.Instance.Context.arena.ActiveBoss);
            var boss = RunHUD.Instance.Context.arena.ActiveBoss;
            Check(run.Level == 5 && boss.kind == Codice.Bosses.BossKind.GoblinKing && boss.MaxHealth == 1200 && !boss.Evolved, "level five boss configuration");
            boss.ReceiveDamage(new DamageInfo { amount = 100000, source = run.Player.gameObject });
            Check(run.Travel(), "boss return portal");
            await Until(() => RunHUD.Instance && !RunHUD.Instance.Context.IsArena && run.Level == 6 && run.Player);
            var actual = RunHUD.Instance.Context.dungeon;
            Check(actual.Total == 10 && actual.composition.All(k => k == EnemyKind.Swordsman || k == EnemyKind.Crossbowman), "round six cap and unlocks");
            foreach (var enemy in actual.GetComponentsInChildren<BasicEnemy>()) enemy.SetPaused(true);
            run.Player.enabled = false; run.Player.GetComponent<PlayerCombat>().enabled = false;
            foreach (int level in new[] { 16, 21 })
            {
                fixture = new GameObject("Wave balance fixture " + level);
                var wave = fixture.AddComponent<BasicEnemyEncounter>(); wave.startOnPlay = false;
                wave.prefabs = actual.prefabs; wave.spawnPoints = actual.spawnPoints; wave.player = run.Player; wave.maxConcurrent = actual.maxConcurrent;
                int completed = 0; wave.Completed += () => completed++;
                Check(wave.BeginEncounter(level, RoundRules.Roster(level)), "spawn advanced wave " + level);
                float end = Time.realtimeSinceStartup + 15;
                while (wave.Defeated < wave.Total)
                {
                    if (Time.realtimeSinceStartup > end) throw new Exception("Advanced wave did not complete");
                    var enemies = wave.GetComponentsInChildren<BasicEnemy>().Where(e => e.IsAlive).ToArray();
                    Check(enemies.Length > 0 && enemies.Length <= wave.maxConcurrent, "bounded live population " + level);
                    foreach (var enemy in enemies)
                    { enemy.SetPaused(false); enemy.ReceiveDamage(new DamageInfo { amount = 100000, source = run.Player.gameObject, ignoreGuard = true }); }
                    await Task.Delay(50);
                }
                Check(wave.Total == (level == 16 ? 20 : 30) && completed == 1, "advanced wave completes once " + level);
                Object.Destroy(fixture); fixture = null;
            }
            Result = "ROUND_BALANCE_PLAY_PASSED: " + count; Debug.Log(Result);
        }
        catch (Exception e) { Result = "FAILED: " + e; Debug.LogError(Result); }
        finally { if (fixture) Object.Destroy(fixture); }
    }
}
