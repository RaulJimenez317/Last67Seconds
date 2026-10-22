using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Multiplayer
{
    public sealed class LanHeldWeapon : MonoBehaviour
    {
        public PlayerCharacter player;
        GameObject model;string key;
        void LateUpdate(){if(player)Apply(player.State.ActiveWeapon);}
        public void Apply(WeaponInstance weapon)
        {
            string next=weapon==null?"":weapon.kind+":"+weapon.tier+":"+weapon.katana+":"+weapon.shuriken;
            if(next==key)return;key=next;if(model)Destroy(model);if(weapon==null)return;
            var root=player?player.classVisuals[(int)player.State.Class].transform:transform;
            string slot=weapon.kind==WeaponKind.Bow&&!weapon.shuriken||weapon.kind==WeaponKind.Shield?"handslot.l":"handslot.r";
            var hand=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.ToLowerInvariant()==slot);
            var prefab=WeaponVisualCatalog.Load()?.Get(weapon);if(!hand||!prefab)return;
            model=Instantiate(prefab,hand);model.transform.localPosition=Vector3.zero;model.transform.localRotation=weapon.kind==WeaponKind.Musket?Quaternion.Euler(90,0,0):weapon.kind==WeaponKind.Dagger?Quaternion.Euler(0,0,180):Quaternion.identity;
            model.transform.localScale=Vector3.one/Mathf.Max(.01f,hand.lossyScale.x);
            foreach(var collider in model.GetComponentsInChildren<Collider>())Destroy(collider);
        }
    }
}
