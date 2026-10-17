using System;
using UnityEngine;

namespace Codice.Characters
{
    public enum PlayerClass { Warrior, Tank, Archer }
    public enum WeaponKind { Sword, Axe, Bow, Dagger, Hammer, Polearm, Scythe, Shield, Staff, Musket }
    public enum PassiveKind { Frenzy, VampiricHeart, Overload, MercuryBoots, ThornArmor }

    [Serializable]
    public sealed class WeaponInstance
    {
        public WeaponKind kind;
        [Range(1, 4)] public int tier = 1;
        public int ammunition;
        public float reloadProgress;
        public float Damage => Mathf.Lerp(BaseDamage, MaxDamage, (tier - 1) / 3f);
        float BaseDamage => new[] { 18f, 55, 75, 12, 60, 35, 42, 25, 45, 95 }[(int)kind];
        float MaxDamage => new[] { 65f, 190, 250, 42, 195, 115, 135, 80, 145, 280 }[(int)kind];
        public float AttacksPerSecond => new[] { 2.5f, .7f, .8f, 4, .65f, 1.3f, 1.1f, 1, 1.2f, .5f }[(int)kind];
        public bool Ranged => kind == WeaponKind.Bow || kind == WeaponKind.Staff || kind == WeaponKind.Musket;
        public int Capacity => kind == WeaponKind.Bow ? 8 : kind == WeaponKind.Musket ? 6 : 0;
        public WeaponInstance(WeaponKind kind, int tier = 1)
        { this.kind = kind; this.tier = Mathf.Clamp(tier, 1, 4); ammunition = Capacity; }
    }

    // Runtime data and transactions, independent of the future HUD and wave controller.
    [Serializable]
    public sealed class CharacterState
    {
        [SerializeField] PlayerClass playerClass;
        [SerializeField] float health;
        [SerializeField] int gold;
        [SerializeField] WeaponInstance[] weapons = new WeaponInstance[2];
        [SerializeField] int activeSlot;
        [SerializeField] PassiveKind[] passives = new PassiveKind[3];
        [SerializeField] int passiveCount;
        public event Action Changed;
        public event Action Died;
        public PlayerClass Class => playerClass;
        public float MaxHealth => playerClass == PlayerClass.Tank ? 180 : playerClass == PlayerClass.Archer ? 100 : 120;
        public float MoveSpeed => playerClass == PlayerClass.Tank ? 4.8f : playerClass == PlayerClass.Archer ? 6.2f : 6.5f;
        public float Health => health;
        public int Gold => gold;
        public bool IsAlive => health > 0;
        public int ActiveSlot => activeSlot;
        public int PassiveCount => passiveCount;
        public WeaponInstance ActiveWeapon => weapons[activeSlot];
        public WeaponInstance GetWeapon(int slot) => slot >= 0 && slot < 2 ? weapons[slot] : null;
        public PassiveKind GetPassive(int slot) => passives[slot];

        public void Initialize(PlayerClass selected)
        {
            playerClass = selected; health = MaxHealth; gold = 0; activeSlot = 0; passiveCount = 0;
            weapons = new WeaponInstance[2];
            weapons[0] = new WeaponInstance(selected == PlayerClass.Tank ? WeaponKind.Axe : selected == PlayerClass.Archer ? WeaponKind.Bow : WeaponKind.Sword);
            Changed?.Invoke();
        }
        public bool Damage(float amount)
        {
            if (!IsAlive || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return false;
            health = Mathf.Max(0, health - amount); Changed?.Invoke();
            if (!IsAlive) Died?.Invoke();
            return true;
        }
        public bool Heal(float amount)
        {
            if (!IsAlive || health >= MaxHealth || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            health = Mathf.Min(MaxHealth, health + amount); Changed?.Invoke(); return true;
        }
        public bool AddGold(int amount)
        {
            if (amount <= 0 || gold > int.MaxValue - amount) return false;
            gold += amount; Changed?.Invoke(); return true;
        }
        public bool SpendGold(int amount)
        {
            if (amount < 0 || amount > gold || !IsAlive) return false;
            gold -= amount; Changed?.Invoke(); return true;
        }
        public bool SelectWeapon(int slot)
        {
            if (slot < 0 || slot > 1 || weapons[slot] == null) return false;
            activeSlot = slot; Changed?.Invoke(); return true;
        }
        public WeaponInstance Equip(WeaponInstance weapon)
        {
            if (weapon == null) return null;
            int slot = weapons[0] == null ? 0 : weapons[1] == null ? 1 : activeSlot;
            var previous = weapons[slot]; weapons[slot] = weapon; activeSlot = slot;
            Changed?.Invoke(); return previous;
        }
        public bool CanEquipPassive(PassiveKind passive, int replaceSlot)
        {
            for (int i = 0; i < passiveCount; i++) if (passives[i] == passive) return false;
            return passiveCount < 3 || (replaceSlot >= 0 && replaceSlot < 3);
        }
        public bool EquipPassive(PassiveKind passive, int replaceSlot = -1)
        {
            if (!CanEquipPassive(passive, replaceSlot)) return false;
            if (passiveCount < 3) passives[passiveCount++] = passive; else passives[replaceSlot] = passive;
            Changed?.Invoke(); return true;
        }
        public bool ConsumeAmmo()
        {
            var weapon = ActiveWeapon;
            if (weapon == null || (weapon.Capacity > 0 && weapon.ammunition <= 0)) return false;
            if (weapon.Capacity > 0) weapon.ammunition--;
            Changed?.Invoke(); return true;
        }
        public bool AddAmmunition(int amount)
        {
            var weapon = ActiveWeapon;
            if (!IsAlive || amount <= 0 || weapon == null || weapon.Capacity == 0 || weapon.ammunition >= weapon.Capacity) return false;
            weapon.ammunition += Mathf.Min(amount, weapon.Capacity - weapon.ammunition);
            Changed?.Invoke(); return true;
        }
        public void TickAmmunition(float deltaTime)
        {
            if (!IsAlive || deltaTime <= 0) return;
            foreach (var weapon in weapons)
            {
                if (weapon == null || weapon.kind != WeaponKind.Bow) continue;
                if (weapon.ammunition >= weapon.Capacity) { weapon.reloadProgress = 0; continue; }
                weapon.reloadProgress += deltaTime;
                while (weapon.reloadProgress >= 1.5f && weapon.ammunition < weapon.Capacity)
                { weapon.reloadProgress -= 1.5f; weapon.ammunition++; Changed?.Invoke(); }
            }
        }
    }
}
