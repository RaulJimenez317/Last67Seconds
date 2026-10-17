using System;
using Codice.Characters;
using UnityEditor;
using UnityEngine;

// Repeatable integration checks. Only run in Play mode; all fixtures are temporary.
public static class CharacterPlayChecks
{
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Requires Play mode.");
        int assertions = 0;
        void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Player.prefab");
        var player = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 10, 0), Quaternion.identity).GetComponent<PlayerCharacter>();
        var merchant = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/01_Prefabs/Characters/Merchant.prefab"), new Vector3(0, 10, 2), Quaternion.identity).GetComponent<Merchant>();
        var wall = new GameObject("Temporary collision test wall");
        try
        {
            int opened = 0, resumed = 0;
            merchant.onIntermissionStarted.AddListener(() => opened++);
            merchant.onRoundContinued.AddListener(() => resumed++);
            Check(!merchant.ReportRoundProgress(1, 2, 5), "Odd wave threshold too early");
            Check(merchant.ReportRoundProgress(1, 3, 5) && opened == 1, "Odd wave midpoint");
            Check(!merchant.ReportRoundProgress(1, 4, 5), "Duplicate intermission");
            Check(merchant.GetOffer(0).tier == 2 && merchant.GetOffer(0).weapon != merchant.GetOffer(1).weapon, "Superior distinct weapons");
            Check(!merchant.TryPurchase(player, 0), "Purchase requires interaction");
            merchant.Interact(player);
            Check(player.ControlsBlocked && merchant.Customer == player, "Shop locks input");
            Check(!merchant.TryPurchase(player, 0) && !merchant.GetOffer(0).sold, "Insufficient funds preserves stock");
            player.State.AddGold(1000);
            Check(!merchant.TryPurchase(player, 3) && player.State.Gold == 1000, "Full-health potion cannot charge gold");
            player.State.Damage(90);
            Check(merchant.TryPurchase(player, 3) && player.State.Health == 90 && player.State.Gold == 920, "Potion transaction");
            Check(!merchant.TryPurchase(player, 3) && player.State.Gold == 920, "Sold item cannot be bought again");
            Check(merchant.TryPurchase(player, 0) && player.State.GetWeapon(1) != null, "Buy second weapon");
            Check(merchant.TryPurchase(player, 2) && player.State.PassiveCount == 1, "Buy passive");
            merchant.CloseShop(); Check(!player.ControlsBlocked && merchant.Available, "Close does not resume round");
            merchant.ContinueRound(); merchant.ContinueRound();
            Check(!merchant.Available && resumed == 1, "Continue fires once");
            Check(!merchant.BeginIntermission(1) && merchant.BeginIntermission(2), "No restock in same round");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false; player.transform.position = new Vector3(0, 10, 0); controller.enabled = true;
            wall.transform.position = new Vector3(0, 11, 1.5f);
            wall.AddComponent<BoxCollider>().size = new Vector3(10, 4, .3f);
            Physics.SyncTransforms(); controller.Move(Vector3.forward * 4);
            Check(player.transform.position.z < 1.1f, "Controller cannot tunnel through wall");
            float health = player.State.Health;
            Check(player.TryDash(Vector3.forward) && !player.TryDash(Vector3.forward), "Dash cooldown");
            player.ReceiveDamage(new DamageInfo { amount = 30 });
            Check(player.State.Health == health, "Dash invulnerability");
            player.State.Damage(1000); Check(!player.TryDash(Vector3.forward) && !player.GetComponent<PlayerCombat>().TryAttack(), "Dead player cannot act");
            Debug.Log("CHARACTER_PLAY_TESTS_OK: " + assertions + " assertions.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player.gameObject);
            UnityEngine.Object.DestroyImmediate(merchant.gameObject);
            UnityEngine.Object.DestroyImmediate(wall);
        }
    }
}
