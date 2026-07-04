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

        // ---- Public read API consumed by other player components ----
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool FirePressedThisFrame { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ReloadPressedThisFrame { get; private set; }

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
        }

        private void OnEnable()
        {
            _move.Enable();
            _look.Enable();
            _fire.Enable();
            _reload.Enable();
        }

        private void OnDisable()
        {
            _move.Disable();
            _look.Disable();
            _fire.Disable();
            _reload.Disable();
        }

        private void OnDestroy()
        {
            _move?.Dispose();
            _look?.Dispose();
            _fire?.Dispose();
            _reload?.Dispose();
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
            LookDelta = fromGamepad
                ? rawLook * (gamepadLookSpeed * Time.deltaTime)
                : rawLook * mouseSensitivity;

            FirePressedThisFrame = _fire.WasPressedThisFrame();
            FireHeld = _fire.IsPressed();
            ReloadPressedThisFrame = _reload.WasPressedThisFrame();
        }
    }
}
