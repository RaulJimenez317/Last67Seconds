using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation
{
    [CreateAssetMenu(menuName="Codice/Weapon visuals")]
    public sealed class WeaponVisualCatalog:ScriptableObject
    {
        public GameObject[] tiers=new GameObject[40];
        public GameObject katana,shuriken;
        public Sprite[] icons=new Sprite[42];
        public Sprite Icon(WeaponInstance w)=>icons!=null&&icons.Length==42?icons[w.katana?40:w.shuriken?41:(int)w.kind*4+Mathf.Clamp(w.tier-1,0,3)]:null;
        static WeaponVisualCatalog cached;
        public static WeaponVisualCatalog Load()=>cached?cached:(cached=Resources.Load<WeaponVisualCatalog>("WeaponVisualCatalog"));
        public GameObject Get(WeaponInstance weapon)=>weapon==null?null:weapon.katana?katana:weapon.shuriken?shuriken:tiers[(int)weapon.kind*4+Mathf.Clamp(weapon.tier-1,0,3)];
        public static bool Apply(GameObject wrapper,WeaponInstance weapon)
        {
            var catalog=Load();var prefab=catalog?catalog.Get(weapon):null;if(!wrapper||!prefab)return false;
            foreach(Transform child in wrapper.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var model=Instantiate(prefab,wrapper.transform);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            wrapper.transform.localRotation=weapon.kind==WeaponKind.Musket?Quaternion.Euler(90,0,0):Quaternion.identity;
            return true;
        }
    }
}
