using Codice.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Mechanics
{
    public enum LootKind { Ammunition, Passive }
    public sealed class LootPickup : MonoBehaviour, IPlayerInteractable
    {
        public LootKind kind;
        public PassiveKind passive;
        public int amount = 4;
        public bool respawns;
        public float respawnSeconds = 15;
        public GameObject visual;
        public bool Available => Time.time >= readyAt;
        float readyAt;
        bool consumed;
        Vector3 visualRest;
        float visualPhase;
        void Start()
        {
            var library = Codice.Presentation.PresentationLibrary.Load();
            var prefab = library ? (kind == LootKind.Ammunition ? library.ammunitionModel :
                library.passiveModels != null && (int)passive < library.passiveModels.Length ? library.passiveModels[(int)passive] : null) : null;
            if (prefab)
            {
                if (visual) Destroy(visual);
                visual = Instantiate(prefab, transform, false);
                visual.name = kind == LootKind.Passive ? "Power-up • " + passive : "Munición";
            }
            if (visual) visualRest = visual.transform.localPosition;
            visualPhase = Random.value * Mathf.PI * 2;
        }
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player) TryCollect(player);
        }
        void Update()
        {
            if (visual && !consumed)
            {
                visual.transform.localPosition = visualRest + Vector3.up * (.10f + Mathf.Sin(Time.time * 2 + visualPhase) * .08f);
                visual.transform.Rotate(0, 35 * Time.deltaTime, 0, Space.Self);
            }
            if (respawns && consumed && Available)
            { consumed = false; SetVisible(true); }
        }
        void SetVisible(bool visible)
        { if (visual) visual.SetActive(visible); var collider = GetComponent<Collider>(); if (collider) collider.enabled = visible; }
        public void Interact(PlayerCharacter player)
        {
            var keys = Keyboard.current;
            int slot = keys != null && keys.leftShiftKey.isPressed ? 0 : keys != null && keys.leftCtrlKey.isPressed ? 1 : keys != null && keys.leftAltKey.isPressed ? 2 : -1;
            if (!TryCollect(player, slot)) Debug.Log("Botín: selecciona un arma con munición faltante o usa Shift/Ctrl/Alt + E para reemplazar pasiva 1/2/3.", this);
        }
        public bool TryCollect(PlayerCharacter player, int replaceSlot = -1)
        {
            if (consumed || !Available || !player || !player.State.IsAlive || player.ControlsBlocked || Time.timeScale == 0 || Vector3.Distance(transform.position, player.transform.position) > 3.5f) return false;
            bool received = kind == LootKind.Ammunition ? player.State.AddAmmunition(amount) : player.State.EquipPassive(passive, replaceSlot);
            if (!received) return false;
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup);
            consumed = true; SetVisible(false);
            if (respawns) readyAt = Time.time + respawnSeconds; else Destroy(gameObject);
            return true;
        }
        public static LootPickup Create(Vector3 position, LootKind kind, int amount)
        {
            var obj = new GameObject(kind == LootKind.Passive ? "Sello de pasiva (E)" : "Munición (E)");
            obj.transform.position = position;
            var pickup = obj.AddComponent<LootPickup>(); pickup.kind = kind; pickup.amount = amount;
            pickup.passive = (PassiveKind)Random.Range(0, 5);
            var trigger = obj.AddComponent<SphereCollider>(); trigger.radius = .4f; trigger.isTrigger = true;
            var body = obj.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var visual = GameObject.CreatePrimitive(kind == LootKind.Passive ? PrimitiveType.Sphere : PrimitiveType.Cube);
            visual.name = "Pickup indicator"; Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(obj.transform, false); visual.transform.localScale = Vector3.one * .3f;
            pickup.visual = visual; return pickup;
        }
        public static void DropFromEnemy(PlayerCharacter player, Vector3 position)
        {
            if (!RunDirector.Instance || RunDirector.Instance.Player != player) return;
            float roll = Random.value;
            if (roll < .12f)
                player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)Random.Range(0, 10), Mathf.Clamp(2 + (RunDirector.Instance.Level - 1) / 5, 2, 4)), position + Vector3.up * .3f);
            else if (roll < .45f) Create(position + Vector3.up * .4f, LootKind.Ammunition, 3);
        }
    }
}
