using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Characters;
using Codice.Bosses;
using Codice.Mechanics;
using Codice.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameplayRepairChecks
{
    public static string Result = "Not run";
    static int count;
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); count++; }
    static async Task Until(Func<bool> ready)
    {
        float deadline = Time.realtimeSinceStartup + 30;
        while (!ready()) { if (Time.realtimeSinceStartup > deadline) throw new Exception("Timeout"); await Task.Delay(50); }
    }
    public static async void Run()
    {
        if (!Application.isPlaying || Result == "Running") return;
        Result = "Running"; count = 0; Application.runInBackground = true;
        try
        {
            RunNavigation.StartGame();
            await Until(() => !RunNavigation.Loading && RunHUD.Instance && RunDirector.Instance.Player);
            var p = RunDirector.Instance.Player; var s = p.State;
            foreach (var enemy in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
            var pickup = LootPickup.Create(p.transform.position + Vector3.right * 1.5f, LootKind.Passive, 1);
            pickup.passive = PassiveKind.Frenzy;
            await Task.Delay(80);
            pickup.transform.position = p.transform.position + Vector3.up * .7f;
            Physics.SyncTransforms();
            await Until(() => s.PassiveCount == 1);
            Check(!s.HasPassive(PassiveKind.Frenzy), "Pickup must wait for activation");
            Check(p.TryActivatePassive(0) && s.HasPassive(PassiveKind.Frenzy), "T slot applies Frenzy");
            Check(!p.TryActivatePassive(0), "Cooldown blocks repeat");
            s.EquipPassive(PassiveKind.MercuryBoots); s.EquipPassive(PassiveKind.ThornArmor);
            Check(p.TryActivatePassive(1) && p.TryActivatePassive(2), "Y and U slots");
            Check(s.MoveSpeed > 6.5f && p.GetComponent<PlayerMechanics>().ModifyIncoming(new DamageInfo { amount = 100 }) == 80, "Buffs reach gameplay");
            RunHUD.Instance.SetPaused(true); float remaining = s.PassiveTime(0);
            await Task.Delay(300);
            Check(s.PassiveTime(0) == remaining && !p.TryActivatePassive(0), "Pause freezes buffs");
            RunHUD.Instance.SetPaused(false); s.TickPassives(11);
            Check(!s.HasPassive(PassiveKind.Frenzy) && !s.HasPassive(PassiveKind.MercuryBoots), "Effects expire");
            s.TickPassives(20); Check(p.TryActivatePassive(0), "Cooldown expires");
            var combat = p.GetComponent<PlayerCombat>(); var gun = new WeaponInstance(WeaponKind.Musket); s.Equip(gun);
            await Task.Delay(80);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Raycast test target";
            target.transform.position = p.view.transform.position + p.view.transform.forward * 2;
            target.transform.localScale = Vector3.one * .5f;
            var damageTarget = target.AddComponent<DestructiblePillar>();
            Physics.SyncTransforms();
            Check(combat.TryAttack() && gun.ammunition == 5, "Musket consumes one round");
            Check(damageTarget.Health == damageTarget.maxHealth - gun.Damage, "Shot follows camera aim and deals damage once");
            Check(!combat.TryAttack() && gun.ammunition == 5, "Fire rate enforced");
            Check(combat.TryReload() && !combat.TryAttack(), "Reload blocks firing");
            RunHUD.Instance.SetPaused(true); float progress = combat.ReloadFraction; await Task.Delay(250);
            Check(combat.ReloadFraction == progress, "Pause freezes reload"); RunHUD.Instance.SetPaused(false);
            await Until(() => !combat.IsReloading);
            Check(gun.ammunition == 6 && gun.reserveAmmunition == 17, "Reload transfers reserve");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = p.view.transform.position + p.view.transform.forward;
            wall.transform.localScale = Vector3.one * .5f; Physics.SyncTransforms();
            float targetHealth = damageTarget.Health;
            Check(combat.TryAttack() && damageTarget.Health == targetHealth, "World geometry blocks musket");
            Object.Destroy(wall); Object.Destroy(target);
            gun.ammunition = 0; gun.reserveAmmunition = 0;
            Check(!combat.TryReload() && !combat.TryAttack(), "Empty reserve blocks reload and shot");
            Check(s.AddAmmunition(3) && combat.TryReload(), "Ammo pickup supplies reload");
            s.SelectWeapon(0); await Until(() => !combat.IsReloading); Check(!combat.IsReloading, "Switch cancels reload");
            Check(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "Single EventSystem");
            RunHUD.Instance.SetPaused(true);
            await Task.Delay(80);
            Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == "IR AL MENÚ PRINCIPAL").onClick.Invoke();
            await Until(() => !RunNavigation.Loading && SceneManager.GetActiveScene().name == "MainMenu");
            Check(Time.timeScale == 1 && !RunDirector.Instance, "Pause to menu resets time and run");
            SceneManager.LoadScene("Coliseo");
            await Until(() => RunHUD.Instance && Object.FindFirstObjectByType<BossCharacter>());
            var boss = Object.FindFirstObjectByType<BossCharacter>();
            await Until(() => boss.State == BossState.Windup);
            boss.ApplyStun(.3f);
            Check(boss.State == BossState.Stunned && boss.CurrentAttack == BossAttack.None, "Stun cancels attack");
            await Until(() => boss.State == BossState.Recovery);
            await Until(() => boss.State == BossState.Windup);
            boss.SetPaused(true); await Task.Delay(300);
            Check(boss.State == BossState.Windup, "Boss pause preserves windup");
            boss.SetPaused(false); await Until(() => boss.State == BossState.Recovery);
            Check(boss.CurrentAttack == BossAttack.None, "Attack enters recovery");
            float hp = boss.Health;
            boss.ReceiveDamage(new DamageInfo { amount = 10, source = RunDirector.Instance.Player.gameObject });
            Check(boss.Health == hp - 10, "Boss receives exact damage");
            boss.ReceiveDamage(new DamageInfo { amount = float.NaN });
            Check(boss.Health == hp - 10, "Invalid damage rejected");
            boss.ReceiveDamage(new DamageInfo { amount = 10000, source = RunDirector.Instance.Player.gameObject });
            await Until(() => RunHUD.Instance.ResultsVisible);
            Check(RunDirector.Instance.Phase == RunPhase.Cleared, "Boss death completes encounter");
            Result = "GAMEPLAY_REPAIRS_PASSED: " + count; Debug.Log(Result);
        }
        catch (Exception e) { Result = "FAILED: " + e; Debug.LogError(Result); }
    }
}
