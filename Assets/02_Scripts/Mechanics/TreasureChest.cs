using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class TreasureChest : MonoBehaviour, IPlayerInteractable
    {
        [Min(1)] public int price = 60;
        public bool Opened { get; private set; }
        public string Message { get; private set; }
        Transform lid;
        Quaternion closedRotation;
        float openProgress;
        public bool TryOpen(PlayerCharacter player)
        {
            if(Opened || !player || !player.State.IsAlive || player.ControlsBlocked || Time.timeScale == 0 || Codice.UI.RunNavigation.Loading ||
                Vector3.Distance(player.transform.position,transform.position)>4) return false;
            var combat=player.GetComponent<PlayerCombat>();
            if(!combat)return false;
            if(player.State.Gold<price){Message="Necesitas "+price+" monedas";return false;}
            Opened=true;
            if(!player.State.SpendGold(price)){Opened=false;return false;}
            int level=RunDirector.Instance?RunDirector.Instance.Level:1;
            int tier=Mathf.Clamp(1+(level-1)/5+Random.Range(0,2),1,4);
            var weapon=new WeaponInstance((WeaponKind)Random.Range(0,10),tier);
            Vector3 direction=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up).normalized;
            combat.CreatePickup(weapon,transform.position+direction*1.2f+Vector3.up*.6f);
            Message="Cofre abierto • Recoge el arma con E";
            return true;
        }
        public void Interact(PlayerCharacter player) { TryOpen(player); }
        void Awake()
        {
            foreach(var child in GetComponentsInChildren<Transform>())
                if(child!=transform && (child.name.ToLowerInvariant().Contains("lid") || child.name.ToLowerInvariant().Contains("top")))
                {lid=child;closedRotation=lid.localRotation;break;}
        }
        void Update()
        { if(Opened && lid){openProgress=Mathf.MoveTowards(openProgress,1,Time.deltaTime*2);lid.localRotation=closedRotation*Quaternion.Euler(-75*openProgress,0,0);} }
        public static void InstallInScene()
        {
            foreach(var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(!candidate.name.Equals("chest_gold",System.StringComparison.OrdinalIgnoreCase) ||
                    (candidate.parent && candidate.parent.name.Equals("chest_gold",System.StringComparison.OrdinalIgnoreCase)) || candidate.GetComponentInParent<TreasureChest>())continue;
                var renderers=candidate.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                candidate.gameObject.AddComponent<TreasureChest>();
                var interaction=new GameObject("Chest interaction");interaction.transform.position=bounds.center;interaction.transform.SetParent(candidate,true);
                var box=interaction.AddComponent<BoxCollider>();box.size=bounds.size+Vector3.one*.12f;box.isTrigger=true;
            }
        }
    }
}
