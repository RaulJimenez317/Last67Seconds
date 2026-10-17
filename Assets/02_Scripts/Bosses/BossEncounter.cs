using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Codice.Bosses
{
    public sealed class BossEncounter : MonoBehaviour
    {
        public BossCharacter goblinPrefab, demonPrefab;
        public PlayerCharacter player;
        public Transform spawnPoint;
        [Min(5)] public int level = 5;
        public bool startOnPlay = true;
        public UnityEvent onCompleted = new UnityEvent();
        public event Action<BossCharacter> Spawned;
        public BossCharacter ActiveBoss { get; private set; }
        public bool Completed { get; private set; }
        public static BossKind KindForLevel(int round) => (round / 5) % 2 == 1 ? BossKind.GoblinKing : BossKind.DemonLord;
        System.Collections.IEnumerator Start()
        {
            // NavMeshSurface registers its saved data during scene initialization.
            yield return null;
            if (startOnPlay && !BeginEncounter(level)) Debug.LogError("Boss encounter could not start: check player, prefabs, level and navigation.", this);
        }
        public bool BeginEncounter(int round)
        {
            if (round < 5 || round % 5 != 0 || ActiveBoss || !player || !player.State.IsAlive || !spawnPoint) return false;
            var prefab = KindForLevel(round) == BossKind.GoblinKing ? goblinPrefab : demonPrefab;
            if (!prefab || !NavMesh.SamplePosition(spawnPoint.position, out var hit, 4, NavMesh.AllAreas)) return false;
            level = round; Completed = false;
            ActiveBoss = Instantiate(prefab, hit.position, spawnPoint.rotation);
            ActiveBoss.Initialize(player, round); ActiveBoss.Died += OnBossDied;
            Spawned?.Invoke(ActiveBoss); return true;
        }
        void OnBossDied(BossCharacter boss) { boss.Died -= OnBossDied; ActiveBoss = null; Completed = true; onCompleted.Invoke(); }
        void OnDisable() { if (ActiveBoss) { ActiveBoss.Died -= OnBossDied; Destroy(ActiveBoss.gameObject); ActiveBoss = null; } }
    }
}
