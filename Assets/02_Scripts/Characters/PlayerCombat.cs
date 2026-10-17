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
            pickup.visual.transform.localRotation = Quaternion.identity;
            pickup.visual.SetActive(true);
        }
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        void Update()
        {
            var weapon = player.State.ActiveWeapon;
            if (weapon == null) return;
            UpdateWeapon(weapon);
            if (displayed)
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
            var weapon = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || !player.view || weapon == null || Time.time < nextAttack) return false;
            if (!player.State.ConsumeAmmo()) return false;
            if (Time.time - lastAttack > 1.2f) combo = 0;
            combo = combo % 3 + 1;
            lastAttack = Time.time; nextAttack = Time.time + 1 / weapon.AttacksPerSecond;
            var origin = player.view.transform.position;
            var forward = player.view.transform.forward;
            if (weapon.Ranged)
            {
                if (weapon.kind == WeaponKind.Musket)
                {
                    if (Physics.Raycast(origin, forward, out var hit, 60, hitMask, QueryTriggerInteraction.Ignore))
                        Hit(hit.collider, weapon, forward);
                }
                else
                {
                    var shot = new GameObject("Player projectile").AddComponent<PlayerProjectile>();
                    shot.transform.position = origin;
                    shot.Initialize(gameObject, forward, weapon.Damage, hitMask);
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
            collider.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo
            {
                amount = weapon.Damage, direction = direction, source = gameObject,
                breaksGuard = weapon.kind == WeaponKind.Axe || weapon.kind == WeaponKind.Hammer,
                knockback = weapon.kind == WeaponKind.Axe ? 5 : 0
            });
        }
    }
}
