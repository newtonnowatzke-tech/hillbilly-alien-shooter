using UnityEngine;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.Player;

namespace HillbillyAlienShooter.Horse
{
    /// <summary>
    /// Buttercup — the hillbilly's loyal steed (Packet 1.2).
    ///
    /// A state machine with momentum-based movement on a CharacterController:
    ///  • Idle    — waits where she is (initial state, by the barn).
    ///  • Follow  — trots after the player, gallops if left far behind, and
    ///              "knows a shortcut" (teleports) if abandoned entirely.
    ///              This is the "bring horse with you" mechanic; the future
    ///              alien-bag summon (Phase 2) will hook into this same state.
    ///  • Stay    — parked until whistled for.
    ///  • Mounted — ridden: W/S is analog throttle/brake along the horse's own
    ///              heading, A/D steers (tighter at low speed, wide at gallop).
    ///              The camera stays on the player's head and mouse-look remains
    ///              fully independent, so you can blast aliens sideways at full
    ///              gallop — the signature move of this game.
    ///
    /// Whistle (H) toggles Follow ↔ Stay from anywhere; interacting (E) mounts
    /// or dismounts. Movement runs through CharacterController.Move, so fences,
    /// the barn, and the new hills are all respected (collision + terrain
    /// following for free).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour, IInteractable
    {
        public enum HorseState { Idle, Follow, Stay, Mounted }

        [Header("Data (optional — falls back to defaults)")]
        [SerializeField] private HorseData data;

        [Header("Rig")]
        [Tooltip("Where the rider sits. Auto-created above the back if empty.")]
        [SerializeField] private Transform mountPoint;

        public HorseState State { get; private set; } = HorseState.Idle;
        public float CurrentSpeed { get; private set; }

        private CharacterController _cc;
        private PlayerController _rider;        // non-null only while Mounted
        private PlayerInputHandler _playerInput; // the (single) player's input, for whistle + riding
        private Transform _player;
        private float _verticalVel;
        private bool _gameActive = true;

        // -------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------
        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (data == null) data = HorseData.CreateDefault();

            if (mountPoint == null)
            {
                var mp = new GameObject("MountPoint");
                mp.transform.SetParent(transform, false);
                mp.transform.localPosition = new Vector3(0f, 1.85f, -0.15f);
                mountPoint = mp.transform;
            }
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                _player = pc.transform;
                _playerInput = pc.GetComponent<PlayerInputHandler>();
            }

