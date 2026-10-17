using UnityEngine;
namespace Codice.Presentation
{
    public sealed class PresentationLibrary : ScriptableObject
    {
        public RuntimeAnimatorController medium, goblin, demon;
        public GameObject impact, dust, explosion, muzzle;
        static PresentationLibrary cached;
        public static PresentationLibrary Load(){if(!cached)cached=Resources.Load<PresentationLibrary>("Presentation/Library");return cached;}
    }
}
