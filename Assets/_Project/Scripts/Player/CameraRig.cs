using UnityEngine;
using HillbillyAlienShooter.Horse;

namespace HillbillyAlienShooter.Player
{
    /// <summary>
    /// Third-person / first-person camera rig (Packet 1.3).
    ///
    /// The camera stays a child of the head-height CameraPivot (which already
    /// carries all yaw/pitch from PlayerController), and this component only
    /// slides it along a local offset:
    ///   • First person  → local zero (exactly the 1.1/1.2 behaviour).
    ///   • Third person  → an over-the-shoulder offset behind the pivot, pulled
    ///     back further while riding so Buttercup fits in frame.
    ///
    /// Because the camera keeps the pivot's ORIENTATION, aim direction is
    /// identical in both modes — the crosshair stays truthful and the shotgun
    /// needs no changes. A sphere-cast keeps the camera from clipping through
    /// the barn/fences/hills, ignoring the player and horse themselves.
    /// Extra juice: FOV widens slightly with gallop speed.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class CameraRig : MonoBehaviour
    {
        public enum ViewMode { ThirdPerson, FirstPerson }

        [Header("Mode")]
        [SerializeField] private ViewMode mode = ViewMode.ThirdPerson;

        [Header("Third-person framing")]
        [Tooltip("Camera offset local to the pivot: x = shoulder, y = lift, z = distance back.")]
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.45f, 0.3f, -3.8f);
        [Tooltip("Multiplies the offset while mounted so the horse fits in frame.")]
        [SerializeField] private float mountedZoomOut = 1.35f;
        [Tooltip("How snappily the camera settles into place (higher = tighter).")]
        [SerializeField] private float smoothing = 14f;

        [Header("Collision")]
        [Tooltip("Radius of the clearance sphere swept from the pivot to the camera.")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Header("FOV")]
        [SerializeField] private float baseFov = 60f;
        [Tooltip("Extra FOV at full gallop, for a sense of speed.")]
        [SerializeField] private float gallopFovBoost = 9f;

        private PlayerController _player;
        private PlayerInputHandler _input;
        private Transform _pivot;
        private Camera _cam;
        private Vector3 _currentLocalOffset;
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputHandler>();
        }

        private void Start()
        {
            // PlayerController guarantees the rig exists by end of its Awake.
            _pivot = _player.CameraPivot;
            _cam = _pivot != null ? _pivot.GetComponentInChildren<Camera>() : Camera.main;
            _currentLocalOffset = TargetLocalOffset();
            if (_cam != null) _cam.fieldOfView = baseFov;
        }

        private void Update()
        {
            if (_input.TogglePerspectivePressedThisFrame)
                mode = mode == ViewMode.ThirdPerson ? ViewMode.FirstPerson : ViewMode.ThirdPerson;
        }

        // LateUpdate so we frame AFTER the player/horse have moved this frame.
        private void LateUpdate()
        {
            if (_cam == null || _pivot == null) return;

            // Ease toward the mode's offset (also animates the FP<->TP toggle).
            Vector3 target = TargetLocalOffset();
            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            _currentLocalOffset = Vector3.Lerp(_currentLocalOffset, target, t);

            // Pull the camera in if something solid sits between pivot and camera.
            Vector3 desiredWorld = _pivot.TransformPoint(_currentLocalOffset);
            Vector3 correctedWorld = ResolveCollision(_pivot.position, desiredWorld);

            _cam.transform.position = correctedWorld;
            _cam.transform.rotation = _pivot.rotation; // same aim as first person

            UpdateFov(t);
        }

        private Vector3 TargetLocalOffset()
        {
            if (mode == ViewMode.FirstPerson) return Vector3.zero;
            return _player.IsMounted ? thirdPersonOffset * mountedZoomOut : thirdPersonOffset;
        }

        /// <summary>
        /// Sphere-cast from the pivot toward the desired camera spot; if world
        /// geometry is in the way, park the camera just in front of the hit.
        /// The player's own hierarchy and any horse are ignored.
        /// </summary>
        private Vector3 ResolveCollision(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.001f) return to;
            dir /= dist;

            int count = Physics.SphereCastNonAlloc(from, collisionRadius, dir, _hits, dist,
                ~0, QueryTriggerInteraction.Ignore);

            float nearest = dist;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null) continue;
                if (hit.transform.IsChildOf(transform.root)) continue;                     // us (or our horse while mounted)
                if (hit.collider.GetComponentInParent<HorseController>() != null) continue; // any horse on foot too
                if (hit.distance > 0.001f && hit.distance < nearest)
                    nearest = hit.distance;
            }

            return from + dir * Mathf.Max(0.15f, nearest - 0.05f);
        }

        private void UpdateFov(float t)
        {
            float targetFov = baseFov;
            if (_player.IsMounted)
            {
                // Player is parented to the horse while riding, so this walk is short.
                var horse = GetComponentInParent<HorseController>();
                if (horse != null)
                    targetFov += gallopFovBoost * horse.SpeedNormalized;
            }
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, t);
        }
    }
}
