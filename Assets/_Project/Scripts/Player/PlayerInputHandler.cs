using UnityEngine;
using UnityEngine.InputSystem;

namespace HillbillyAlienShooter.Player
{
    /// <summary>
    /// Single ownership point for all player input, built on the New Input System.
    /// Other components (PlayerController, Shotgun) READ from here instead of
    /// touching Keyboard/Mouse directly — so re-binding, gamepad support, or a
    /// future .inputactions asset only ever changes THIS file.
    ///
    /// The action map is created in code on purpose for Packet 1.1: it means the
    /// game "just works" with zero asset wiring. In the controls-polish packet
    /// (1.3) we can migrate these to a serialized InputActionAsset + rebinding UI
    /// without changing any consumer.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Look sensitivity")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float gamepadLookSpeed = 220f; // degrees/sec at full stick

        private InputAction _move;
        private InputAction _look;
        private InputAction _fire;
        private InputAction _reload;
        private InputAction _interact;
        private InputAction _whistle;
        private InputAction _togglePerspective;
        private InputAction _upgrade;

        // ---- Public read API consumed by other player components ----
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool FirePressedThisFrame { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ReloadPressedThisFrame { get; private set; }
        public bool InteractPressedThisFrame { get; private set; }
        public bool WhistlePressedThisFrame { get; private set; }
        public bool TogglePerspectivePressedThisFrame { get; private set; }
        public bool UpgradePressedThisFrame { get; private set; }

        // ---- Settings surface (persisted/driven by the pause menu) ----
        /// <summary>Mouse look sensitivity (per-pixel multiplier).</summary>
        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, 0.01f, 0.5f);
        }

        /// <summary>Invert vertical look.</summary>
        public bool InvertY { get; set; }

        private void Awake()
        {
            // Movement: WASD / arrows / left stick as a composite Vector2.
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            // Look: mouse delta (pixels) or right stick.
            _look = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
            _look.AddBinding("<Mouse>/delta");
            _look.AddBinding("<Gamepad>/rightStick");

            // Fire: LMB / right trigger.
            _fire = new InputAction("Fire", InputActionType.Button);
            _fire.AddBinding("<Mouse>/leftButton");
            _fire.AddBinding("<Gamepad>/rightTrigger");

            // Reload: R / West face button.
            _reload = new InputAction("Reload", InputActionType.Button);
            _reload.AddBinding("<Keyboard>/r");
            _reload.AddBinding("<Gamepad>/buttonWest");

            // Interact (mount horse, future pickups): E / North face button.
            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonNorth");

            // Whistle for the horse (follow/stay toggle): H / D-pad up.
            _whistle = new InputAction("Whistle", InputActionType.Button);
            _whistle.AddBinding("<Keyboard>/h");
            _whistle.AddBinding("<Gamepad>/dpad/up");

            // Camera first/third person toggle: V / right stick click.
            _togglePerspective = new InputAction("TogglePerspective", InputActionType.Button);
            _togglePerspective.AddBinding("<Keyboard>/v");
            _togglePerspective.AddBinding("<Gamepad>/rightStickPress");

            // Jury-rig a wild upgrade (spend tech): Q / left shoulder.
            _upgrade = new InputAction("Upgrade", InputActionType.Button);
            _upgrade.AddBinding("<Keyboard>/q");
            _upgrade.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            _move.Enable();
            _look.Enable();
            _fire.Enable();
            _reload.Enable();
            _interact.Enable();
            _whistle.Enable();
            _togglePerspective.Enable();
            _upgrade.Enable();
        }

        private void OnDisable()
        {
            _move.Disable();
            _look.Disable();
            _fire.Disable();
            _reload.Disable();
            _interact.Disable();
            _whistle.Disable();
            _togglePerspective.Disable();
            _upgrade.Disable();
        }

        private void OnDestroy()
        {
            _move?.Dispose();
            _look?.Dispose();
            _fire?.Dispose();
            _reload?.Dispose();
            _interact?.Dispose();
            _whistle?.Dispose();
            _togglePerspective?.Dispose();
            _upgrade?.Dispose();
        }

        private void Update()
        {
            MoveInput = _move.ReadValue<Vector2>();

            // Mouse delta is already a per-frame pixel delta (do NOT multiply by
            // deltaTime). Gamepad stick is a -1..1 value, so scale it into a
            // comparable per-frame delta. We detect the source by which device
            // most recently actuated the action.
            Vector2 rawLook = _look.ReadValue<Vector2>();
            bool fromGamepad = _look.activeControl != null && _look.activeControl.device is Gamepad;
            Vector2 look = fromGamepad
                ? rawLook * (gamepadLookSpeed * Time.deltaTime)
                : rawLook * mouseSensitivity;
            if (InvertY) look.y = -look.y;
            LookDelta = look;

            FirePressedThisFrame = _fire.WasPressedThisFrame();
            FireHeld = _fire.IsPressed();
            ReloadPressedThisFrame = _reload.WasPressedThisFrame();
            InteractPressedThisFrame = _interact.WasPressedThisFrame();
            WhistlePressedThisFrame = _whistle.WasPressedThisFrame();
            TogglePerspectivePressedThisFrame = _togglePerspective.WasPressedThisFrame();
            UpgradePressedThisFrame = _upgrade.WasPressedThisFrame();
        }
    }
}
