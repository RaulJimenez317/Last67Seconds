using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerCharacter : MonoBehaviour, IDamageReceiver
    {
        public PlayerClass initialClass;
        public Camera view;
        public GameObject[] classVisuals;
        [Min(.01f)] public float mouseSensitivity = .09f;
        public LayerMask interactionMask = ~0;
        [Min(.1f)] public float jumpHeight = 1.25f;
        public bool Grounded => controller && controller.isGrounded;
        [SerializeField] CharacterState state = new CharacterState();
        public CharacterState State => state;
        public float DashCooldown => Mathf.Max(0, nextDash - Time.time);
        public float SecondDashCooldown => Mathf.Max(0, secondDashReady - Time.time);
        public bool IsDashing => Time.time < dashEnds;
        public bool ControlsBlocked { get; private set; }
        public event Action<Merchant> ShopOpened;
        public event Action SpecialRequested;
        CharacterController controller;
        float pitch, verticalSpeed, dashEnds, nextDash;
        float secondDashReady;
        Vector3 dashDirection;
        Merchant merchant;
        bool initialized;
        float slowUntil, staggerUntil, movementMultiplier = 1;
        public float MovementMultiplier => Time.time < slowUntil ? movementMultiplier : 1;
        public bool IsStaggered => Time.time < staggerUntil;
        // Boss crowd control uses deadlines so destroyed puddles cannot leave permanent debuffs.
        public void ApplyBossSlow(float duration) { slowUntil = Time.time + duration; movementMultiplier = .65f; }
        public void ApplyBossKnockdown() { if (!IsDashing && !(GetComponent<Codice.Mechanics.PlayerMechanics>()?.Invulnerable ?? false)) staggerUntil = Time.time + .6f; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            initialClass = Codice.UI.RunNavigation.SelectedClass;
            state.Initialize(initialClass); initialized = true;
            ApplyVisual(); state.Died += OnDeath;
            Codice.Presentation.ActorPresentation.Install(gameObject);
            mouseSensitivity = PlayerPrefs.GetFloat("Codice.Sensitivity", mouseSensitivity);
        }
        void Start() { CaptureCursor(true); }
        void OnDestroy() { state.Died -= OnDeath; }
        void OnDisable() { if (merchant) merchant.CloseShop(); CaptureCursor(false); }
        void OnApplicationFocus(bool focused) { if (!focused) CaptureCursor(false); }
        public bool SelectClass(PlayerClass selected)
        {
            // Selection is a pre-run operation: never reset health/inventory mid-combat.
            if (initialized && Application.isPlaying) return false;
            initialClass = selected; return true;
        }
        void ApplyVisual()
        {
            if (classVisuals == null) return;
            for (int i = 0; i < classVisuals.Length; i++)
                if (classVisuals[i]) classVisuals[i].SetActive(i == (int)initialClass);
        }
        void Update()
        {
            if (Codice.UI.RunNavigation.Loading) return;
            if (!state.IsAlive || Time.timeScale == 0) return;
            if (!ControlsBlocked && !(Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible))
            { state.TickAmmunition(Time.deltaTime); state.TickPassives(Time.deltaTime); }
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys == null || mouse == null || !view) return;
            if (merchant)
            {
                if (keys.escapeKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame) merchant.CloseShop();
                else if (keys.enterKey.wasPressedThisFrame) merchant.ContinueRound();
                else
                {
                    int offer = keys.digit1Key.wasPressedThisFrame ? 0 : keys.digit2Key.wasPressedThisFrame ? 1 : keys.digit3Key.wasPressedThisFrame ? 2 : keys.digit4Key.wasPressedThisFrame ? 3 : -1;
                    int replace = keys.leftShiftKey.isPressed ? 0 : keys.leftCtrlKey.isPressed ? 1 : keys.leftAltKey.isPressed ? 2 : -1;
                    if (offer >= 0) Debug.Log(merchant.TryPurchase(this, offer, replace) ? "Compra realizada." : "Compra rechazada: revisa oro, salud, stock o ranuras.", this);
                }
                return;
            }
            if (Codice.UI.RunHUD.HandlesEscapeThisFrame) return;
            if (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) return;
            if (keys.escapeKey.wasPressedThisFrame) { CaptureCursor(false); return; }
            if (Cursor.lockState != CursorLockMode.Locked)
            { if (mouse.leftButton.wasPressedThisFrame) CaptureCursor(true); return; }
            if (ControlsBlocked) return;
            if (keys.tKey.wasPressedThisFrame) TryActivatePassive(0);
            if (keys.yKey.wasPressedThisFrame) TryActivatePassive(1);
            if (keys.uKey.wasPressedThisFrame) TryActivatePassive(2);
            var delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -85, 85);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 input = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized);
            if (keys.leftShiftKey.wasPressedThisFrame) TryDash(direction);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (keys.spaceKey.wasPressedThisFrame) TryJump();
            verticalSpeed += Physics.gravity.y * 2 * Time.deltaTime;
            float specialSpeed = GetComponent<Codice.Mechanics.PlayerMechanics>()?.ShadowStep == true ? 1.75f : 1;
            var collision = controller.Move(((IsDashing ? dashDirection * 18 : IsStaggered ? Vector3.zero : direction * state.MoveSpeed * MovementMultiplier * specialSpeed) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if (transform.position.y < -20) state.Damage(state.MaxHealth);
            if (keys.digit1Key.wasPressedThisFrame) state.SelectWeapon(0);
            else if (keys.digit2Key.wasPressedThisFrame) state.SelectWeapon(1);
            else if (keys.tabKey.wasPressedThisFrame || mouse.scroll.ReadValue().y != 0) state.SelectWeapon(1 - state.ActiveSlot);
            if (keys.eKey.wasPressedThisFrame) TryInteract();
            if (!ControlsBlocked && keys.qKey.wasPressedThisFrame) SpecialRequested?.Invoke();
        }
        public bool TryDash(Vector3 direction)
        {
            if (!state.IsAlive || ControlsBlocked || IsStaggered || IsDashing) return false;
            bool primaryReady = Time.time >= nextDash;
            if (!primaryReady && (!state.HasPassive(PassiveKind.MercuryBoots) || Time.time < secondDashReady)) return false;
            direction.y = 0; dashDirection = direction.sqrMagnitude > .01f ? direction.normalized : transform.forward;
            dashEnds = Time.time + .3f;
            if (primaryReady) nextDash = Time.time + 1.5f; else secondDashReady = Time.time + 1.5f;
            return true;
        }
        public bool TryJump()
        {
            if (!state.IsAlive || ControlsBlocked || IsStaggered || IsDashing || !Grounded || verticalSpeed > 0 ||
                Time.timeScale == 0 || Codice.UI.RunNavigation.Loading ||
                (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible)) return false;
            verticalSpeed = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y * 2) * jumpHeight);
            return true;
        }
        public bool TryActivatePassive(int slot)
        {
            if (ControlsBlocked || IsStaggered || Time.timeScale == 0 ||
                (Codice.UI.RunHUD.Instance && (Codice.UI.RunHUD.Instance.ResultsVisible || Codice.UI.RunHUD.Instance.Paused))) return false;
            return state.ActivatePassive(slot);
        }
        public void ReceiveDamage(DamageInfo hit)
        {
            if (IsDashing || !state.IsAlive) return;
            var mechanics = GetComponent<Codice.Mechanics.PlayerMechanics>();
            if (mechanics && mechanics.Invulnerable) return;
            if (mechanics) hit.amount = mechanics.ModifyIncoming(hit);
            if (state.Damage(hit.amount) && mechanics) mechanics.AfterDamage(hit);
        }
        public void RestoreRunState(string snapshot)
        { state.Restore(snapshot); initialClass = state.Class; ApplyVisual(); }
        public void TryInteract()
        {
            if (ControlsBlocked || !state.IsAlive) return;
            (InteractionTarget.Find(this) as IPlayerInteractable)?.Interact(this);
        }
        public void BeginShop(Merchant shop)
        { merchant = shop; ControlsBlocked = true; CaptureCursor(false); ShopOpened?.Invoke(shop); }
        public void EndShop()
        { merchant = null; ControlsBlocked = false; CaptureCursor(state.IsAlive); }
        // A new boss attempt resets the run state and all temporary movement effects.
        public bool RestartAfterDefeat(Vector3 position, Quaternion rotation)
        {
            if (state.IsAlive) return false;
            if (merchant) merchant.CloseShop();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            state.Initialize(initialClass);
            verticalSpeed = pitch = dashEnds = nextDash = secondDashReady = slowUntil = staggerUntil = 0;
            movementMultiplier = 1; dashDirection = Vector3.zero; ControlsBlocked = false;
            if (view) view.transform.localRotation = Quaternion.identity;
            CaptureCursor(true);
            return true;
        }
        void OnDeath() { if (merchant) merchant.CloseShop(); ControlsBlocked = true; CaptureCursor(false); }
        static void CaptureCursor(bool capture)
        { Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !capture; }
#if UNITY_EDITOR
        [ContextMenu("Prueba / Dar 500 oro (solo Play)")]
        void GrantTestGold() { if (Application.isPlaying) state.AddGold(500); }
#endif
    }
}
