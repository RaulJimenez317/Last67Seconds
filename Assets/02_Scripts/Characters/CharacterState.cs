using System;
using UnityEngine;

namespace Codice.Characters
{
    public enum PlayerClass { Warrior, Tank, Archer, Mage, Shinobi }
    public enum WeaponKind { Sword, Axe, Bow, Dagger, Hammer, Polearm, Scythe, Shield, Staff, Musket }
    public enum PassiveKind { Frenzy, VampiricHeart, Overload, MercuryBoots, ThornArmor }

    [Serializable]
    public sealed class WeaponInstance
    {
        public WeaponKind kind;
        public bool katana, shuriken;
        [Range(1, 4)] public int tier = 1;
        public int ammunition;
        public int reserveAmmunition = 18;
        public float reloadProgress;
        [Range(0, 100)] public float ultimateCharge;
        public float Damage => shuriken?22:Mathf.Lerp(BaseDamage, MaxDamage, (tier - 1) / 3f);
        float BaseDamage => new[] { 18f, 55, 75, 12, 60, 35, 42, 25, 45, 95 }[(int)kind];
        float MaxDamage => new[] { 65f, 190, 250, 42, 195, 115, 135, 80, 145, 280 }[(int)kind];
        public float AttacksPerSecond => shuriken?3:Mathf.Lerp(new[] { 2.5f, .7f, .8f, 4, .65f, 1.3f, 1.1f, 1, 1.2f, .5f }[(int)kind], new[] { 3.4f, .85f, .95f, 4.5f, .85f, 1.6f, 1.4f, 1.2f, 1.5f, .5f }[(int)kind], (tier - 1) / 3f);
        public bool Ranged => kind == WeaponKind.Bow || kind == WeaponKind.Staff || kind == WeaponKind.Musket;
        public int Capacity => shuriken ? 30 : kind == WeaponKind.Bow ? Mathf.RoundToInt(Mathf.Lerp(24, 36, (tier - 1) / 3f)) : kind == WeaponKind.Musket ? 6 : 0;
        public WeaponInstance(WeaponKind kind, int tier = 1)
        { this.kind = kind; this.tier = Mathf.Clamp(tier, 1, 4); ammunition = Capacity; }
    }

