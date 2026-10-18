using System;
using System.Linq;
using Codice.Enemies;
using Codice.Mechanics;
using UnityEditor;
using UnityEngine;

public static class RoundBalanceChecks
{
    public static string Result { get; private set; } = "Not run";
    [MenuItem("Codice/Mechanics/Validate wave balance")]
    public static string Run()
    {
        int count = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception("Wave balance: " + name); count++; }
        var previousRandom = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(6717);
            Check(RoundRules.Roster(0).Length == 0 && RoundRules.Roster(-1).Length == 0, "invalid rounds empty");
            Check(RoundRules.EnemyCount(1) == 5 && RoundRules.EnemyCount(2) == 8, "introductory counts preserved");
            int[] levels = { 3, 4, 6, 9, 11, 19, 21, 29, 31, 99, 101 };
            int[] limits = { 10, 10, 10, 10, 20, 20, 30, 30, 40, 100, 110 };
            for (int i = 0; i < levels.Length; i++) Check(RoundRules.EnemyCount(levels[i]) == limits[i], "limit boundary " + levels[i]);
            Vector4 previous = Vector4.zero;
            for (int level = 1; level <= 200; level++)
            {
                var roster = RoundRules.Roster(level);
                if (level % 5 == 0)
                {
                    Check(RoundRules.IsBoss(level) && roster.Length == 0 && RoundRules.EnemyLimit(level) == 0, "boss excluded " + level);
                    continue;
                }
                var p = RoundRules.SpawnProbabilities(level);
                Check(roster.Length == RoundRules.EnemyCount(level) && roster.Length <= ((level - 1) / 10 + 1) * 10, "total cap " + level);
                Check(Mathf.Abs(p.x + p.y + p.z + p.w - 1) < .00001f && p.x >= 0 && p.y >= 0 && p.z >= 0 && p.w >= 0, "normalized weights " + level);
                Check(roster.All(k => k == EnemyKind.Swordsman || k == EnemyKind.Crossbowman && level >= 6 || k == EnemyKind.Mage && level >= 11 || k == EnemyKind.Heavy && level >= 16), "no premature spawn " + level);
                Check((p.y > 0) == (level >= 6) && (p.z > 0) == (level >= 11) && (p.w > 0) == (level >= 16), "exact unlock thresholds " + level);
                if (level >= 11)
                    Check(p.z >= previous.z - .000001f && p.w >= previous.w - .000001f && p.x <= previous.x + .000001f && p.y <= previous.y + .000001f, "progressive difficulty " + level);
                for (int kind = 0; kind < 4; kind++)
                {
                    if (p[kind] <= 0) continue;
                    float midpoint = p[kind] * .5f;
                    for (int j = 0; j < kind; j++) midpoint += p[j];
                    Check((int)RoundRules.SelectEnemy(p, midpoint) == kind, "weighted selection " + level + "/" + kind);
                }
                var last = RoundRules.SelectEnemy(p, 1);
                Check(p[(int)last] > 0, "inclusive random endpoint " + level);
                previous = p;
            }
            var rng = new System.Random(6717);
            foreach (int level in new[] { 6, 11, 16, 36, 101 })
            {
                var p = RoundRules.SpawnProbabilities(level); var samples = new int[4];
                for (int i = 0; i < 20000; i++) samples[(int)RoundRules.SelectEnemy(p, (float)rng.NextDouble())]++;
                for (int i = 0; i < 4; i++) Check(Mathf.Abs(samples[i] / 20000f - p[i]) < .015f, "sampled distribution " + level + "/" + i);
            }
            Result = "ROUND_BALANCE_CHECKS_PASSED: " + count;
            Debug.Log(Result); return Result;
        }
        catch (Exception e) { Result = "FAILED: " + e.Message; throw; }
        finally { UnityEngine.Random.state = previousRandom; }
    }
}

