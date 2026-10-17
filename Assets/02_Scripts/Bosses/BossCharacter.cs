using System;
using System.Collections.Generic;
using Codice.Characters;
using Codice.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Bosses
{
    public enum BossKind { GoblinKing, DemonLord }
    public enum BossAttack { None, Rock, Mud, Sweep, Slam, Bow }

    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class BossCharacter : MonoBehaviour, IDamageReceiver
    {
        public BossKind kind;
        [Min(1)] public float baseHealth = 1200;
        [Min(0)] public int goldReward = 300;
        public Transform visual;
        public Material baseMaterial, evolvedMaterial, rockMaterial, mudMaterial, fireMaterial;
        public BasicEnemy[] summons;
        public GameObject meleeWeapon, bowWeapon;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive => Health > 0;
        public bool Evolved { get; private set; }
        public bool Paused { get; private set; }
        public int Level { get; private set; }
        public int Phase => kind == BossKind.GoblinKing ? 1 : Health > MaxHealth * .65f + .001f ? 1 : Health > MaxHealth * .30f + .001f ? 2 : 3;
        public float SpeedMultiplier => kind == BossKind.DemonLord ? 1 + .3f * (Phase - 1) : 1;
        public BossAttack CurrentAttack { get; private set; }
        public event Action<BossCharacter> Changed, Died, HitByPlayer;
        public PlayerCharacter Target { get; private set; }
        NavMeshAgent agent;
        float attackTimer = 2, windup, summonTimer, repath;
        int sequence, healthSummons;
        Vector3 aim, facing;
        Quaternion rest;
        Transform leftArm, rightArm;
        Quaternion leftRest, rightRest;
        readonly List<BasicEnemy> minions = new List<BasicEnemy>();
        readonly List<GameObject> hazards = new List<GameObject>();
        public int LivingSummons { get { minions.RemoveAll(e => !e || !e.IsAlive); return minions.Count; } }
        public float SummonInterval => kind == BossKind.GoblinKing ? 15 : Phase == 1 ? 20 : Phase == 2 ? 15 : 10;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (!visual) return;
            rest = visual.localRotation;
            foreach (var bone in visual.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "upperarm_l") { leftArm = bone; leftRest = bone.localRotation; }
                if (bone.name == "upperarm_r") { rightArm = bone; rightRest = bone.localRotation; }
            }
        }
        public void Initialize(PlayerCharacter player, int level)
        {
            Target = player; Level = Mathf.Max(1, level); Evolved = Level >= 15;
            MaxHealth = baseHealth * (Evolved ? 1.5f : 1); Health = MaxHealth;
            summonTimer = SummonInterval; attackTimer = 2; healthSummons = sequence = 0;
            CurrentAttack = BossAttack.None;
            if (visual)
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    if ((meleeWeapon && renderer.transform.IsChildOf(meleeWeapon.transform)) || (bowWeapon && renderer.transform.IsChildOf(bowWeapon.transform))) continue;
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Evolved ? evolvedMaterial : baseMaterial;
                    renderer.sharedMaterials = mats;
                }
            Changed?.Invoke(this);
        }
        public void SetPaused(bool paused)
        {
            Paused = paused;
            if (agent && agent.isOnNavMesh) agent.isStopped = paused;
            foreach (var minion in minions) if (minion) minion.SetPaused(paused);
        }
        void Update()
        {
            if (!IsAlive || Paused || Time.time < stunnedUntil || !Target || !Target.State.IsAlive)
            { if (agent.isOnNavMesh) agent.isStopped = true; return; }
            float dt = Time.deltaTime;
            summonTimer -= dt;
            if (summonTimer <= 0) { Summon(); summonTimer = SummonInterval; }
            if (!agent.isOnNavMesh) return;
            agent.speed = (kind == BossKind.GoblinKing ? 2.4f : 3.6f) * SpeedMultiplier;
            Vector3 delta = Target.transform.position - transform.position; delta.y = 0;
            if (CurrentAttack != BossAttack.None)
            {
                agent.isStopped = true; windup -= dt;
                if (visual) visual.localRotation = rest * Quaternion.Euler(-20 * Mathf.Clamp01(windup), 0, 0);
                if (windup <= 0)
                {
                    ExecuteAttack(); CurrentAttack = BossAttack.None;
                    if (visual) visual.localRotation = rest;
                    attackTimer = kind == BossKind.GoblinKing ? 2.2f : Phase == 3 ? .65f : 1.5f;
                }
                return;
            }
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 180 * dt);
            attackTimer -= dt; repath -= dt;
            float range = kind == BossKind.GoblinKing ? 22 : 3.8f;
            bool bow = kind == BossKind.DemonLord && Phase >= 2 && sequence % 3 == 2;
            if (bow) range = 24;
            if (attackTimer <= 0 && delta.magnitude <= range && HasLineOfSight(Target.transform.position + Vector3.up))
            {
                CurrentAttack = kind == BossKind.GoblinKing ? (sequence % 2 == 0 ? BossAttack.Rock : BossAttack.Mud) : bow ? BossAttack.Bow : sequence % 2 == 0 ? BossAttack.Sweep : BossAttack.Slam;
                if (bow) { retreatRemaining = .45f; retreatDirection = -delta.normalized; }
                sequence++; aim = Target.transform.position + (CurrentAttack == BossAttack.Mud ? Vector3.zero : Vector3.up);
                facing = transform.forward; windup = CurrentAttack == BossAttack.Slam ? 1.15f : .9f;
                if (bowWeapon) bowWeapon.SetActive(bow);
                if (meleeWeapon) meleeWeapon.SetActive(!bow);
                agent.isStopped = true; Changed?.Invoke(this); return;
            }
            agent.isStopped = delta.magnitude <= range * .8f && HasLineOfSight(Target.transform.position + Vector3.up);
            if (!agent.isStopped && repath <= 0) { repath = .3f; agent.SetDestination(Target.transform.position); }
        }
        float retreatRemaining;
        float stunnedUntil;
        public void ApplyStun(float seconds) { stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds); CurrentAttack = BossAttack.None; }
        Vector3 retreatDirection;
        void LateUpdate()
        {
            if (IsAlive && !Paused)
            {
                float lift = CurrentAttack == BossAttack.None ? 0 : Mathf.Sin(Mathf.Clamp01(windup) * Mathf.PI) * 40;
                if (leftArm) leftArm.localRotation = leftRest * Quaternion.Euler(0, 0, -35 + lift);
                if (rightArm) rightArm.localRotation = rightRest * Quaternion.Euler(0, 0, 35 - lift);
            }
            if (retreatRemaining <= 0 || Paused || Time.time < stunnedUntil || !IsAlive || !Target || !Target.State.IsAlive || !agent.isOnNavMesh) return;
            retreatRemaining -= Time.deltaTime;
            agent.Move(retreatDirection * (7 * Time.deltaTime));
            if (visual) { var p = visual.localPosition; p.y = Mathf.Sin(Mathf.Clamp01(retreatRemaining / .45f) * Mathf.PI) * .8f; visual.localPosition = p; }
        }
        public bool HasLineOfSight(Vector3 point)
        {
            Vector3 start = transform.position + Vector3.up * 1.6f, delta = point - start;
            foreach (var hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && (!Target || !hit.transform.IsChildOf(Target.transform))) return false;
            return true;
        }
        void ExecuteAttack()
        {
            if (CurrentAttack == BossAttack.Sweep || CurrentAttack == BossAttack.Slam)
            {
                Vector3 delta = Target.transform.position - transform.position; delta.y = 0;
                float dot = Vector3.Dot(facing, delta.normalized);
                if (delta.magnitude <= 4.2f && dot >= (CurrentAttack == BossAttack.Slam ? .8f : 0) && HasLineOfSight(Target.transform.position + Vector3.up))
                    Target.ReceiveDamage(new DamageInfo { amount = 45, source = gameObject, direction = facing, melee = true });
                if (Evolved) Fire(BossAttack.Slam, facing, 20, fireMaterial);
                return;
            }
            Vector3 origin = transform.position + Vector3.up * 1.7f + facing * 1.1f;
            Vector3 direction = (aim - origin).normalized;
            int count = CurrentAttack == BossAttack.Rock ? (Evolved ? 2 : 1) : CurrentAttack == BossAttack.Bow ? (Evolved ? 5 : Phase == 3 ? 3 : 1) : 1;
            for (int i = 0; i < count; i++)
                Fire(CurrentAttack, Quaternion.Euler(0, (i - (count - 1) * .5f) * 10, 0) * direction,
                    CurrentAttack == BossAttack.Rock ? 35 : CurrentAttack == BossAttack.Bow ? 40 : 0,
                    CurrentAttack == BossAttack.Rock ? rockMaterial : CurrentAttack == BossAttack.Mud ? mudMaterial : fireMaterial);
        }
        void Fire(BossAttack attack, Vector3 direction, float damage, Material material)
        {
            var obj = GameObject.CreatePrimitive(attack == BossAttack.Bow ? PrimitiveType.Cube : PrimitiveType.Sphere);
            obj.name = attack.ToString(); Destroy(obj.GetComponent<Collider>());
            obj.transform.position = transform.position + Vector3.up * (attack == BossAttack.Slam ? .6f : 1.7f) + facing * 1.1f;
            obj.transform.rotation = Quaternion.LookRotation(direction);
            obj.transform.localScale = attack == BossAttack.Bow ? new Vector3(.13f, .13f, .9f) : Vector3.one * .6f;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.AddComponent<BossProjectile>().Initialize(this, attack, direction, damage);
            Track(obj);
        }
        public void Track(GameObject obj) { hazards.RemoveAll(h => !h); hazards.Add(obj); }
        public void Summon()
        {
            if (!IsAlive || Paused || !Target || !Target.State.IsAlive || summons == null || summons.Length == 0) return;
            int count = kind == BossKind.GoblinKing ? UnityEngine.Random.Range(Evolved ? 3 : 2, Evolved ? 5 : 4) : UnityEngine.Random.Range(Phase == 1 ? 1 : 2, Phase == 1 ? 3 : 4) + (Evolved ? 2 : 0);
            for (int i = 0; i < count && LivingSummons < 12; i++)
            {
                var prefab = kind == BossKind.GoblinKing ? Array.Find(summons, e => e && e.kind == EnemyKind.Mage) : summons[UnityEngine.Random.Range(0, summons.Length)];
                if (!prefab) continue;
                Vector3 offset = Quaternion.Euler(0, (i + 1) * 360f / count, 0) * Vector3.forward * 4;
                if (!NavMesh.SamplePosition(transform.position + offset, out var hit, 3, agent.areaMask)) continue;
                var minion = Instantiate(prefab, hit.position, Quaternion.identity);
                minion.Initialize(Target, Evolved ? Mathf.Max(16, Level) : Level); minions.Add(minion);
            }
        }
        public void ReceiveDamage(DamageInfo hit)
        {
            if (!IsAlive || Paused || hit.amount <= 0 || float.IsNaN(hit.amount) || float.IsInfinity(hit.amount)) return;
            int previousPhase = Phase;
            Health = Mathf.Max(0, Health - hit.amount);
            if (hit.source && hit.source.GetComponentInParent<PlayerCharacter>())
            {
                HitByPlayer?.Invoke(this);
                if (!hit.secondary) hit.source.GetComponentInParent<PlayerCharacter>().GetComponent<Codice.Mechanics.PlayerMechanics>()?.BossHit();
            }
            Changed?.Invoke(this);
            if (!IsAlive)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
                foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
                Cleanup();
                var killer = hit.source ? hit.source.GetComponentInParent<PlayerCharacter>() : null;
                if (killer && killer.State.IsAlive) killer.State.AddGold(goldReward);
                Died?.Invoke(this); gameObject.SetActive(false); Destroy(gameObject); return;
            }
            if (Phase != previousPhase) summonTimer = Mathf.Min(summonTimer, SummonInterval);
            if (kind == BossKind.GoblinKing)
            {
                int thresholds = Mathf.Min(3, Mathf.FloorToInt((1 - Health / MaxHealth) * 4 + .0001f));
                while (healthSummons < thresholds) { healthSummons++; Summon(); }
            }
        }
        void Cleanup()
        {
            foreach (var minion in minions)
            {
                if (!minion) continue;
                foreach (var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (shot.Owner == minion) Destroy(shot.gameObject);
                minion.gameObject.SetActive(false); Destroy(minion.gameObject);
            }
            minions.Clear();
            foreach (var obj in hazards) if (obj) { obj.SetActive(false); Destroy(obj); }
            hazards.Clear();
        }
        void OnDisable() { Cleanup(); }
    }
}
