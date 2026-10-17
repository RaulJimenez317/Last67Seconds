using System;
using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Codice.Bosses
{
    public sealed class BossEncounter : MonoBehaviour
    {
        public BossCharacter goblinPrefab, demonPrefab;
        public PlayerCharacter player;
        public Transform spawnPoint;
        [Min(5)] public int level = 5;
        public bool startOnPlay = true;
        public bool managedByRun;
        public UnityEvent onCompleted = new UnityEvent();
        public event Action<BossCharacter> Spawned;
        public BossCharacter ActiveBoss { get; private set; }
        public bool Completed { get; private set; }
        public bool Defeated => player && !player.State.IsAlive;
        public bool Restarting { get; private set; }
        Vector3 playerStart;
        Quaternion playerRotation;
        void Awake()
        {
            if (player) { playerStart = player.transform.position; playerRotation = player.transform.rotation; }
        }
        void Update()
        {
            if (!Defeated) return;
            if (ActiveBoss && !ActiveBoss.Paused) ActiveBoss.SetPaused(true);
            if (!managedByRun && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartEncounter();
        }
        public bool RestartEncounter()
        {
            if (managedByRun) return Codice.Mechanics.RunDirector.Instance && Codice.Mechanics.RunDirector.Instance.RestartRun();
            if (!Defeated || Restarting) return false;
            Restarting = true;
            StartCoroutine(RestartAttempt());
            return true;
        }
        System.Collections.IEnumerator RestartAttempt()
        {
            if (ActiveBoss)
            {
                ActiveBoss.Died -= OnBossDied;
                ActiveBoss.gameObject.SetActive(false); // Immediately removes owned hazards and summons.
                Destroy(ActiveBoss.gameObject); ActiveBoss = null;
            }
            yield return null; // Let deferred destruction finish before reviving the player.
            player.RestartAfterDefeat(playerStart, playerRotation);
            Completed = false;
            if (!BeginEncounter(level)) Debug.LogError("Could not restart boss encounter.", this);
            Restarting = false;
        }
        void OnGUI()
        {
            if (!player || Codice.UI.RunHUD.Instance) return;
            GUI.Box(new Rect(20, 20, 250, 55), $"Vida: {Mathf.CeilToInt(player.State.Health)} / {player.State.MaxHealth}\nColiseo — Nivel {level}");
            if (ActiveBoss)
                GUI.Box(new Rect(Screen.width / 2f - 180, 20, 360, 55), $"{(ActiveBoss.kind == BossKind.GoblinKing ? "Rey Goblin" : "Señor Demonio")} — Fase {ActiveBoss.Phase}\n{Mathf.CeilToInt(ActiveBoss.Health)} / {ActiveBoss.MaxHealth} HP");
            if (Defeated || Restarting)
            {
                var panel = new Rect(Screen.width / 2f - 210, Screen.height / 2f - 90, 420, 180);
                GUI.Box(panel, "Has sido derrotado");
                GUI.Label(new Rect(panel.x + 25, panel.y + 40, 380, 45), managedByRun ? "Tu vida llegó a cero.\nComienza una nueva partida desde el Calabozo." : "Tu vida llegó a cero.\nReintenta este nivel con tu equipo inicial.");
                GUI.enabled = !Restarting;
                if (GUI.Button(new Rect(panel.x + 60, panel.y + 105, 300, 45), managedByRun ? "Nueva partida (R)" : "Reintentar combate (R)")) RestartEncounter();
                GUI.enabled = true;
            }
            else if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Box(new Rect(Screen.width / 2f - 200, Screen.height - 65, 400, 40), "Haz clic en el juego para recuperar el control del ratón.");
        }
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
