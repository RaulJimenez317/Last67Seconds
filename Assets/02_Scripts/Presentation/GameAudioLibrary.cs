using UnityEngine;
namespace Codice.Presentation
{
    public enum SoundCue { Click, Swing, Bow, Gun, Magic, Heavy, Hit, Block, Hurt, Death, Footstep, Coin, Pickup, Chest, Purchase, Error, Reload, Power, Dash, Explosion, Victory, Defeat, Boss }
    [System.Serializable] public sealed class SoundEntry
    {
        public SoundCue cue;
        public AudioClip[] clips;
        [Range(0,1)] public float volume=.5f;
        [Min(0)] public float cooldown=.06f;
    }
    public sealed class GameAudioLibrary : ScriptableObject
    {
        public AudioClip menu, dungeon, arena;
        public SoundEntry[] effects;
    }
}
