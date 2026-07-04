using System.Collections.Generic;
using UnityEngine;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.Livestock
{
    /// <summary>
    /// A cow. Wanders lazily around the farm until an alien latches a beam on it.
    /// While being abducted its abduction meter fills; if the alien is shot off,
    /// the meter slowly drains and the cow wriggles back down (so saving a cow
    /// mid-lift is a real, satisfying outcome). When the meter hits 1 the cow is
    /// rustled and removed.
    ///
    /// Keeps a static registry (<see cref="Alive"/>) so aliens can find the
    /// nearest cow without per-frame FindObjectsOfType calls, and so tallies are
    /// cheap to broadcast.
    /// </summary>
    public class Cattle : MonoBehaviour
    {
        // ---- Static registry / tallies ----
        public static readonly List<Cattle> Alive = new List<Cattle>();
        public static int TakenCount { get; private set; }
        public static int SavedCount => Alive.Count;              // still on the farm
        public static int TotalCount => Alive.Count + TakenCount; // established at load

        [Header("Abduction")]
        [Tooltip("How high the cow is lifted at full abduction (metres).")]
        [SerializeField] private float liftHeight = 6f;
        [Tooltip("Meter drain per second when NOT being beamed.")]
        [SerializeField] private float recoverPerSecond = 0.25f;

        [Header("Idle wander (flavour)")]
        [SerializeField] private float wanderSpeed = 0.6f;
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float wanderRetargetTime = 4f;

        public float Progress01 { get; private set; }
        public bool IsBeingAbducted => _beamedThisFrame || _beamedLastFrame;

        private Vector3 _groundPos;
        private Vector3 _wanderTarget;
        private float _nextWanderTime;
        private bool _beamedThisFrame;
        private bool _beamedLastFrame;
        private bool _taken;

        private void OnEnable()
        {
            if (!Alive.Contains(this))
                Alive.Add(this);
            _groundPos = transform.position;
            _wanderTarget = _groundPos;
            BroadcastCounts();
        }

        private void OnDisable()
        {
            Alive.Remove(this);
            if (!_taken) // only broadcast here for non-taken removals (scene teardown)
                BroadcastCounts();
        }

        /// <summary>
        /// Reset the static tallies. Call once at scene start (the GameManager does
        /// this) so counts are correct after a restart with fast play-mode.
        /// </summary>
        public static void ResetTallies()
        {
            Alive.Clear();
            TakenCount = 0;
        }

        /// <summary>Called by an alien every frame while its beam is on this cow.</summary>
        public void Beam(float progressPerSecond)
        {
            if (_taken) return;
            _beamedThisFrame = true;
            Progress01 = Mathf.Clamp01(Progress01 + progressPerSecond * Time.deltaTime);
            if (Progress01 >= 1f)
                Rustled();
        }

        private void Update()
        {
            if (_taken) return;

            if (!_beamedThisFrame)
            {
                // Not being beamed: recover and wander around the pasture.
                Progress01 = Mathf.Clamp01(Progress01 - recoverPerSecond * Time.deltaTime);
                if (Progress01 <= 0f)
                    Wander();
            }

            // Lift toward the sky proportional to abduction progress + a little spin.
            Vector3 pos = _groundPos + Vector3.up * (liftHeight * Progress01);
            transform.position = pos;
            if (Progress01 > 0.01f)
                transform.Rotate(Vector3.up, 120f * Progress01 * Time.deltaTime, Space.World);

            _beamedLastFrame = _beamedThisFrame;
            _beamedThisFrame = false;
        }

        private void Wander()
        {
            if (Time.time >= _nextWanderTime)
            {
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                _wanderTarget = _groundPos + new Vector3(r.x, 0f, r.y);
                _nextWanderTime = Time.time + wanderRetargetTime;
            }

            Vector3 flat = new Vector3(_wanderTarget.x, _groundPos.y, _wanderTarget.z);
            _groundPos = Vector3.MoveTowards(_groundPos, flat, wanderSpeed * Time.deltaTime);
        }

        private void Rustled()
        {
            _taken = true;
            Alive.Remove(this);
            TakenCount++;
            BroadcastCounts();
            // TODO (Packet 4.3): swap for a "beam-away" VFX + moo. For now, poof.
            Destroy(gameObject);
        }

        private static void BroadcastCounts()
        {
            GameEvents.RaiseCattleCountsChanged(SavedCount, TakenCount, TotalCount);
        }
    }
}
