using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Mechanics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MechanicsChecks
{
    public static string Result { get; private set; } = "Not run";
    static int count;
    static MechanicsChecks()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MechanicsChecksArmed", false))
            { SessionState.SetBool("MechanicsChecksArmed", false); Begin(); }
        };
    }
    public static void Arm() { SessionState.SetBool("MechanicsChecksArmed", true); }
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL: " + message); count++; }
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .02f, message + " actual=" + actual + " expected=" + expected);
    public static string Data()
    {
        count = 0;
        int[] rounds = { 1, 2, 3, 4, 6, 7, 8, 9, 11, 12, 13, 14, 16, 17, 18, 19 };
        int[] totals = { 5, 8, 10, 10, 10, 10, 10, 10, 20, 20, 20, 20, 20, 20, 20, 20 };
        for (int i = 0; i < rounds.Length; i++) Check(RoundRules.Roster(rounds[i]).Length == totals[i], "roster " + rounds[i]);
        Check(RoundRules.Roster(5).Length == 0 && RoundRules.IsBoss(20), "boss levels exclude dungeon");
        Check(RoundRules.Roster(3).All(k => k == EnemyKind.Swordsman), "round three only swordsmen");
        var state = new CharacterState(); state.Initialize(PlayerClass.Archer);
        state.ConsumeAmmo(); state.EquipPassive(PassiveKind.Frenzy); state.ActivatePassive(0); state.TickAmmunition(1.125f);
        Check(state.ActiveWeapon.ammunition == 8, "frenzy arrow regeneration");
        Near(state.AttackSpeedMultiplier, 1.25f, "frenzy attack speed");
        state.EquipPassive(PassiveKind.MercuryBoots); state.ActivatePassive(1); Near(state.MoveSpeed, 9.6f, "mercury speed");
        state.EquipPassive(PassiveKind.Overload); Check(!state.EquipPassive(PassiveKind.ThornArmor), "fourth passive needs replacement");
        Check(state.EquipPassive(PassiveKind.ThornArmor, 0) && !state.HasPassive(PassiveKind.Frenzy), "replace passive removes old effect");
        Check(!state.EquipPassive(PassiveKind.ThornArmor, 1), "reject duplicate");
        state.AddGold(231); state.Damage(15); state.ActiveWeapon.ultimateCharge = 63;
        state.Equip(new WeaponInstance(WeaponKind.Musket, 3)); state.SelectWeapon(0);
        var restored = new CharacterState(); restored.Initialize(PlayerClass.Tank); restored.Restore(state.Capture());
        Check(restored.Class == PlayerClass.Archer && restored.Gold == 231 && restored.Health == 85 && restored.PassiveCount == 3, "snapshot resources and class");
        Check(restored.GetWeapon(1).kind == WeaponKind.Musket && restored.ActiveWeapon.ultimateCharge == 63, "snapshot weapons and charge");
        Check(new WeaponInstance(WeaponKind.Bow, 4).Capacity == 12, "tier four quiver");
        Near(new WeaponInstance(WeaponKind.Sword, 4).AttacksPerSecond, 3.4f, "tier four cadence");
        return "MECHANICS_DATA_PASSED: " + count;
    }
    public static async void Begin()
    {
        if (Result == "Running") return;
        Result = "Running";
        try { Data(); await Runtime(); Result = "MECHANICS_CHECKS_PASSED: " + count; Debug.Log(Result); }
        catch (Exception e) { Result = e.ToString(); Debug.LogError(Result); }
    }
    static async Task Runtime()
    {
        Application.runInBackground = true;
        await Task.Delay(700);
        var director = RunDirector.Instance; Check(director && director.Player, "run initialized");
        director.enabled = false;
        foreach (var e in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None)) e.SetPaused(true);
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Player.prefab"));
        var player = root.GetComponent<PlayerCharacter>(); player.enabled = false;
        root.GetComponent<PlayerCombat>().enabled = false;
        foreach (var c in root.GetComponentsInChildren<Camera>()) c.enabled = false;
        foreach (var c in root.GetComponentsInChildren<AudioListener>()) c.enabled = false;
        var controller = root.GetComponent<CharacterController>(); controller.enabled = false; root.transform.position = new Vector3(0, .1f, -4); controller.enabled = true;
        var mechanics = root.GetComponent<PlayerMechanics>();
        var created = new List<GameObject>();
        BasicEnemy Enemy(EnemyKind kind, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Enemies/" + kind + ".prefab");
            var enemy = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<BasicEnemy>();
            enemy.Initialize(player); enemy.enabled = false; created.Add(enemy.gameObject); return enemy;
        }
        try
        {
            player.State.Initialize(PlayerClass.Warrior); player.State.EquipPassive(PassiveKind.ThornArmor); player.State.ActivatePassive(0);
            var attacker = Enemy(EnemyKind.Swordsman, new Vector3(2, .1f, -4));
            player.ReceiveDamage(new DamageInfo { amount = 20, melee = true, source = attacker.gameObject });
            Near(player.State.Health, 104, "thorn armor reduction"); Near(attacker.Health, 30, "thorn armor reflection");
            player.ReceiveDamage(new DamageInfo { amount = 20, source = attacker.gameObject }); Near(attacker.Health, 30, "ranged hit not reflected");
            Object.Destroy(attacker.gameObject);
            player.State.Initialize(PlayerClass.Warrior); player.State.Equip(new WeaponInstance(WeaponKind.Shield)); mechanics.RaiseShield();
            player.ReceiveDamage(new DamageInfo { amount = 20, direction = Vector3.back }); Near(player.State.Health, 110, "frontal shield reduction");
            player.ReceiveDamage(new DamageInfo { amount = 20, direction = Vector3.forward }); Near(player.State.Health, 90, "shield rear vulnerability");
            player.State.Initialize(PlayerClass.Warrior); player.State.EquipPassive(PassiveKind.VampiricHeart); player.State.ActivatePassive(0); player.State.Damage(50);
            player.State.EquipPassive(PassiveKind.Overload); player.State.ActivatePassive(1);
            var victim = Enemy(EnemyKind.Heavy, Vector3.zero);
            victim.ReceiveDamage(new DamageInfo { amount = 1000, breaksGuard = true, source = root });
            Near(player.State.Health, 74.8f, "vampiric kill heal"); Near(mechanics.Charge, 18, "heavy charge with overload");
            for (int i = 0; i < 10; i++) mechanics.BossHit();
            Near(player.State.Health, 79.6f, "every tenth boss hit heal"); Near(mechanics.Charge, 48, "boss charge with overload");
            player.State.Initialize(PlayerClass.Warrior); player.State.EquipPassive(PassiveKind.MercuryBoots); player.State.ActivatePassive(0);
            Check(player.TryDash(Vector3.forward), "first dash"); await Task.Delay(350);
            Check(player.TryDash(Vector3.forward), "mercury second dash"); await Task.Delay(350);
            Check(!player.TryDash(Vector3.forward), "dash charges exhausted");
            var pickup = LootPickup.Create(player.transform.position, LootKind.Ammunition, 3); created.Add(pickup.gameObject);
            player.State.Equip(new WeaponInstance(WeaponKind.Bow)); player.State.ConsumeAmmo();
            pickup.respawns = true; pickup.respawnSeconds = .2f;
            Check(pickup.TryCollect(player) && !pickup.TryCollect(player), "supply collects once");
            await Task.Delay(300); player.State.ConsumeAmmo(); Check(pickup.TryCollect(player), "supply regenerates");
            var pillarObj = GameObject.CreatePrimitive(PrimitiveType.Cube); created.Add(pillarObj); pillarObj.transform.position = new Vector3(10, 1, 0);
            var pillar = pillarObj.AddComponent<DestructiblePillar>(); pillar.ReceiveDamage(new DamageInfo { amount = 180 });
            Check(!pillar.gameObject.activeSelf && pillar.Health == 0, "pillar destruction removes collider");
            // Every weapon special must consume charge, perform its effect and finish without errors.
            foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
            {
                mechanics.Cancel(); player.State.Initialize(PlayerClass.Warrior);
                player.State.Equip(new WeaponInstance(kind, 1)); player.State.ActiveWeapon.ultimateCharge = 100;
                var target = Enemy(EnemyKind.Heavy, player.transform.position + Vector3.forward * 2);
                Physics.SyncTransforms(); float initial = target.Health;
                Check(mechanics.TryUltimate() && player.State.ActiveWeapon.ultimateCharge == 0, "activate " + kind);
                Check(!mechanics.TryUltimate(), "reject duplicate " + kind);
                if (kind == WeaponKind.Dagger)
                { Check(mechanics.ShadowStep && mechanics.RollBasicDamage(player.State.ActiveWeapon) == 24, "dagger guaranteed critical"); }
                else
                {
                    if (kind == WeaponKind.Sword || kind == WeaponKind.Shield)
                    { float hp = player.State.Health; player.ReceiveDamage(new DamageInfo { amount = 50 }); Near(player.State.Health, hp, "special invulnerability " + kind); }
                    await Task.Delay(kind == WeaponKind.Shield ? 4200 : kind == WeaponKind.Staff ? 4300 : kind == WeaponKind.Sword ? 2200 : kind == WeaponKind.Bow ? 1300 : 100);
                    Check(!target || target.Health < initial, "special damages enemy " + kind);
                }
                mechanics.Cancel(); if (target) Object.Destroy(target.gameObject); await Task.Delay(50);
            }
            // Real basic musket penetration, with a solid wall stopping the next target.
            controller.enabled = false; player.transform.position = new Vector3(0, .1f, -4); controller.enabled = true;
            player.State.Initialize(PlayerClass.Warrior); player.State.Equip(new WeaponInstance(WeaponKind.Musket));
            var first = Enemy(EnemyKind.Heavy, new Vector3(0, .1f, -2)); var second = Enemy(EnemyKind.Heavy, new Vector3(0, .1f, 0));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); created.Add(wall); wall.transform.position = new Vector3(0, 1, 2); wall.transform.localScale = new Vector3(3, 3, .3f);
            var third = Enemy(EnemyKind.Heavy, new Vector3(0, .1f, 4)); Physics.SyncTransforms();
            Check(root.GetComponent<PlayerCombat>().TryAttack(), "musket fires");
            Near(first.Health, 125, "musket first target"); Near(second.Health, 125, "musket penetrates second"); Near(third.Health, 220, "wall stops musket");
            var splash = Enemy(EnemyKind.Heavy, new Vector3(1.4f, .1f, -2));
            player.State.ActiveWeapon.ultimateCharge = 100; Physics.SyncTransforms();
            Check(mechanics.TryUltimate(), "musket special ready"); await Task.Delay(100);
            Check(splash.Health < 220, "musket explosion hits off-axis target");
        }
        finally
        {
            foreach (var obj in created) if (obj) Object.Destroy(obj);
            Object.Destroy(root); director.enabled = true;
        }
        await Progression();
    }
    static async Task Progression()
    {
        var director = RunDirector.Instance;
        Check(director.Level == 1 && director.Player.State.IsAlive, "start progression alive");
        director.Player.enabled = false; director.Player.GetComponent<PlayerCombat>().enabled = false;
        director.Player.State.EquipPassive(PassiveKind.MercuryBoots);
        for (int round = 1; round <= 4; round++)
        {
            var wave = Object.FindFirstObjectByType<BasicEnemyEncounter>();
            int guard = 0;
            while (wave.Defeated < wave.Total && guard++ < 50)
            {
                foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))
                { enemy.SetPaused(false); enemy.ReceiveDamage(new DamageInfo { amount = 10000, breaksGuard = true, source = director.Player.gameObject }); }
                if (wave.merchant.Available)
                {
                    Check(director.Phase == RunPhase.Shop, "round " + round + " shop pauses timer");
                    float elapsed = director.RoundElapsed; await Task.Delay(120); Near(director.RoundElapsed, elapsed, "shop timer frozen");
                    wave.merchant.ContinueRound();
                }
                await Task.Delay(50);
            }
            Check(wave.Defeated == wave.Total, "complete round " + round);
            if (round < 4) { await WaitFor(() => director.Level == round + 1); Check(director.Level == round + 1, "advance round"); }
        }
        Check(director.Level == 4 && director.Phase == RunPhase.Cleared && Object.FindFirstObjectByType<RunPortal>().Available, "boss portal unlock");
        int gold = director.Player.State.Gold;
        director.Player.State.Equip(new WeaponInstance(WeaponKind.Musket, 3));
        for (int i = 0; i < 3; i++) director.Player.State.ConsumeAmmo();
        director.Player.State.Damage(7);
        Check(director.Travel() && !director.Travel(), "single scene transition");
        await WaitFor(() => director.Player && SceneManager.GetActiveScene().name == "Coliseo" && director.Phase == RunPhase.Combat);
        var arena = Object.FindFirstObjectByType<BossEncounter>(); arena.ActiveBoss.SetPaused(true);
        Check(director.Level == 5 && arena.ActiveBoss.kind == BossKind.GoblinKing, "level five boss");
        Check(director.Player.State.Gold == gold && Enumerable.Range(0,director.Player.State.PassiveCount).Any(i=>director.Player.State.GetPassive(i)==PassiveKind.MercuryBoots), "travel preserves inventory");
        Check(director.Player.State.ActiveWeapon.kind == WeaponKind.Musket && director.Player.State.ActiveWeapon.tier == 3 && director.Player.State.ActiveWeapon.ammunition == 3, "travel preserves weapon and ammunition");
        Near(director.Player.State.Health, 113, "travel preserves health");
        Check(Object.FindObjectsByType<DestructiblePillar>(FindObjectsSortMode.None).Length == 4, "four destructible pillars");
        Check(Object.FindObjectsByType<LootPickup>(FindObjectsSortMode.None).Count(p => p.respawns && p.respawnSeconds == 15) == 3, "three fifteen-second supplies");
        arena.ActiveBoss.SetPaused(false); arena.ActiveBoss.ReceiveDamage(new DamageInfo { amount = 100000, source = director.Player.gameObject });
        int rewarded = director.Player.State.Gold; arena.onCompleted.Invoke(); Check(director.Player.State.Gold == rewarded, "no duplicate speed bonus");
        Check(director.Phase == RunPhase.Cleared && Object.FindFirstObjectByType<RunPortal>().Available, "boss unlocks return portal");
        Check(director.Travel(), "return travel");
        await WaitFor(() => director.Player && SceneManager.GetActiveScene().name == "Calabozo" && director.Level == 6 && director.Phase == RunPhase.Combat);
        foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
        Check(Object.FindFirstObjectByType<BasicEnemyEncounter>().Total == 10, "round six roster");
        Check(director.Player.State.Gold == gold + 400, "boss reward and one speed bonus");
        director.Player.State.Damage(10000);
        Check(director.Phase == RunPhase.Defeated && director.RestartRun() && !director.RestartRun(), "defeat and duplicate restart guard");
        await WaitFor(() => director.Player && director.Level == 1 && director.Phase == RunPhase.Combat);
        Check(director.Player.State.IsAlive && director.Player.State.Gold == 0 && director.Player.State.PassiveCount == 0, "new run resets state");
        foreach (var enemy in Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
    }
    static async Task WaitFor(Func<bool> condition)
    {
        for (int i = 0; i < 200; i++) { if (condition()) return; await Task.Delay(50); }
        throw new Exception("Scene transition timed out");
    }
}
