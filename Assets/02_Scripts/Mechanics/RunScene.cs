using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class RunScene : MonoBehaviour
    {
        public PlayerCharacter player;
        public BasicEnemyEncounter dungeon;
        public BossEncounter arena;
        public RunPortal portal;
        public Transform[] lootMarkers;
        public bool IsArena => arena && !dungeon;
        public bool UsesBossEncounter(int round) => arena && RoundRules.IsBoss(round);
        public bool UsesDungeonEncounter(int round) => dungeon && !UsesBossEncounter(round);
        void Awake()
        {
            if (dungeon) dungeon.startOnPlay = false;
            if (arena) { arena.startOnPlay = false; arena.managedByRun = true; }
        }
        System.Collections.IEnumerator Start()
        {
            yield return null;
            if(Codice.Multiplayer.LanSession.Client){TreasureChest.InstallInScene();Codice.Multiplayer.LanSession.Instance.EnterClient(this);yield break;}
            TreasureChest.InstallInScene();
            if (!RunDirector.Instance) new GameObject("Run director").AddComponent<RunDirector>();
            Codice.UI.RunHUD.Install(this);
            RunDirector.Instance.Enter(this);
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EnterHost(this);
        }
    }
}