    // Runtime data and transactions, independent of the future HUD and wave controller.
    [Serializable]
    public sealed class CharacterState
    {
        [SerializeField] PlayerClass playerClass;
        [SerializeField] float health;
        [SerializeField] float mana, manaDelay;
        [SerializeField] StaffSpell selectedSpell;
        public StaffSpell SelectedSpell => selectedSpell;
        public void SelectSpell(int index) { if(index<0||index>3)return; selectedSpell=(StaffSpell)index; Changed?.Invoke(); }
        public const float StaffManaCost=20, StaffUltimateManaCost=40, ManaRegen=8;
        public float MaxMana => playerClass==PlayerClass.Mage?100:60;
        public float Mana => mana;
        public bool HasStaff => Array.Exists(weapons,w=>w!=null&&w.kind==WeaponKind.Staff);
        public bool SpendMana(float amount)
        {
            if(!IsAlive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount)||mana<amount)return false;
            mana-=amount;manaDelay=1.5f;Changed?.Invoke();return true;
        }
        public void TickMana(float dt)
        {
            if(!IsAlive||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            float recharge=Mathf.Max(0,dt-manaDelay);manaDelay=Mathf.Max(0,manaDelay-dt);
            mana=Mathf.Min(MaxMana,mana+ManaRegen*recharge);
        }
        [SerializeField] int gold;
        [SerializeField] WeaponInstance[] weapons = new WeaponInstance[2];
        [SerializeField] int activeSlot;
        [SerializeField] PassiveKind[] passives = new PassiveKind[3];
        [SerializeField] int passiveCount;
        [SerializeField] float[] passiveRemaining = new float[3];
        [SerializeField] float[] passiveCooldown = new float[3];
        public const float PassiveDuration = 10, PassiveCooldown = 20;
        public float PassiveTime(int slot) => slot >= 0 && slot < passiveCount ? passiveRemaining[slot] : 0;
        public float PassiveWait(int slot) => slot >= 0 && slot < passiveCount ? passiveCooldown[slot] : 0;
        public bool ActivatePassive(int slot)
        {
            if (!IsAlive || slot < 0 || slot >= passiveCount || passiveCooldown[slot] > 0) return false;
            passiveRemaining[slot] = PassiveDuration; passiveCooldown[slot] = PassiveCooldown;
            Changed?.Invoke(); return true;
        }
        public void TickPassives(float dt)
        {
            if (!IsAlive || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            for (int i = 0; i < passiveCount; i++)
            { passiveRemaining[i] = Mathf.Max(0, passiveRemaining[i] - dt); passiveCooldown[i] = Mathf.Max(0, passiveCooldown[i] - dt); }
        }
        public event Action Changed;
        public event Action Died;
        public PlayerClass Class => playerClass;
        public float MaxHealth => playerClass == PlayerClass.Tank ? 180 : playerClass == PlayerClass.Archer || playerClass == PlayerClass.Mage ? 100 : 120;
        public float MoveSpeed => (playerClass == PlayerClass.Tank ? 4.8f : playerClass == PlayerClass.Archer ? 8f : 6.5f) * (HasPassive(PassiveKind.MercuryBoots) ? 1.2f : 1);
        public bool DualWield => ActiveWeapon!=null&&!ActiveWeapon.katana&&(ActiveWeapon.kind==WeaponKind.Sword||ActiveWeapon.kind==WeaponKind.Dagger)&&weapons[1-activeSlot]!=null&&weapons[1-activeSlot].kind==ActiveWeapon.kind&&!weapons[1-activeSlot].katana;
        public float AttackSpeedMultiplier => (HasPassive(PassiveKind.Frenzy)?1.25f:1)*(DualWield?1.3f:1);
        public bool HasPassive(PassiveKind passive)
        { for (int i = 0; i < passiveCount; i++) if (passives[i] == passive && passiveRemaining[i] > 0) return true; return false; }
        public void NetworkRevive(){health=MaxHealth;mana=MaxMana;Changed?.Invoke();}
        public string Capture() => JsonUtility.ToJson(this);
        public void Restore(string snapshot) { JsonUtility.FromJsonOverwrite(snapshot, this); Changed?.Invoke(); }
        public void NotifyChanged() => Changed?.Invoke();
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
            selectedSpell=StaffSpell.Arcane; playerClass = selected; health = MaxHealth; mana=MaxMana; manaDelay=0; gold = 0; activeSlot = 0; passiveCount = 0;
            passiveRemaining = new float[3]; passiveCooldown = new float[3];
            weapons = new WeaponInstance[2];
            weapons[0] = new WeaponInstance(selected == PlayerClass.Mage ? WeaponKind.Staff : selected == PlayerClass.Tank ? WeaponKind.Hammer : selected == PlayerClass.Archer ? WeaponKind.Bow : WeaponKind.Sword);
            if(selected==PlayerClass.Shinobi){weapons[0].katana=true;weapons[1]=new WeaponInstance(WeaponKind.Bow){shuriken=true,ammunition=30};}
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
            int slot = passiveCount < 3 ? passiveCount++ : replaceSlot;
            passives[slot] = passive; passiveRemaining[slot] = passiveCooldown[slot] = 0;
            Changed?.Invoke(); return true;
        }
        public bool ConsumeAmmo()
        {
            var weapon = ActiveWeapon;
            if (weapon == null || (weapon.Capacity > 0 && weapon.ammunition <= 0)) return false;
            if(weapon.kind==WeaponKind.Staff&&!SpendMana(StaffManaCost))return false;
            if (weapon.Capacity > 0) weapon.ammunition--;
            Changed?.Invoke(); return true;
        }
        public bool AddAmmunition(int amount, int ammoType = 0)
        {
            var weapon = ActiveWeapon;
            if(ammoType!=0)weapon=Array.Find(weapons,w=>w!=null&&w.kind==WeaponKind.Bow&&w.shuriken==(ammoType==2)&&w.ammunition<w.Capacity);
            if (!IsAlive || amount <= 0 || weapon == null || weapon.Capacity == 0) return false;
            if (weapon.kind == WeaponKind.Musket)
            {
                if (weapon.reserveAmmunition >= 60) return false;
                weapon.reserveAmmunition += Mathf.Min(amount, 60 - weapon.reserveAmmunition);
                Changed?.Invoke(); return true;
            }
            if (weapon.ammunition >= weapon.Capacity) return false;
            weapon.ammunition += Mathf.Min(amount, weapon.Capacity - weapon.ammunition);
            Changed?.Invoke(); return true;
        }
        // Arrows and shuriken are replenished by world pickups, never by waiting.
        public void TickAmmunition(float deltaTime) { }
    }
}
