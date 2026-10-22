using UnityEngine;
namespace Codice.Presentation
{
    public sealed class PresentationLibrary : ScriptableObject
    {
        public RuntimeAnimatorController medium, goblin, demon;
        public GameObject impact, dust, explosion, muzzle;
        public GameObject blood, gunSmoke, magicProjectile, swordSparks, magicCast;
        public GameObject[] passiveModels = new GameObject[5];
        public GameObject ammunitionModel;
        static PresentationLibrary cached;
        public static PresentationLibrary Load(){if(!cached)cached=Resources.Load<PresentationLibrary>("Presentation/Library");return cached;}
    }
}
