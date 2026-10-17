#if UNITY_EDITOR
using System;
using System.Collections;
using Codice.Bosses;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;

public sealed class BossPlayChecks : MonoBehaviour
{
    public static string Result { get; private set; } = "Not run";
    int checks;
    public static void Begin()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play in Coliseo first.");
        UnityEditor.EditorApplication.isPaused = false;
        Application.runInBackground = true;
        Result = "RUNNING"; new GameObject("Boss checks").AddComponent<BossPlayChecks>().StartCoroutine("Run");
    }
    void Check(bool value, string label)
    { if (!value) { Result = "FAILED: " + label; throw new Exception(Result); } checks++; }
    IEnumerator Run()
    {
        yield return null;
        yield return null;
        var encounter = FindFirstObjectByType<BossEncounter>();
        Check(encounter && encounter.ActiveBoss, "integrated encounter");
        var player = encounter.player; var boss = encounter.ActiveBoss;
        player.State.Initialize(player.initialClass);
        player.enabled = false; player.GetComponent<PlayerCombat>().enabled = false;
        boss.enabled = false;
        Check(boss.kind == BossKind.GoblinKing && !boss.Evolved, "level 5 goblin");
        Check(boss.GetComponent<NavMeshAgent>().isOnNavMesh, "navigation");
        Check(!encounter.BeginEncounter(10), "no duplicate encounter");
        Check(BossEncounter.KindForLevel(15) == BossKind.GoblinKing && BossEncounter.KindForLevel(30) == BossKind.DemonLord, "cyclic selection");
        boss.ReceiveDamage(new DamageInfo { amount = float.NaN });
        Check(boss.Health == boss.MaxHealth, "reject NaN");
        boss.SetPaused(true); boss.ReceiveDamage(new DamageInfo { amount = 100 });
        Check(boss.Health == boss.MaxHealth, "paused damage"); boss.SetPaused(false);
        boss.Summon(); Check(boss.LivingSummons >= 2 && boss.LivingSummons <= 3, "goblin summons");
        boss.ReceiveDamage(new DamageInfo { amount = boss.MaxHealth * .25f });
        Check(boss.LivingSummons >= 4, "health threshold summons");
        int gold = player.State.Gold, deaths = 0; boss.Died += b => deaths++;
        boss.ReceiveDamage(new DamageInfo { amount = 100000, source = player.gameObject });
        boss.ReceiveDamage(new DamageInfo { amount = 100000, source = player.gameObject });
        Check(deaths == 1 && player.State.Gold == gold + 300, "single death and reward");
        Check(encounter.Completed && !encounter.ActiveBoss, "completion");
        yield return null;
        Check(FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Length == 0, "summon cleanup");
        Check(!encounter.BeginEncounter(6), "reject non-boss level");
        Check(encounter.BeginEncounter(20), "level 20 spawn"); boss = encounter.ActiveBoss; boss.enabled = false;
        Check(boss.kind == BossKind.DemonLord && boss.Evolved && boss.Phase == 1, "evolved demon");
        Check(boss.visual.GetComponentInChildren<Renderer>().sharedMaterial == boss.evolvedMaterial, "evolved texture");
        boss.ReceiveDamage(new DamageInfo { amount = boss.MaxHealth * .35f });
        Check(boss.Phase == 2 && Mathf.Approximately(boss.SpeedMultiplier, 1.3f) && boss.SummonInterval == 15, "medium phase");
        boss.ReceiveDamage(new DamageInfo { amount = boss.MaxHealth * .35f });
        Check(boss.Phase == 3 && Mathf.Approximately(boss.SpeedMultiplier, 1.6f) && boss.SummonInterval == 10, "hard phase");
        boss.Summon(); Check(boss.LivingSummons >= 4, "evolved demon summons");
        player.ApplyBossSlow(.15f); Check(Mathf.Approximately(player.MovementMultiplier, .65f), "mud slow");
        yield return new WaitForSeconds(.2f);
        Check(player.MovementMultiplier == 1, "slow expires");
        Check(player.TryDash(Vector3.forward), "dash available"); float hp = player.State.Health;
        player.ReceiveDamage(new DamageInfo { amount = 35 }); player.ApplyBossKnockdown();
        Check(player.State.Health == hp && !player.IsStaggered, "dash prevents boss damage and knockdown");
        yield return new WaitForSeconds(.35f);
        player.ApplyBossKnockdown(); Check(player.IsStaggered, "rock knockdown");
        yield return new WaitForSeconds(.7f); Check(!player.IsStaggered, "knockdown expires");
        boss.SetPaused(true); var position = boss.transform.position;
        yield return new WaitForSeconds(.3f); Check(boss.transform.position == position, "pause navigation");
        boss.SetPaused(false);
        boss.ReceiveDamage(new DamageInfo { amount = 100000, source = player.gameObject }); yield return null;
        Check(encounter.BeginEncounter(15), "evolved goblin spawn"); boss = encounter.ActiveBoss; boss.enabled = false;
        Check(boss.Evolved && boss.kind == BossKind.GoblinKing, "evolved goblin identity");
        boss.Summon(); Check(boss.LivingSummons >= 3 && boss.LivingSummons <= 4, "evolved goblin summons");
        boss.ReceiveDamage(new DamageInfo { amount = 100000 }); yield return null;
        Check(encounter.BeginEncounter(5), "projectile test encounter"); boss = encounter.ActiveBoss; boss.enabled = false;
        player.State.Heal(10000); hp = player.State.Health;
        var shot = new GameObject("Test rock").AddComponent<BossProjectile>();
        shot.transform.position = player.transform.position + Vector3.up + player.transform.forward * 2;
        shot.Initialize(boss, BossAttack.Rock, -player.transform.forward, 35); boss.Track(shot.gameObject);
        yield return new WaitForSeconds(.25f);
        Check(Mathf.Approximately(player.State.Health, hp - 35), "rock collision damage");
        Check(player.IsStaggered, "rock collision knockdown");
        BossMud.Create(boss, player.transform.position);
        yield return new WaitForSeconds(.15f);
        Check(Mathf.Approximately(player.MovementMultiplier, .65f), "puddle applies slow");
        boss.ReceiveDamage(new DamageInfo { amount = 100000 }); yield return null;
        Check(FindObjectsByType<BossMud>(FindObjectsSortMode.None).Length == 0, "puddle cleanup");
        yield return new WaitForSeconds(.2f);
        Check(player.MovementMultiplier == 1, "cleanup releases slow");
        Result = "BOSS_CHECKS_PASSED: " + checks; Debug.Log(Result); Destroy(gameObject);
    }
}
#endif
