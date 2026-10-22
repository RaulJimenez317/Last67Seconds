using System;
using System.Collections;
using Codice.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Codice.Mechanics
{
    public enum RunPhase { Combat, Shop, Cleared, Travelling, Defeated, Victory }
    public sealed class RunDirector : MonoBehaviour
    {
        public static RunDirector Instance { get; private set; }
        public int Level { get; private set; }
        public float Elapsed { get; private set; }
        public float RoundElapsed { get; private set; }
        public RunPhase Phase { get; private set; }
        public bool CombatActive => Phase == RunPhase.Combat;
        public int TotalKills { get; private set; }
        public float ShopRemaining { get; private set; }
        public PlayerCharacter Player => scene ? scene.player : null;
        public event Action Changed;
        public event Action<string> Notice;
        RunScene scene;
        string snapshot,checkpoint,checkpointScene;int checkpointLevel;float checkpointElapsed;
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
            if (Level == 0) Level = Mathf.Clamp(PlayerPrefs.GetInt("Codice.TestRound", 1), 1, RoundRules.FinalRound);
            PlayerPrefs.DeleteKey("Codice.TestRound");
            if (!string.IsNullOrEmpty(snapshot)) { context.player.RestoreRunState(snapshot); snapshot = null; }
            context.player.State.Died += OnDeath;
            if (context.dungeon)
            {
                context.dungeon.Completed += RoundCleared;
                if(context.dungeon.merchant){context.dungeon.merchant.onIntermissionStarted.AddListener(OpenShop);context.dungeon.merchant.onRoundContinued.AddListener(CloseShop);}
            }
            if (context.arena) context.arena.onCompleted.AddListener(RoundCleared);
            transition = false; BeginRound();
        }
        void Update()
        {
            if (Codice.Multiplayer.LanSession.Client || !scene || transition) return;
            if (Phase == RunPhase.Shop && scene.dungeon && scene.dungeon.merchant)
            {
                var merchant = scene.dungeon.merchant;
                if (!merchant.Customer) ShopRemaining = Mathf.Max(0, ShopRemaining - Time.deltaTime);
                if (ShopRemaining <= 0 || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)) merchant.ContinueRound();
            }
            if (Player && (Player.State.IsAlive||Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanSession.Instance.AnyAlive) && Phase != RunPhase.Defeated && Phase != RunPhase.Victory)
            {
                Elapsed += Time.deltaTime;
                if (CombatActive) RoundElapsed += Time.deltaTime;
            }
            if (Phase == RunPhase.Defeated && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartRun();
        }
        void BeginRound()
        {
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            checkpoint=Player.State.Capture();checkpointScene=SceneManager.GetActiveScene().name;checkpointLevel=Level;checkpointElapsed=Elapsed;
            if(scene.portal)scene.portal.SetAvailable(false);
            RoundElapsed = 0; SetPhase(RunPhase.Combat);
            bool boss = scene.UsesBossEncounter(Level);
            bool started = boss ? scene.arena.BeginEncounter(Level) : scene.UsesDungeonEncounter(Level) && scene.dungeon.BeginEncounter(Level, RoundRules.Roster(Level));
            if (!started) { SetPhase(RunPhase.Cleared); Debug.LogError("No se pudo iniciar la ronda " + Level, scene); return; }
            if (!boss) SpawnLoot();
            Notice?.Invoke("¡Ronda " + Level + " iniciada!");
            Debug.Log("Ronda " + Level + " iniciada.", scene);
        }
        void OpenShop() { ShopRemaining=25; SetPhase(RunPhase.Shop); Notice?.Invoke("Mercader disponible • [E] Comprar • [Enter] Continuar ronda"); }
        void CloseShop() { if (Phase == RunPhase.Shop) SetPhase(RunPhase.Combat); }
        void RoundCleared()
        {
            if (Phase != RunPhase.Combat || !Player || (!Player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host)) return;
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ReviveTeam();
            GoldPickup.CollectRemaining(Player);
            if (RoundElapsed < 180) { Player.State.AddGold(100); Notice?.Invoke("¡Bono de Rapidez +100 Oro!"); Debug.Log("Bono de rapidez: +100 oro."); }
            SetPhase(RunPhase.Cleared);
            if (RoundRules.IsFinal(Level))
            { SetPhase(RunPhase.Victory); Notice?.Invoke("¡El Ork Berserker ha caído! El Códice de Ceniza ha sido conquistado."); return; }
            if (RoundRules.OpensPortalAfter(Level))
            { if(scene.portal)scene.portal.SetAvailable(true); Notice?.Invoke("¡Portal abierto! Pulsa E para continuar el viaje."); Debug.Log("Portal disponible: acércate y pulsa E para continuar."); }
            else StartCoroutine(NextDungeonRound());
        }
        IEnumerator NextDungeonRound()
        {
            yield return new WaitForSeconds(3);
            if (Phase != RunPhase.Cleared || !Player || (!Player.State.IsAlive&&!Codice.Multiplayer.LanSession.Host)) yield break;
            Level++; BeginRound();
        }
        public bool Travel()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if (transition || Phase != RunPhase.Cleared || !scene || !Player || !Player.State.IsAlive) return false;
            string destination = RoundRules.SceneForRound(Level + 1);
            if (!Application.CanStreamedLevelBeLoaded(destination)) { Debug.LogError("Falta escena en Build Settings: " + destination); return false; }
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.SaveTeam();
            snapshot = Player.State.Capture(); Level++; transition = true; SetPhase(RunPhase.Travelling);
            SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
            return true;
        }
        void OnDeath()
        {
            if(Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanSession.Instance.AnyAlive)return;
            StopAllCoroutines(); SetPhase(RunPhase.Defeated);
            Player.GetComponent<PlayerMechanics>()?.Cancel();
            if (scene.arena && scene.arena.ActiveBoss) scene.arena.ActiveBoss.SetPaused(true);
            foreach (var enemy in FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) enemy.SetPaused(true);
            Debug.Log("Partida terminada. Pulsa R para comenzar de nuevo desde el Calabozo.");
        }
        public bool RestartRun()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if ((Phase != RunPhase.Defeated && Phase != RunPhase.Victory) || transition) return false;
            Time.timeScale=1;
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.ResetTeam();
            transition = true; snapshot = null; Level = 1; TotalKills=0; Elapsed = RoundElapsed = 0;
            SceneManager.LoadSceneAsync(RoundRules.SceneForRound(1), LoadSceneMode.Single); return true;
        }
        public bool RestartLevel()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if(transition||string.IsNullOrEmpty(checkpoint)||(Phase!=RunPhase.Defeated&&Phase!=RunPhase.Cleared))return false;
            StopAllCoroutines();transition=true;snapshot=checkpoint;Level=checkpointLevel;Elapsed=checkpointElapsed;RoundElapsed=0;Time.timeScale=1;
            SetPhase(RunPhase.Travelling);SceneManager.LoadSceneAsync(checkpointScene,LoadSceneMode.Single);return true;
        }
        public void CheckTeamDefeat(){if(Codice.Multiplayer.LanSession.Host&&!Codice.Multiplayer.LanSession.Instance.AnyAlive)OnDeath();}
        public void ApplyLan(RunScene context,int level,RunPhase phase,float elapsed,float roundTime,int kills){scene=context;Level=level;Phase=phase;Elapsed=elapsed;RoundElapsed=roundTime;TotalKills=kills;Changed?.Invoke();}
        void SetPhase(RunPhase phase) { Phase = phase; Changed?.Invoke(); }
        public void RegisterKill(){TotalKills++;Changed?.Invoke();}
        void SpawnLoot()
        {
            // Clean previous round loot; keep quantities bounded across an endless run.
            foreach (var pickup in FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None)) Destroy(pickup.gameObject);
            foreach (var pickup in FindObjectsByType<LootPickup>(FindObjectsSortMode.None)) if (!pickup.respawns) Destroy(pickup.gameObject);
            if(scene.lootMarkers==null)return;
            for (int i = 0; i < scene.lootMarkers.Length; i++)
            {
                if (i % 2 == 0) Player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)UnityEngine.Random.Range(0, 10), Mathf.Clamp(2 + (Level - 1) / 5, 2, 4)), scene.lootMarkers[i].position);
                else LootPickup.Create(scene.lootMarkers[i].position, i%6==1?LootKind.Arrows:i%6==3?LootKind.Shuriken:LootKind.Passive, i%6==5?1:8);
            }
        }
    }
}
