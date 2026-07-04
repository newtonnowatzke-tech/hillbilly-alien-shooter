using UnityEngine;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.Player
{
    /// <summary>
    /// First-person hillbilly controller for Packet 1.1: WASD movement relative to
    /// where you're looking, mouse-look yaw/pitch, and gravity via CharacterController.
    ///
    /// The camera lives on a child "CameraPivot" at head height. Keeping the camera
    /// on a pivot (rather than parented straight to the body) means Packet 1.3 can
    /// pull it back into a third-person rig for horse riding by only moving the
    /// camera — the control math here won't change.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float pitchClamp = 82f;

        [Header("Rig (auto-created if left empty)")]
        [SerializeField] private Transform cameraPivot;

        private CharacterController _controller;
        private PlayerInputHandler _input;
        private float _pitch;          // accumulated camera pitch
        private float _verticalVel;    // gravity accumulator
        private bool _controlEnabled = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputHandler>();
            EnsureCameraRig();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            LockCursor(true);
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_controlEnabled) return;
            HandleLook();
            HandleMove();
        }

        // Yaw rotates the whole body; pitch rotates only the camera pivot.
        private void HandleLook()
        {
            Vector2 look = _input.LookDelta;
            transform.Rotate(Vector3.up, look.x, Space.Self);

            _pitch = Mathf.Clamp(_pitch - look.y, -pitchClamp, pitchClamp);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            Vector2 move = _input.MoveInput;
            Vector3 planar = (transform.right * move.x + transform.forward * move.y);
            planar = Vector3.ClampMagnitude(planar, 1f) * moveSpeed;

            // Simple gravity so we stick to the ground / ramps.
            if (_controller.isGrounded && _verticalVel < 0f)
                _verticalVel = -2f;
            _verticalVel += gravity * Time.deltaTime;

            Vector3 velocity = planar + Vector3.up * _verticalVel;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void OnGameStateChanged(GameState state)
        {
            // Freeze the player and free the cursor on win/lose/pause.
            bool playing = state == GameState.Playing;
            _controlEnabled = playing;
            LockCursor(playing);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        /// Builds the camera rig at runtime if the designer didn't wire one, so a
        /// bare capsule + this script is instantly playable.
        /// </summary>
        private void EnsureCameraRig()
        {
            // Adopt a pre-built rig (e.g. from the scene builder) before making one,
            // so we never end up with two camera pivots.
            if (cameraPivot == null)
            {
                Transform existing = transform.Find("CameraPivot");
                if (existing != null) cameraPivot = existing;
            }

            if (cameraPivot == null)
            {
                var pivotGo = new GameObject("CameraPivot");
                pivotGo.transform.SetParent(transform, false);
                pivotGo.transform.localPosition = new Vector3(0f, 0.7f, 0f); // eye height above capsule centre
                cameraPivot = pivotGo.transform;
            }

            // Reuse an existing camera if the scene already has one on the pivot.
            var cam = cameraPivot.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                if (Camera.main != null && Camera.main.transform.IsChildOf(transform))
                {
                    cam = Camera.main;
                    cam.transform.SetParent(cameraPivot, false);
                }
                else
                {
                    var camGo = new GameObject("PlayerCamera");
                    camGo.tag = "MainCamera";
                    camGo.transform.SetParent(cameraPivot, false);
                    cam = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                }
            }

            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.05f;
        }
    }
}
