using System;
using UnityEngine;
using UnityEngine.Events;

namespace Codice.Characters
{
    public enum OfferKind { Weapon, Passive, Healing }
    [Serializable]
    public sealed class MerchantOffer
    {
        public OfferKind kind;
        public WeaponKind weapon;
        public PassiveKind passive;
        public int tier;
        public int price;
        public bool sold;
    }
    public sealed class Merchant : MonoBehaviour, IPlayerInteractable
    {
        public GameObject visual;
        public Collider interactionCollider;
        [Tooltip("Prices are provisional; the GDD only fixes potion and passive prices.")]
        public int tierTwoPrice = 120, tierThreePrice = 220, tierFourPrice = 320;
        public UnityEvent onIntermissionStarted = new UnityEvent();
        public UnityEvent onRoundContinued = new UnityEvent();
        public event Action StockChanged;
        [SerializeField] MerchantOffer[] offers = new MerchantOffer[0];
        [SerializeField] bool available;
        int lastRound;
        PlayerCharacter customer;
        public bool Available => available;
        public PlayerCharacter Customer => customer;
        public int OfferCount => offers.Length;
        public MerchantOffer GetOffer(int index) => index >= 0 && index < offers.Length ? offers[index] : null;
        void Awake() { SetVisible(available); }
        void Update()
        {
            if (customer && (!customer.State.IsAlive || Vector3.Distance(customer.transform.position, transform.position) > 4)) CloseShop();
        }
        void OnDisable() { CloseShop(); }
        // Called by the future wave controller; does not implement waves or enemy spawning.
        public bool ReportRoundProgress(int round, int defeated, int total)
        {
            if (round <= lastRound || total <= 0 || defeated < (total + 1) / 2) return false;
            return BeginIntermission(round);
        }
        public bool BeginIntermission(int round)
        {
            if (round <= lastRound || available) return false;
            lastRound = round; available = true;
            int tier = Mathf.Clamp(2 + (round - 1) / 5, 2, 4);
            int price = Mathf.Max(0, tier == 2 ? tierTwoPrice : tier == 3 ? tierThreePrice : tierFourPrice);
            var first = (WeaponKind)UnityEngine.Random.Range(0, 10);
            var second = (WeaponKind)(((int)first + UnityEngine.Random.Range(1, 10)) % 10);
            offers = new[] {
                new MerchantOffer { kind = OfferKind.Weapon, weapon = first, tier = tier, price = price },
                new MerchantOffer { kind = OfferKind.Weapon, weapon = second, tier = tier, price = price },
                new MerchantOffer { kind = OfferKind.Passive, passive = (PassiveKind)UnityEngine.Random.Range(0, 5), price = 150 },
                new MerchantOffer { kind = OfferKind.Healing, price = 80 }
            };
            SetVisible(true); StockChanged?.Invoke(); onIntermissionStarted.Invoke(); return true;
        }
        public void Interact(PlayerCharacter player)
        {
            if (!available || customer || !player || !player.State.IsAlive || player.ControlsBlocked || Vector3.Distance(player.transform.position, transform.position) > 4) return;
            customer = player; player.BeginShop(this);
            Debug.Log("Mercader: 1/2 comprar arma; 3 comprar pasiva; 4 curar; E/Escape cerrar; Enter continuar ronda. Oro: " + player.State.Gold, this);
            for (int i = 0; i < offers.Length; i++) Debug.Log($"{i + 1}: {offers[i].kind} {offers[i].weapon} {offers[i].passive}, Tier {offers[i].tier}, {offers[i].price} oro, vendido={offers[i].sold}", this);
        }
        public bool TryPurchase(PlayerCharacter player, int index, int replacePassiveSlot = -1)
        {
            var offer = GetOffer(index);
            if (!available || !player || customer != player || !player.State.IsAlive || Vector3.Distance(player.transform.position, transform.position) > 4 || offer == null || offer.sold || offer.price < 0 || player.State.Gold < offer.price) return false;
            if (offer.kind == OfferKind.Healing && player.State.Health >= player.State.MaxHealth) return false;
            if (offer.kind == OfferKind.Passive && !player.State.CanEquipPassive(offer.passive, replacePassiveSlot)) return false;
            // Reserve before callbacks so repeated UI events cannot purchase the same offer twice.
            offer.sold = true;
            if (!player.State.SpendGold(offer.price)) { offer.sold = false; return false; }
            if (offer.kind == OfferKind.Healing) player.State.Heal(player.State.MaxHealth * .5f);
            else if (offer.kind == OfferKind.Passive) player.State.EquipPassive(offer.passive, replacePassiveSlot);
            else
            {
                var dropped = player.State.Equip(new WeaponInstance(offer.weapon, offer.tier));
                if (dropped != null)
                {
                    player.GetComponent<PlayerCombat>()?.CreatePickup(dropped, player.transform.position + player.transform.forward + Vector3.up * .25f);
                }
            }
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Purchase);
            StockChanged?.Invoke(); return true;
        }
        public void CloseShop()
        { var previous = customer; customer = null; if (previous) previous.EndShop(); }
        public void ContinueRound()
        {
            if (!available) return;
            CloseShop(); available = false; SetVisible(false); onRoundContinued.Invoke();
        }
        void SetVisible(bool value)
        { if (visual) visual.SetActive(value); if (interactionCollider) interactionCollider.enabled = value; }
#if UNITY_EDITOR
        [ContextMenu("Prueba / Abrir interludio (solo Play)")]
        void PreviewIntermission() { if (Application.isPlaying) BeginIntermission(lastRound + 1); }
#endif
    }
}
