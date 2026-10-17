using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerMechanics : MonoBehaviour
    {
        public event Action Changed;
        public bool UltimateActive { get; private set; }
        public bool Invulnerable => Time.time < invulnerableUntil;
        public bool ShieldActive => Time.time < shieldUntil && player.State.ActiveWeapon?.kind == WeaponKind.Shield;
        public bool ShadowStep => Time.time < shadowUntil;
        public float Charge => player.State.ActiveWeapon?.ultimateCharge ?? 0;
        PlayerCharacter player;
        float invulnerableUntil, shadowUntil, shieldUntil;
        int bossHits;
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        void OnEnable() { player.SpecialRequested += ActivateFromInput; player.State.Died += Cancel; }
        void OnDisable() { player.SpecialRequested -= ActivateFromInput; player.State.Died -= Cancel; Cancel(); }
        void ActivateFromInput() { if (!TryUltimate()) Debug.Log("Especial: necesita 100% de carga y control activo.", this); }
        public void Cancel()
        { StopAllCoroutines(); UltimateActive = false; invulnerableUntil = shadowUntil = shieldUntil = 0; bossHits = 0; }
        void Update()
        {
            if (!player.State.IsAlive || Time.timeScale == 0 || player.ControlsBlocked) return;
            bool combat = RunDirector.Instance && RunDirector.Instance.Player == player && RunDirector.Instance.CombatActive;
            if (combat) AddCharge(1.5f * Time.deltaTime);
        }
        public void AddCharge(float amount)
        {
            var weapon = player.State.ActiveWeapon;
            if (weapon == null || !player.State.IsAlive || UltimateActive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            bool ready = weapon.ultimateCharge >= 100;
            weapon.ultimateCharge = Mathf.Clamp(weapon.ultimateCharge + amount * (player.State.HasPassive(PassiveKind.Overload) ? 1.5f : 1), 0, 100);
            if (!ready && weapon.ultimateCharge >= 100) Debug.Log("Especial listo: Q o clic derecho (" + weapon.kind + ").", this);
            Changed?.Invoke();
        }
        public void EnemyKilled(float charge)
        {
            AddCharge(charge);
            if (player.State.HasPassive(PassiveKind.VampiricHeart)) player.State.Heal(player.State.MaxHealth * .04f);
        }
        public void BossHit()
        {
            AddCharge(2);
            if (++bossHits % 10 == 0 && player.State.HasPassive(PassiveKind.VampiricHeart)) player.State.Heal(player.State.MaxHealth * .04f);
        }
        public float ModifyIncoming(DamageInfo hit)
        {
            float damage = hit.amount * (player.State.HasPassive(PassiveKind.ThornArmor) ? .8f : 1);
            if (Time.time < shieldUntil && player.State.ActiveWeapon?.kind == WeaponKind.Shield && Vector3.Dot(transform.forward, -hit.direction.normalized) >= .5f) damage *= .5f;
            return damage;
        }
        public void AfterDamage(DamageInfo hit)
        {
            if (hit.melee && !hit.secondary && hit.source && player.State.HasPassive(PassiveKind.ThornArmor))
                hit.source.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo { amount = 35, source = gameObject, direction = -hit.direction, secondary = true, reflected = true });
        }
        public void RaiseShield() { shieldUntil = Time.time + .65f; }
        public float RollBasicDamage(WeaponInstance weapon)
        {
            bool critical = ShadowStep || (weapon.kind == WeaponKind.Dagger && UnityEngine.Random.value < .25f) || (weapon.kind == WeaponKind.Bow && UnityEngine.Random.value < .1f);
            return weapon.Damage * (critical ? 2 : 1);
        }
        public bool TryUltimate()
        {
            var weapon = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Time.timeScale == 0 || UltimateActive || weapon == null || weapon.ultimateCharge < 100) return false;
            weapon.ultimateCharge = 0; UltimateActive = true; Changed?.Invoke();
            StartCoroutine(Execute(weapon.kind, weapon.tier)); return true;
        }
        IEnumerator Execute(WeaponKind kind, int tier)
        {
            float strength = (tier - 1) / 3f;
            float damage = Mathf.Lerp(180, 450, strength);
            Vector3 forward = player.view ? player.view.transform.forward : transform.forward;
            Vector3 origin = transform.position + Vector3.up;
            if (kind == WeaponKind.Sword)
            {
                invulnerableUntil = Time.time + 2;
                for (int i = 0; i < 8; i++)
                { Area(transform.position + Vector3.up, 3.5f, Mathf.Lerp(18, 65, strength), false); yield return new WaitForSeconds(.25f); }
            }
            else if (kind == WeaponKind.Axe || kind == WeaponKind.Hammer)
            {
                var targets = CombatTargets.InRadius(origin, kind == WeaponKind.Hammer ? 7 : 9);
                foreach (var target in targets)
                {
                    if (kind == WeaponKind.Axe && Vector3.Dot(forward, (target.transform.position + Vector3.up - origin).normalized) < .5f) continue;
                    if (kind == WeaponKind.Hammer) CombatTargets.Pull(target, origin, 2);
                    CombatTargets.Stun(target, 3); CombatTargets.Hit(target, player, damage, true, kind == WeaponKind.Axe ? 5 : 0);
                }
            }
            else if (kind == WeaponKind.Dagger)
            { shadowUntil = Time.time + 3.5f; yield return new WaitForSeconds(3.5f); }
            else if (kind == WeaponKind.Shield)
            { invulnerableUntil = Time.time + 4; yield return new WaitForSeconds(4); Area(transform.position + Vector3.up, 6, damage, true, 8); }
            else if (kind == WeaponKind.Staff)
            {
                Vector3 center = origin + forward * 7;
                if (Physics.Raycast(origin, forward, out var hit, 7, ~0, QueryTriggerInteraction.Ignore)) center = hit.point - forward * .3f;
                for (int tick = 0; tick < 16; tick++)
                {
                    foreach (var target in CombatTargets.InRadius(center, 5))
                    { CombatTargets.Pull(target, center, .65f); CombatTargets.Hit(target, player, damage / 16, false, 0, tick > 0); }
                    yield return new WaitForSeconds(.25f);
                }
            }
            else if (kind == WeaponKind.Polearm || kind == WeaponKind.Musket || kind == WeaponKind.Bow || kind == WeaponKind.Scythe)
            {
                float range = kind == WeaponKind.Polearm ? 12 : kind == WeaponKind.Scythe ? 15 : 60;
                var targets = Line(origin, forward, range, kind == WeaponKind.Musket ? .25f : 1.2f);
                Vector3 explosion = origin + forward * range;
                if (kind == WeaponKind.Musket)
                {
                    var hits = Physics.RaycastAll(origin, forward, range, ~0, QueryTriggerInteraction.Ignore);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var hit in hits)
                        if (!hit.collider.GetComponentInParent<PlayerCharacter>()) { explosion = hit.point - forward * .1f; break; }
                }
                foreach (var target in targets)
                {
                    float before = HealthOf(target);
                    CombatTargets.Hit(target, player, kind == WeaponKind.Bow ? Mathf.Lerp(260, 600, strength) : damage, true, kind == WeaponKind.Scythe ? 3 : 0);
                    if (kind == WeaponKind.Scythe) player.State.Heal(Mathf.Max(0, before - HealthOf(target)) * .15f);
                    if (kind == WeaponKind.Musket && target && HealthOf(target) > 0) StartCoroutine(Bleed(target, damage * .2f));
                }
                if (kind == WeaponKind.Musket)
                    foreach (var target in CombatTargets.InRadius(explosion, 2.5f))
                        if (!targets.Contains(target)) CombatTargets.Hit(target, player, damage * .5f, true);
                if (kind == WeaponKind.Bow)
                    for (int i = 0; i < 4; i++)
                    {
                        foreach (var target in Line(origin, forward, range, 2)) CombatTargets.Hit(target, player, damage / 8, true, 0, true);
                        yield return new WaitForSeconds(.25f);
                    }
                if (kind == WeaponKind.Polearm) player.GetComponent<CharacterController>().Move(Vector3.ProjectOnPlane(forward, Vector3.up).normalized * 3);
            }
            UltimateActive = false; Changed?.Invoke();
        }
        IEnumerator Bleed(Component target, float total)
        {
            for (int i = 0; i < 3; i++)
            { yield return new WaitForSeconds(1); if (!target || !player.State.IsAlive) yield break; CombatTargets.Hit(target, player, total / 3, true, 0, true); }
        }
        static float HealthOf(Component target)
        {
            if (!target) return 0;
            if (target is Codice.Enemies.BasicEnemy enemy) return enemy.Health;
            if (target is Codice.Bosses.BossCharacter boss) return boss.Health;
            return 0;
        }
        void Area(Vector3 center, float range, float damage, bool guard, float push = 0)
        { foreach (var target in CombatTargets.InRadius(center, range)) CombatTargets.Hit(target, player, damage, guard, push); }
        List<Component> Line(Vector3 origin, Vector3 direction, float range, float width)
        {
            // Stop the corridor at solid world geometry, while allowing penetration through enemies.
            foreach (var hit in Physics.RaycastAll(origin, direction, range, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.GetComponentInParent<PlayerCharacter>() && !CombatTargets.IsEnemy(hit.collider.GetComponentInParent<IDamageReceiver>() as Component)) range = Mathf.Min(range, hit.distance);
            var result = new List<Component>();
            foreach (var target in CombatTargets.InRadius(origin, range))
            {
                Vector3 delta = target.GetComponent<Collider>().bounds.center - origin;
                float along = Vector3.Dot(delta, direction);
                if (along >= 0 && along <= range && (delta - direction * along).magnitude <= width) result.Add(target);
            }
            return result;
        }
    }
}
