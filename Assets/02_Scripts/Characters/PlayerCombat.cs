using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public LayerMask hitMask = ~0;
        public GameObject[] weaponModels;
        PlayerCharacter player;
        float nextAttack, lastAttack;
        int combo;
        WeaponKind previousKind;
        int previousSlot = -1;
        Vector3 restPosition;
        Transform displayed;
        public int Combo => combo;
        WeaponInstance reloading;
        float reloadElapsed;
        public const float ReloadSeconds = 2;
        public bool IsReloading => reloading != null;
        public float ReloadFraction => IsReloading ? reloadElapsed / ReloadSeconds : 0;
        public bool TryReload()
        {
            var w = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Codice.UI.RunNavigation.Loading || Time.timeScale == 0 || IsReloading ||
                (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) ||
                w == null || w.kind != WeaponKind.Musket || w.ammunition >= w.Capacity || w.reserveAmmunition <= 0) return false;
            reloading = w; reloadElapsed = 0; return true;
        }
        void OnDisable() { reloading = null; reloadElapsed = 0; }
        public WeaponPickup CreatePickup(WeaponInstance weapon, Vector3 position)
        {
            var pickup = new GameObject("Dropped " + weapon.kind + " T" + weapon.tier).AddComponent<WeaponPickup>();
            pickup.weapon = weapon; pickup.transform.position = position;
            var collider = pickup.gameObject.AddComponent<SphereCollider>(); collider.radius = .5f; collider.isTrigger = true;
            AddPickupVisual(pickup);
            return pickup;
        }
        public void AddPickupVisual(WeaponPickup pickup)
        {
            if (pickup.visual) Destroy(pickup.visual);
            int index = (int)pickup.weapon.kind;
            if (weaponModels == null || index >= weaponModels.Length || !weaponModels[index]) return;
            pickup.visual = Instantiate(weaponModels[index], pickup.transform);
            pickup.visual.transform.localPosition = Vector3.zero;
            pickup.visual.transform.localScale = weaponModels[index].transform.lossyScale;
            pickup.visual.transform.localRotation = Quaternion.identity;
            pickup.visual.SetActive(true);
        }
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        void Update()
        {
            var weapon = player.State.ActiveWeapon;
            if (reloading != null)
            {
                if (!player.State.IsAlive || weapon != reloading) { reloading = null; reloadElapsed = 0; }
                else if (!player.ControlsBlocked && !(Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible))
                {
                    reloadElapsed += Time.deltaTime;
                    if (reloadElapsed >= ReloadSeconds)
                    {
                        int rounds = Mathf.Min(reloading.Capacity - reloading.ammunition, reloading.reserveAmmunition);
                        reloading.ammunition += rounds; reloading.reserveAmmunition -= rounds;
                        reloading = null; reloadElapsed = 0; player.State.NotifyChanged();
                    }
                }
            }
            if (weapon == null) return;
            UpdateWeapon(weapon);
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) TryReload();
            if (displayed && !GetComponent<Codice.Presentation.ActorPresentation>())
            {
                float phase = Mathf.Clamp01((Time.time - lastAttack) / .25f);
                displayed.localRotation = Quaternion.Euler(-30 * Mathf.Sin(phase * Mathf.PI), 0, -25 * Mathf.Sin(phase * Mathf.PI));
                displayed.localPosition = restPosition + Vector3.forward * (.15f * Mathf.Sin(phase * Mathf.PI));
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0)
                TryAttack();
        }
        void UpdateWeapon(WeaponInstance weapon)
        {
            if (previousSlot == player.State.ActiveSlot && previousKind == weapon.kind) return;
            previousSlot = player.State.ActiveSlot; previousKind = weapon.kind; combo = 0;
            displayed = null;
            if (weaponModels == null) return;
            for (int i = 0; i < weaponModels.Length; i++)
            {
                if (!weaponModels[i]) continue;
                weaponModels[i].SetActive(i == (int)weapon.kind);
                if (i == (int)weapon.kind) { displayed = weaponModels[i].transform; restPosition = displayed.localPosition; }
            }
        }
        public bool TryAttack()
        {
            if (Codice.UI.RunNavigation.Loading || Codice.UI.RunHUD.HandlesEscapeThisFrame) return false;
            if (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) return false;
            var weapon = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Time.timeScale == 0 || !player.view || weapon == null || Time.time < nextAttack) return false;
            if (IsReloading) return false;
            if (!player.State.ConsumeAmmo()) { TryReload(); return false; }
            if (Time.time - lastAttack > 1.2f) combo = 0;
            combo = combo % 3 + 1;
            lastAttack = Time.time; nextAttack = Time.time + 1 / (weapon.AttacksPerSecond * player.State.AttackSpeedMultiplier);
            var mechanics = GetComponent<Codice.Mechanics.PlayerMechanics>();
            if (weapon.kind == WeaponKind.Shield && mechanics) mechanics.RaiseShield();
            GetComponent<Codice.Presentation.ActorPresentation>()?.Attack(weapon.Ranged, 1 / (weapon.AttacksPerSecond * player.State.AttackSpeedMultiplier));
            var origin = player.view.transform.position;
            var forward = player.view.transform.forward;
            if (weapon.Ranged)
            {
                if (weapon.kind != WeaponKind.Bow)
                {
                    Vector3 muzzle=displayed?displayed.position:origin+forward*.6f;
                    // Imported staff and musket meshes extend along their local Y axis.
                    // Keep the flash on the animated weapon tip, rather than at the hand.
                    var mesh=displayed?displayed.GetComponentInChildren<MeshFilter>():null;
                    if(mesh&&mesh.sharedMesh)
                    {
                        var bounds=mesh.sharedMesh.bounds;
                        muzzle=mesh.transform.TransformPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
                    }
                    Codice.Presentation.PresentationVfx.Muzzle(muzzle,forward);
                }
                if (weapon.kind == WeaponKind.Musket)
                {
                    var hits = Physics.RaycastAll(origin, forward, 60, hitMask, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    var struck = new HashSet<IDamageReceiver>();
                    foreach (var hit in hits)
                    {
                        if (hit.transform.IsChildOf(transform)) continue;
                        Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                        var receiver = hit.collider.GetComponentInParent<IDamageReceiver>();
                        if (receiver == null) break;
                        if (struck.Add(receiver)) Hit(hit.collider, weapon, forward);
                        if (!Codice.Mechanics.CombatTargets.IsEnemy(receiver as Component)) break;
                    }
                }
                else
                {
                    var shot = new GameObject("Player projectile").AddComponent<PlayerProjectile>();
                    shot.transform.position = origin;
                    float damage = mechanics ? mechanics.RollBasicDamage(weapon) : weapon.Damage;
                    shot.Initialize(gameObject, forward, damage, hitMask, damage > weapon.Damage * 1.5f);
                }
            }
            else
            {
                float range = weapon.kind == WeaponKind.Polearm ? 3.5f : weapon.kind == WeaponKind.Sword && combo == 3 ? 2.8f : 2.2f;
                bool wide = weapon.kind == WeaponKind.Axe || weapon.kind == WeaponKind.Hammer || weapon.kind == WeaponKind.Scythe;
                var receivers = new HashSet<IDamageReceiver>();
                foreach (var collider in Physics.OverlapSphere(transform.position + Vector3.up, range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (collider.transform.IsChildOf(transform)) continue;
                    Vector3 point = collider.ClosestPoint(origin);
                    Vector3 offset = point - origin;
                    if (Vector3.Angle(forward, offset) > (wide ? 90 : 40)) continue;
                    // Cover blocks melee damage too. A multi-collider receiver is hit only once.
                    if (Physics.Raycast(origin, offset.normalized, out var obstruction, offset.magnitude, hitMask, QueryTriggerInteraction.Ignore) &&
                        obstruction.collider.GetComponentInParent<IDamageReceiver>() != collider.GetComponentInParent<IDamageReceiver>()) continue;
                    var receiver = collider.GetComponentInParent<IDamageReceiver>();
                    if (receiver != null && receivers.Add(receiver)) Hit(collider, weapon, offset.normalized);
                }
            }
            return true;
        }
        void Hit(Collider collider, WeaponInstance weapon, Vector3 direction)
        {
            if (collider.transform.IsChildOf(transform)) return;
            float damage = GetComponent<Codice.Mechanics.PlayerMechanics>()?.RollBasicDamage(weapon) ?? weapon.Damage;
            collider.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo
            {
                amount = damage, critical = damage > weapon.Damage * 1.5f, hasImpactPoint = true, impactPoint = collider.ClosestPoint(player.view.transform.position), direction = direction, source = gameObject, melee = !weapon.Ranged,
                breaksGuard = weapon.kind == WeaponKind.Axe || weapon.kind == WeaponKind.Hammer,
                knockback = weapon.kind == WeaponKind.Axe ? 5 : 0
            });
        }
    }
}
