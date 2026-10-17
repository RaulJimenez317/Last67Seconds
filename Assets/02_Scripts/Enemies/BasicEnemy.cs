using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Enemies
{
    public enum EnemyKind { Swordsman, Crossbowman, Mage, Heavy }

    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class BasicEnemy : MonoBehaviour, IDamageReceiver
    {
        public EnemyKind kind;
        public Transform weaponPivot;
        public GameObject shield;
        public EnemyProjectile projectilePrefab;
        public Animator animator;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive => Health > 0;
        public bool GuardBroken { get; private set; }
        public bool Paused { get; private set; }
        public event Action<BasicEnemy> Died;
        public int GoldReward => new[] { 10, 15, 20, 35 }[(int)kind];
        public float AttackDamage => new[] { 12f, 18f, 22f, 32f }[(int)kind];
        public float UltimateChargeReward => kind == EnemyKind.Heavy ? 12 : 6;
        NavMeshAgent agent;
        PlayerCharacter target;
        float cooldown, windup, repath, stagger;
        bool attacking;
        Vector3 aim, impulse;
        Quaternion weaponRest;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (weaponPivot) weaponRest = weaponPivot.localRotation;
            Initialize(null, 1);
        }

        public void Initialize(PlayerCharacter player, int level = 1)
        {
            target = player;
            float scale = level >= 16 ? 1.5f : level >= 11 ? 1.3f : level >= 6 ? 1.15f : 1;
            MaxHealth = new[] { 65f, 55f, 80f, 220f }[(int)kind] * scale;
            Health = MaxHealth; GuardBroken = false;
            if (shield) shield.SetActive(true);
            if (!agent) agent = GetComponent<NavMeshAgent>();
            agent.speed = new[] { 4.2f, 3f, 2.5f, 1.8f }[(int)kind] * (level >= 16 ? 1.1f : 1);
            agent.stoppingDistance = IsRanged ? 8 : 1.45f;
        }

        bool IsRanged => kind == EnemyKind.Crossbowman || kind == EnemyKind.Mage;
        void Start() { if (!target) target = FindFirstObjectByType<PlayerCharacter>(); }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            if (agent && agent.isOnNavMesh)
            {
                agent.isStopped = paused;
                if (paused) agent.velocity = Vector3.zero;
            }
            if (animator) animator.speed = paused ? 0 : 1;
        }

        void Update()
        {
            if (!IsAlive || !agent.isOnNavMesh) return;
            if (animator) animator.SetFloat("Speed", agent.velocity.magnitude);
            if (Paused || !target || !target.State.IsAlive) { agent.isStopped = true; return; }
            float dt = Time.deltaTime;
            cooldown -= dt; stagger -= dt; repath -= dt;
            if (impulse.sqrMagnitude > .01f) { agent.Move(impulse * dt); impulse = Vector3.MoveTowards(impulse, Vector3.zero, 18 * dt); }
            if (stagger > 0) { agent.isStopped = true; return; }
            if (!attacking && weaponPivot) weaponPivot.localRotation = weaponRest;
            Vector3 offset = target.transform.position - transform.position;
            float distance = offset.magnitude;
            Vector3 facing = new Vector3(offset.x, 0, offset.z);
            if (facing.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 360 * dt);
            if (attacking)
            {
                agent.isStopped = true;
                windup -= dt;
                if (weaponPivot) weaponPivot.localRotation = weaponRest * Quaternion.Euler(-45 * Mathf.Clamp01(windup / .8f), 0, 0);
                if (windup <= 0)
                {
                    attacking = false;
                    if (weaponPivot) weaponPivot.localRotation = weaponRest;
                    Attack();
                    cooldown = kind == EnemyKind.Crossbowman ? 2 : kind == EnemyKind.Mage ? 2.4f : kind == EnemyKind.Heavy ? 1.8f : .7f;
                }
                return;
            }
            bool visible = CanSeeTarget();
            float range = IsRanged ? 16 : 2.1f;
            if (distance <= range && visible && cooldown <= 0)
            {
                attacking = true; windup = kind == EnemyKind.Crossbowman ? .8f : kind == EnemyKind.Heavy ? .9f : kind == EnemyKind.Mage ? 1 : .35f;
                aim = target.transform.position + Vector3.up;
                agent.isStopped = true;
                return;
            }
            agent.isStopped = visible && distance <= (IsRanged ? 10 : 1.5f);
            if (IsRanged && visible && distance < 5) agent.isStopped = false;
            if (!agent.isStopped && repath <= 0)
            {
                repath = .25f;
                Vector3 goal = IsRanged && visible && distance < 5 ? transform.position - facing.normalized * 3 : target.transform.position;
                if (NavMesh.SamplePosition(goal, out var hit, 2, agent.areaMask)) agent.SetDestination(hit.position);
            }
        }

        bool CanSeeTarget()
        {
            Vector3 start = transform.position + Vector3.up * 1.2f;
            Vector3 delta = target.transform.position + Vector3.up - start;
            foreach (var hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target.transform)) return false;
            return true;
        }

        void Attack()
        {
            if (!target || !target.State.IsAlive) return;
            if (!IsRanged)
            {
                if (Vector3.Distance(transform.position, target.transform.position) <= 2.2f && CanSeeTarget())
                    target.ReceiveDamage(new DamageInfo { amount = AttackDamage, source = gameObject, direction = transform.forward });
                return;
            }
            if (!projectilePrefab) return;
            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * .65f;
            Vector3 direction = (aim - origin).normalized;
            int count = kind == EnemyKind.Mage ? 3 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector3 shot = Quaternion.Euler(0, count == 1 ? 0 : (i - 1) * 12, 0) * direction;
                var projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(shot));
                projectile.Launch(this, target, shot, kind == EnemyKind.Mage ? 9 : 24, AttackDamage);
            }
        }

        public void ReceiveDamage(DamageInfo hit)
        {
            if (!IsAlive || Paused || hit.amount <= 0 || float.IsNaN(hit.amount) || float.IsInfinity(hit.amount)) return;
            Vector3 incoming = -hit.direction; incoming.y = 0;
            bool front = incoming.sqrMagnitude > .001f && Vector3.Dot(transform.forward, incoming.normalized) >= .5f;
            if (kind == EnemyKind.Heavy && hit.breaksGuard)
            { GuardBroken = true; if (shield) shield.SetActive(false); stagger = 1.2f; attacking = false; }
            float damage = kind == EnemyKind.Heavy && !GuardBroken && front ? hit.amount * .2f : hit.amount;
            Health = Mathf.Max(0, Health - damage);
            if (hit.knockback > 0)
            { impulse = Vector3.ProjectOnPlane(hit.direction, Vector3.up).normalized * Mathf.Min(hit.knockback, 12); stagger = Mathf.Max(stagger, .25f); attacking = false; }
            if (IsAlive) return;
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            var killer = hit.source ? hit.source.GetComponentInParent<PlayerCharacter>() : null;
            if (killer && killer.State.IsAlive) killer.State.AddGold(GoldReward);
            Died?.Invoke(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
