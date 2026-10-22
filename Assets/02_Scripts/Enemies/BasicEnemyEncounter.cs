using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Enemies
{
    // Owns one configured wave. RunDirector decides when to start the next one.
    public sealed class BasicEnemyEncounter : MonoBehaviour
    {
        public BasicEnemy[] prefabs;
        public Transform[] spawnPoints;
        public PlayerCharacter player;
        public Merchant merchant;
        [Min(1)] public int level = 1;
        public bool startOnPlay = true;
        [Min(1)] public int maxConcurrent = 8;
        public EnemyKind[] composition = { EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman };
        public int Defeated { get; private set; }
        public int Total => composition.Length;
        public event Action Completed;
        BasicEnemy[] spawned;
        int next;
        int spawnLimit;
        bool running;
        void Start()
        {
            if (startOnPlay) BeginEncounter(level, composition);
        }
        public bool BeginEncounter(int round, EnemyKind[] roster)
        {
            if (running || !player || !player.State.IsAlive || roster == null || roster.Length == 0 || spawnPoints.Length == 0 || prefabs.Length != 4) return false;
            level = round; composition = (EnemyKind[])roster.Clone(); Defeated = next = 0;
            spawned = new BasicEnemy[Total]; running = true;
            if (merchant) { merchant.onRoundContinued.RemoveListener(Continue); merchant.onRoundContinued.AddListener(Continue); }
            spawnLimit = merchant ? (Total + 1) / 2 : Total;
            SpawnUntil(spawnLimit); return true;
        }
        void OnDestroy() { if (merchant) merchant.onRoundContinued.RemoveListener(Continue); }
        void SpawnUntil(int end)
        {
            while (next < end && next - Defeated < Mathf.Max(1, maxConcurrent))
            {
                if (spawnPoints.Length == 0 || !player) { Debug.LogError("Enemy encounter needs player and spawn points.", this); return; }
                var marker = spawnPoints[next % spawnPoints.Length];
                if (!marker || !NavMesh.SamplePosition(marker.position, out var position, 2, NavMesh.AllAreas))
                { Debug.LogError("Enemy spawn is outside the navigation mesh.", this); return; }
                var selected=prefabs[(int)composition[next]];
                if(level>=2&&next%3==1){var variant=Resources.Load<BasicEnemy>("EnemyVariants/"+composition[next]);if(variant)selected=variant;}
                string extra=ExtraEnemy(level,next,Total);
                if(extra!=null){var native=Resources.Load<BasicEnemy>("EnemyVariants/"+extra);if(native)selected=native;}
                var enemy = Instantiate(selected, position.position, marker.rotation, transform);
                enemy.Initialize(player, level); enemy.Died += OnDefeated; spawned[next++] = enemy;
            }
        }
        public static string ExtraEnemy(int round,int index,int total)
        {
            if(round%5==4&&index==total-1)return "DungeonSkeleton";
            if(round>=3&&index%4==2)return round>=6&&index%8==6?"TurtleShell":"Slime";
            return null;
        }
        public void ApplyLan(int total,int defeated){composition=new EnemyKind[Mathf.Clamp(total,0,200)];Defeated=defeated;}
        void OnDefeated(BasicEnemy enemy)
        {
            enemy.Died -= OnDefeated; Defeated++;
            if (merchant && Defeated == (Total + 1) / 2)
            {
                // Remove in-flight attacks before the safe shopping interval.
                foreach (var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (shot.gameObject.scene == gameObject.scene) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
                merchant.ReportRoundProgress(level, Defeated, Total);
            }
            else SpawnUntil(spawnLimit);
            if (Defeated == Total) { running = false; Completed?.Invoke(); }
        }
        void Continue() { spawnLimit = Total; SpawnUntil(spawnLimit); }
    }
}
