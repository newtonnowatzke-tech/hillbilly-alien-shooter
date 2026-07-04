using System.Collections;
using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;

namespace HillbillyAlienShooter.Enemies
{
    /// <summary>
    /// The "Little Alien" scout for Packet 1.1. Behaviour:
    ///  1. Find the nearest cow and shamble toward it.
    ///  2. In grab range, fire an abduction beam that fills the cow's meter.
    ///  3. If no cattle remain, charge the hillbilly and melee him.
    ///  4. On death: score, squash, and despawn.
    ///
    /// Moves kinematically over the flat farm (no NavMesh needed for 1.1). A
    /// static <see cref="ActiveCount"/> lets the WaveSpawner know when the wave is
    /// clear without polling the scene.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class AlienEnemy : MonoBehaviour
    {
        /// <summary>Number of aliens currently alive in the scene.</summary>
        public static int ActiveCount { get; private set; }

        [Header("Data (optional — falls back to defaults)")]
        [SerializeField] private EnemyData data;

        private Health _health;
        private Transform _player;
        private HillbillyAlienShooter.Livestock.Cattle _targetCow;
        private float _nextMeleeTime;
        private bool _dead;

        private LineRenderer _beam;
        private Vector3 _baseScale;
        private Coroutine _squashRoutine;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (data == null) data = EnemyData.CreateDefault();
            _health.SetMaxHealth(data.maxHealth);
            _baseScale = transform.localScale;
            SetupBeam();
        }

        private void OnEnable()
        {
            ActiveCount++;
            GameEvents.RaiseEnemyCountChanged(ActiveCount);
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            GameEvents.RaiseEnemyCountChanged(ActiveCount);
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        /// <summary>Reset the shared counter (called by GameManager on scene start).</summary>
        public static void ResetCount() => ActiveCount = 0;

        /// <summary>Assign tuning data at spawn time (used by the WaveSpawner).</summary>
        public void Configure(EnemyData enemyData)
        {
            data = enemyData != null ? enemyData : EnemyData.CreateDefault();
            if (_health == null) _health = GetComponent<Health>();
            _health.SetMaxHealth(data.maxHealth);
        }

        private void Start()
        {
            var pc = FindFirstObjectByType<HillbillyAlienShooter.Player.PlayerController>();
            if (pc != null) _player = pc.transform;
        }

        private void Update()
        {
            if (_dead) return;

            _targetCow = FindNearestCow();

            if (_targetCow != null)
                HuntCow();
            else
                HuntPlayer();
        }

        // -------------------------------------------------------------------
        // Behaviours
        // -------------------------------------------------------------------
        private void HuntCow()
        {
            Vector3 cowPos = _targetCow.transform.position;
            float dist = FlatDistance(transform.position, cowPos);

            if (dist > data.grabRange)
            {
                MoveTowards(cowPos);
                SetBeam(false, cowPos);
            }
            else
            {
                FaceFlat(cowPos);
                _targetCow.Beam(data.abductRatePerSecond);
                SetBeam(true, cowPos);
            }
        }

        private void HuntPlayer()
        {
            SetBeam(false, transform.position);
            if (_player == null) return;

            Vector3 target = _player.position;
            float dist = FlatDistance(transform.position, target);

            if (dist > data.meleeRange)
            {
                MoveTowards(target);
            }
            else
            {
                FaceFlat(target);
                if (Time.time >= _nextMeleeTime)
                {
                    _nextMeleeTime = Time.time + data.meleeCooldown;
                    var dmg = _player.GetComponentInChildren<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                    {
                        Vector3 dir = (target - transform.position).normalized;
                        dmg.TakeDamage(new DamageInfo(data.meleeDamage, target, dir, source: gameObject));
                    }
                }
            }
        }

        private void MoveTowards(Vector3 worldTarget)
        {
            Vector3 flatTarget = new Vector3(worldTarget.x, transform.position.y, worldTarget.z);
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, data.moveSpeed * Time.deltaTime);
            FaceFlat(worldTarget);
        }

        private void FaceFlat(Vector3 worldTarget)
        {
            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        private HillbillyAlienShooter.Livestock.Cattle FindNearestCow()
        {
            var list = HillbillyAlienShooter.Livestock.Cattle.Alive;
            HillbillyAlienShooter.Livestock.Cattle nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var cow = list[i];
                if (cow == null) continue;
                float d = FlatSqrDistance(transform.position, cow.transform.position);
                if (d < best) { best = d; nearest = cow; }
            }
            return nearest;
        }

        // -------------------------------------------------------------------
        // Damage / death juice
        // -------------------------------------------------------------------
        private void OnDamaged(DamageInfo info)
        {
            if (_dead) return;
            if (_squashRoutine != null) StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(SquashRoutine());
        }

        private IEnumerator SquashRoutine()
        {
            // Quick low-poly hit react: squash & recover.
            transform.localScale = Vector3.Scale(_baseScale, new Vector3(1.25f, 0.7f, 1.25f));
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 8f;
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, t);
                yield return null;
            }
            transform.localScale = _baseScale;
        }

        private void OnDied(Health _)
        {
            if (_dead) return;
            _dead = true;
            SetBeam(false, transform.position);
            GameEvents.RaiseEnemyKilled(transform.position, data.scoreValue);
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            // Shrink-and-poof placeholder death (real VFX in Packet 4.3).
            float t = 0f;
            Vector3 from = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                transform.localScale = Vector3.Lerp(from, Vector3.zero, t);
                transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
                yield return null;
            }
            Destroy(gameObject);
        }

        // -------------------------------------------------------------------
        // Beam FX
        // -------------------------------------------------------------------
        private void SetupBeam()
        {
            var go = new GameObject("AbductBeam");
            go.transform.SetParent(transform, false);
            _beam = go.AddComponent<LineRenderer>();
            _beam.useWorldSpace = true;
            _beam.widthMultiplier = 0.25f;
            _beam.numCapVertices = 2;
            _beam.material = new Material(Shader.Find("Sprites/Default"));
            _beam.startColor = new Color(0.5f, 1f, 0.6f, 0.7f);
            _beam.endColor = new Color(0.3f, 1f, 0.9f, 0.15f);
            _beam.positionCount = 2;
            _beam.enabled = false;
        }

        private void SetBeam(bool on, Vector3 cowPos)
        {
            if (_beam == null) return;
            _beam.enabled = on;
            if (!on) return;
            _beam.SetPosition(0, transform.position + Vector3.up * 0.6f);
            _beam.SetPosition(1, cowPos);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return (a - b).sqrMagnitude;
        }
    }
}
