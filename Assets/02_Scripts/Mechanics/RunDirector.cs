using System;
using System.Collections;
using Codice.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Codice.Mechanics
{
    public enum RunPhase { Combat, Shop, Cleared, Travelling, Defeated }
    public sealed class RunDirector : MonoBehaviour
    {
        public static RunDirector Instance { get; private set; }
        public int Level { get; private set; }
        public float Elapsed { get; private set; }
        public float RoundElapsed { get; private set; }
        public RunPhase Phase { get; private set; }
        public bool CombatActive => Phase == RunPhase.Combat;
        public PlayerCharacter Player => scene ? scene.player : null;
        public event Action Changed;
        RunScene scene;
        string snapshot;
        bool transition;
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void Enter(RunScene context)
        {
            scene = context;
            if (Level == 0) Level = context.IsArena ? Mathf.Max(5, context.arena.level) : 1;
            if (!string.IsNullOrEmpty(snapshot)) { context.player.RestoreRunState(snapshot); snapshot = null; }
            context.player.State.Died += OnDeath;
            if (context.dungeon)
            {
                context.dungeon.Completed += RoundCleared;
                context.dungeon.merchant.onIntermissionStarted.AddListener(OpenShop);
                context.dungeon.merchant.onRoundContinued.AddListener(CloseShop);
            }
            if (context.arena) context.arena.onCompleted.AddListener(RoundCleared);
            transition = false; BeginRound();
        }
        void Update()
        {
            if (!scene || transition) return;
            if (Player && Player.State.IsAlive && Phase != RunPhase.Defeated)
            {
                Elapsed += Time.deltaTime;
                if (CombatActive) RoundElapsed += Time.deltaTime;
            }
            if (Phase == RunPhase.Defeated && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartRun();
        }
        void BeginRound()
        {
            scene.portal.SetAvailable(false);
            RoundElapsed = 0; SetPhase(RunPhase.Combat);
            bool started = scene.IsArena ? scene.arena.BeginEncounter(Level) : scene.dungeon.BeginEncounter(Level, RoundRules.Roster(Level));
            if (!started) { SetPhase(RunPhase.Cleared); Debug.LogError("No se pudo iniciar la ronda " + Level, scene); return; }
            if (!scene.IsArena) SpawnLoot();
            Debug.Log("Ronda " + Level + " iniciada.", scene);
        }
        void OpenShop() { SetPhase(RunPhase.Shop); Debug.Log("Interludio: mercader en el centro. E para comprar; Enter en la tienda para continuar."); }
        void CloseShop() { if (Phase == RunPhase.Shop) SetPhase(RunPhase.Combat); }
        void RoundCleared()
        {
            if (Phase != RunPhase.Combat || !Player || !Player.State.IsAlive) return;
            if (RoundElapsed < 180) { Player.State.AddGold(100); Debug.Log("Bono de rapidez: +100 oro."); }
            SetPhase(RunPhase.Cleared);
            if (scene.IsArena || RoundRules.IsBoss(Level + 1))
            { scene.portal.SetAvailable(true); Debug.Log("Portal disponible: acércate y pulsa E para continuar."); }
            else StartCoroutine(NextDungeonRound());
        }
        IEnumerator NextDungeonRound()
        {
            yield return new WaitForSeconds(3);
            if (Phase != RunPhase.Cleared || !Player || !Player.State.IsAlive) yield break;
            Level++; BeginRound();
        }
        public bool Travel()
        {
            if (transition || Phase != RunPhase.Cleared || !scene || !Player || !Player.State.IsAlive) return false;
            string destination = RoundRules.IsBoss(Level + 1) ? "Coliseo" : "Calabozo";
            if (!Application.CanStreamedLevelBeLoaded(destination)) { Debug.LogError("Falta escena en Build Settings: " + destination); return false; }
            snapshot = Player.State.Capture(); Level++; transition = true; SetPhase(RunPhase.Travelling);
            SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
            return true;
        }
        void OnDeath()
        {
            StopAllCoroutines(); SetPhase(RunPhase.Defeated);
            Player.GetComponent<PlayerMechanics>()?.Cancel();
            if (scene.arena && scene.arena.ActiveBoss) scene.arena.ActiveBoss.SetPaused(true);
            foreach (var enemy in FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
            Debug.Log("Partida terminada. Pulsa R para comenzar de nuevo desde el Calabozo.");
        }
        public bool RestartRun()
        {
            if (Phase != RunPhase.Defeated || transition) return false;
            transition = true; snapshot = null; Level = 1; Elapsed = RoundElapsed = 0;
            SceneManager.LoadSceneAsync("Calabozo", LoadSceneMode.Single); return true;
        }
        void SetPhase(RunPhase phase) { Phase = phase; Changed?.Invoke(); }
        void SpawnLoot()
        {
            // Clean previous round loot; keep quantities bounded across an endless run.
            foreach (var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None)) Destroy(pickup.gameObject);
            foreach (var pickup in FindObjectsByType<LootPickup>(FindObjectsSortMode.None)) if (!pickup.respawns) Destroy(pickup.gameObject);
            for (int i = 0; i < scene.lootMarkers.Length; i++)
            {
                if (i % 2 == 0) Player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)UnityEngine.Random.Range(0, 10), Mathf.Clamp(2 + (Level - 1) / 5, 2, 4)), scene.lootMarkers[i].position);
                else LootPickup.Create(scene.lootMarkers[i].position, LootKind.Passive, 1);
            }
        }
    }
}
