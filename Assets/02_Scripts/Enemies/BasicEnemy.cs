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
        public bool IsStunned => stagger > 0;
        public bool GuardBroken { get; private set; }
        public bool Paused { get; private set; }
        public event Action<BasicEnemy> Died;
        public int GoldReward => new[] { 40, 50, 60, 80 }[(int)kind];
        public float AttackDamage => new[] { 12f, 18f, 22f, 32f }[(int)kind];
        public float UltimateChargeReward => kind == EnemyKind.Heavy ? 12 : 6;
        NavMeshAgent agent;
        PlayerCharacter target;
        float cooldown, windup, repath, stagger;
        bool attacking;
        Vector3 patrolOrigin;float patrolTimer;
        public bool IsPatrolling {get;private set;}
        Vector3 aim, impulse;
        Quaternion weaponRest;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (weaponPivot) weaponRest = weaponPivot.localRotation;
            Initialize(null, 1);
            patrolOrigin=transform.position;
            Codice.Presentation.ActorPresentation.Install(gameObject);
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
        public void ApplyStun(float seconds) { stagger = Mathf.Max(stagger, seconds); attacking = false; }
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
            if (Paused || (target && !target.State.IsAlive)) { agent.isStopped = true; return; }
            IsPatrolling=!target;
            if(IsPatrolling){Patrol();return;}
            float dt = Time.deltaTime;
            cooldown -= dt; stagger -= dt; repath -= dt;
            if (impulse.sqrMagnitude > .01f) { agent.Move(impulse * dt); impulse = Vector3.MoveTowards(impulse, Vector3.zero, 18 * dt); }
            if (stagger > 0) { agent.isStopped = true; return; }
            if (!GetComponent<Codice.Presentation.ActorPresentation>() && !attacking && weaponPivot) weaponPivot.localRotation = weaponRest;
            Vector3 offset = target.transform.position - transform.position;
            float distance = offset.magnitude;
            Vector3 facing = new Vector3(offset.x, 0, offset.z);
            if (facing.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 360 * dt);
            if (attacking)
            {
                agent.isStopped = true;
                windup -= dt;
                if (!GetComponent<Codice.Presentation.ActorPresentation>() && weaponPivot) weaponPivot.localRotation = weaponRest * Quaternion.Euler(-45 * Mathf.Clamp01(windup / .8f), 0, 0);
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
                GetComponent<Codice.Presentation.ActorPresentation>()?.Attack(IsRanged,windup);
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

        void Patrol()
        {
            patrolTimer-=Time.deltaTime;if(patrolTimer>0)return;patrolTimer=3;
            Vector3 goal=patrolOrigin+new Vector3(Mathf.Sin(Time.time)*3,0,Mathf.Cos(Time.time)*3);
            if(NavMesh.SamplePosition(goal,out var hit,2,agent.areaMask)){agent.isStopped=false;agent.SetDestination(hit.position);}
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
                    target.ReceiveDamage(new DamageInfo { amount = AttackDamage, source = gameObject, direction = transform.forward, melee = true });
                return;
            }
            if (!projectilePrefab) return;
            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * .65f;
            Vector3 direction = (aim - origin).normalized;
            Codice.Presentation.PresentationVfx.Muzzle(weaponPivot?weaponPivot.position:origin,direction);
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
            float damage = kind == EnemyKind.Heavy && !GuardBroken && front && !hit.ignoreGuard ? hit.amount * .2f : hit.amount;
            float actual = Mathf.Min(Health, damage);
            Health = Mathf.Max(0, Health - damage);
            CombatFeedback.Report(this, hit, actual, damage < hit.amount);
            if (hit.knockback > 0)
            { impulse = Vector3.ProjectOnPlane(hit.direction, Vector3.up).normalized * Mathf.Min(hit.knockback, 12); stagger = Mathf.Max(stagger, .25f); attacking = false; }
            if (IsAlive) return;
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            var killer = hit.source ? hit.source.GetComponentInParent<PlayerCharacter>() : null;
            if (killer && killer.State.IsAlive)
            {
                Codice.Mechanics.GoldPickup.Drop(transform.position, GoldReward, killer);
                killer.GetComponent<Codice.Mechanics.PlayerMechanics>()?.EnemyKilled(UltimateChargeReward);
                Codice.Mechanics.LootPickup.DropFromEnemy(killer, transform.position);
            }
            GetComponent<Codice.Presentation.ActorPresentation>()?.Die();
            Died?.Invoke(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
