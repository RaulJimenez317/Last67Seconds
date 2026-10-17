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
        public bool IsArena => arena;
        void Awake()
        {
            if (dungeon) dungeon.startOnPlay = false;
            if (arena) { arena.startOnPlay = false; arena.managedByRun = true; }
        }
        System.Collections.IEnumerator Start()
        {
            yield return null;
            if (!RunDirector.Instance) new GameObject("Run director").AddComponent<RunDirector>();
            Codice.UI.RunHUD.Install(this);
            RunDirector.Instance.Enter(this);
        }
    }
}
