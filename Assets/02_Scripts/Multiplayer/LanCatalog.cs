using System;
using UnityEngine;
namespace Codice.Multiplayer
{
    public sealed class LanCatalog : ScriptableObject
    {
        [Serializable] public struct Entry { public string key; public GameObject prefab; }
        public Entry[] entries;
        public GameObject Find(string key){foreach(var e in entries)if(e.key==key)return e.prefab;return null;}
    }
}
