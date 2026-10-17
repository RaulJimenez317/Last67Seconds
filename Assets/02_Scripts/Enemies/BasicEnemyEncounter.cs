using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace Codice.Enemies
{
    // One encounter only. Round progression and boss portals belong to the next phase.
    public sealed class BasicEnemyEncounter : MonoBehaviour
    {
        public BasicEnemy[] prefabs;
        public Transform[] spawnPoints;
        public PlayerCharacter player;
        public Merchant merchant;
        [Min(1)] public int level = 1;
        public EnemyKind[] composition = { EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman, EnemyKind.Crossbowman, EnemyKind.Swordsman };
        public int Defeated { get; private set; }
        public int Total => composition.Length;
        public event Action Completed;
        BasicEnemy[] spawned;
        int next;
        void Start()
        {
            spawned = new BasicEnemy[Total];
            if (merchant) merchant.onRoundContinued.AddListener(Continue);
            SpawnUntil(merchant ? (Total + 1) / 2 : Total);
        }
        void OnDestroy() { if (merchant) merchant.onRoundContinued.RemoveListener(Continue); }
        void SpawnUntil(int end)
        {
            while (next < end)
            {
                if (spawnPoints.Length == 0 || !player) { Debug.LogError("Enemy encounter needs player and spawn points.", this); return; }
                var marker = spawnPoints[next % spawnPoints.Length];
                if (!marker || !NavMesh.SamplePosition(marker.position, out var position, 2, NavMesh.AllAreas))
                { Debug.LogError("Enemy spawn is outside the navigation mesh.", this); return; }
                var enemy = Instantiate(prefabs[(int)composition[next]], position.position, marker.rotation, transform);
                enemy.Initialize(player, level); enemy.Died += OnDefeated; spawned[next++] = enemy;
            }
        }
        void OnDefeated(BasicEnemy enemy)
        {
            enemy.Died -= OnDefeated; Defeated++;
            if (merchant && Defeated == (Total + 1) / 2)
            {
                // Remove in-flight attacks before the safe shopping interval.
                foreach (var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (shot.gameObject.scene == gameObject.scene) Destroy(shot.gameObject);
                merchant.ReportRoundProgress(level, Defeated, Total);
            }
            if (Defeated == Total) Completed?.Invoke();
        }
        void Continue() { SpawnUntil(Total); }
    }
}