            GameEvents.RaiseHorseStateChanged($"{data.displayName}: waitin' by the barn");
        }

        /// <summary>Assign tuning data + seat at build time (used by the scene builder/factory).</summary>
        public void Configure(HorseData horseData, Transform seat)
        {
            data = horseData != null ? horseData : HorseData.CreateDefault();
            if (seat != null) mountPoint = seat;
        }

        // -------------------------------------------------------------------
        // Update loop
        // -------------------------------------------------------------------
        private void Update()
        {
            if (!_gameActive) return;

            HandleWhistle();

            switch (State)
            {
                case HorseState.Mounted:
                    TickMounted();
                    break;
                case HorseState.Follow:
                    TickFollow();
                    break;
                default: // Idle / Stay — settle to a stop, keep gravity applied
                    ApplyMotion(targetSpeed: 0f, decel: data.idleDeceleration, steer: 0f);
                    break;
            }
        }

        // -------------------------------------------------------------------
        // Riding
        // -------------------------------------------------------------------
        private void TickMounted()
        {
            if (_playerInput == null) return;
            Vector2 move = _playerInput.MoveInput;

            float targetSpeed;
            float decel = data.idleDeceleration;

            if (move.y > 0.01f)
            {
                // Analog throttle: half-stick = half gallop.
                targetSpeed = data.maxSpeed * move.y;
            }
            else if (move.y < -0.01f)
            {
                if (CurrentSpeed > 0.5f)
                {
                    // Moving forward + pulling back = brake hard.
                    targetSpeed = 0f;
                    decel = data.braking;
                }
                else
                {
                    // Stopped: back up slowly.
                    targetSpeed = -data.reverseSpeed;
                }
            }
            else
            {
                targetSpeed = 0f; // coast down naturally
            }

            ApplyMotion(targetSpeed, decel, steer: move.x);
        }

        // -------------------------------------------------------------------
        // Following
        // -------------------------------------------------------------------
        private void TickFollow()
        {
            if (_player == null) { ApplyMotion(0f, data.idleDeceleration, 0f); return; }

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // Left hopelessly behind → Buttercup knows a shortcut.
            if (dist > data.teleportDistance)
            {
                TeleportBehindPlayer();
                return;
            }

            if (dist > data.followDistance)
            {
                // Trot when close, gallop when far.
                float t = Mathf.InverseLerp(data.followDistance, data.gallopDistance, dist);
                float targetSpeed = Mathf.Lerp(data.walkSpeed, data.maxSpeed, t);

                // Face the player at the speed-appropriate turn rate.
                Quaternion want = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, CurrentTurnRate() * Time.deltaTime);

                // Don't run at full speed into a turn — throttle by facing error.
                float facingDot = Mathf.Clamp01(Vector3.Dot(transform.forward, toPlayer.normalized));
                ApplyMotion(targetSpeed * Mathf.Max(0.35f, facingDot), data.idleDeceleration, steer: 0f);
            }
            else
            {
                ApplyMotion(0f, data.idleDeceleration, 0f);
            }
        }

        private void TeleportBehindPlayer()
        {
            Vector3 pos = _player.position - _player.forward * 3f + Vector3.up * 0.5f;
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            CurrentSpeed = 0f;
        }

        // -------------------------------------------------------------------
        // Shared movement core
        // -------------------------------------------------------------------
        private void ApplyMotion(float targetSpeed, float decel, float steer)
        {
            // Speed eases toward the target: accelerate with power, settle with decel/brakes.
            float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(CurrentSpeed) ? data.acceleration : decel;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, rate * Time.deltaTime);

            // Steering (mounted only passes non-zero steer). Reversing flips the wheel.
            if (Mathf.Abs(steer) > 0.01f)
            {
                float dir = CurrentSpeed < -0.05f ? -1f : 1f;
                transform.Rotate(Vector3.up, steer * dir * CurrentTurnRate() * Time.deltaTime, Space.World);
            }

            // Gravity so we ride up and down the hills.
            if (_cc.isGrounded && _verticalVel < 0f) _verticalVel = -2f;
            _verticalVel += data.gravity * Time.deltaTime;

            Vector3 velocity = transform.forward * CurrentSpeed + Vector3.up * _verticalVel;
            _cc.Move(velocity * Time.deltaTime);
        }

        /// <summary>Turn rate narrows as speed rises: nimble at a stand, wide arcs at a gallop.</summary>
        private float CurrentTurnRate()
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / Mathf.Max(0.01f, data.maxSpeed));
            return Mathf.Lerp(data.turnSpeedStanding, data.turnSpeedAtMax, speed01);
        }

        // -------------------------------------------------------------------
        // Whistle: Follow <-> Stay (and the initial "come here, girl!")
        // -------------------------------------------------------------------
        private void HandleWhistle()
        {
            if (State == HorseState.Mounted) return;
            if (_playerInput == null || !_playerInput.WhistlePressedThisFrame) return;

            if (State == HorseState.Follow)
            {
                State = HorseState.Stay;
                GameEvents.RaiseHorseStateChanged($"{data.displayName}: stayin' put");
            }
            else // Idle or Stay → come along
            {
                State = HorseState.Follow;
                GameEvents.RaiseHorseStateChanged($"{data.displayName}: followin' you");
            }
        }

        // -------------------------------------------------------------------
        // IInteractable — mounting & dismounting
        // -------------------------------------------------------------------
        public string Prompt => State == HorseState.Mounted
            ? $"[E] Hop off {data.displayName}"
            : $"[E] Ride {data.displayName}";

        public bool CanInteract(GameObject interactor)
        {
            if (State == HorseState.Mounted)
                return _rider != null && interactor == _rider.gameObject; // only the rider can hop off
            return interactor.GetComponent<PlayerController>() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (State == HorseState.Mounted) Dismount();
            else Mount(interactor);
        }

        private void Mount(GameObject who)
        {
            var pc = who.GetComponent<PlayerController>();
            if (pc == null) return;

            _rider = pc;
            _rider.MountTo(mountPoint);
            CurrentSpeed = 0f;
            State = HorseState.Mounted;
            GameEvents.RaiseHorseStateChanged($"Ridin' {data.displayName} — yeehaw!");
        }

        private void Dismount()
        {
            if (_rider == null) return;

            _rider.DismountTo(FindDismountSpot());
            _rider = null;

            // After a ride she tags along — the "bring horse with you" default.
            State = HorseState.Follow;
            CurrentSpeed = 0f;
            GameEvents.RaiseHorseStateChanged($"{data.displayName}: followin' you");
        }

        /// <summary>Pick a clear spot beside (or behind) the horse to drop the rider.</summary>
        private Vector3 FindDismountSpot()
        {
            Vector3[] offsets =
            {
                -transform.right * 1.7f, // left side first (tradition!)
                 transform.right * 1.7f,
                -transform.forward * 2.2f,
            };

            foreach (var off in offsets)
            {
                Vector3 candidate = transform.position + off + Vector3.up * 1.0f;
                // A capsule roughly the player's size must fit there.
                if (!Physics.CheckCapsule(candidate + Vector3.up * 0.4f, candidate - Vector3.up * 0.4f, 0.35f,
                        ~0, QueryTriggerInteraction.Ignore))
                    return candidate;
            }
            // Everything blocked? Drop them on the saddle spot; gravity sorts it out.
            return transform.position + Vector3.up * 2.2f;
        }

        // -------------------------------------------------------------------
        // Game flow
        // -------------------------------------------------------------------
        private void OnGameStateChanged(GameState state)
        {
            _gameActive = state == GameState.Playing;
        }
    }
}
