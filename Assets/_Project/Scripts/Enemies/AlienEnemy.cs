using System.Collections;
using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;

namespace HillbillyAlienShooter.Enemies
{
    /// <summary>
    /// Ground alien, role-driven by <see cref="EnemyData"/> (Packet 2.1):
    ///
    ///  Rustler (Little Alien) — finds the nearest cow, weaves toward it on an
    ///  "annoying path" (data-tuned sine sway), beams it up in grab range, and
    ///  only bothers the hillbilly once no cattle remain.
    ///
    ///  Hunter (Medium Alien) — faster and meaner: curves toward the player's
    ///  FLANK while far, then charges straight in for quick light melee swipes.
    ///
    /// Moves kinematically (GroundSnap handles hills). Registers with
    /// <see cref="EnemyRegistry"/> so the WaveSpawner knows when the wave is clear.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class AlienEnemy : MonoBehaviour
    {
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
        private HitFlash _hitFlash;
        private float _weavePhase;   // per-alien offset so scouts don't sway in sync
        private int _flankSide;      // -1 or +1, chosen once per hunter
        private bool _smashing;      // Brute wind-up in progress: rooted in place

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (data == null) data = EnemyData.CreateDefault();
            _health.SetMaxHealth(data.maxHealth);
            _baseScale = transform.localScale;
            _hitFlash = GetComponent<HitFlash>();
            _weavePhase = Random.Range(0f, Mathf.PI * 2f);
            _flankSide = Random.value < 0.5f ? -1 : 1;
            SetupBeam();
        }

        private void OnEnable()
        {
            EnemyRegistry.Register();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister();
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

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
            if (_dead || _smashing) return; // a winding-up Brute is rooted

            if (data.role == EnemyData.EnemyRole.Hunter)
            {
                HuntPlayerFlanking();
                return;
            }

            // Rustler: cattle first, hillbilly as a last resort.
            _targetCow = HillbillyAlienShooter.Livestock.Cattle.FindNearest(transform.position);

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
                MoveTowards(target);
            else
                TryMelee(target);
        }

        /// <summary>
        /// Hunter approach: while far, aim at a point offset to the player's side
        /// (a cheap flank — pairs of hunters naturally pincer since each rolls its
        /// own side); inside flankCloseRange, drop the act and charge straight in.
        /// </summary>
        private void HuntPlayerFlanking()
        {
            SetBeam(false, transform.position);
            if (_player == null) return;

            Vector3 playerPos = _player.position;
            float dist = FlatDistance(transform.position, playerPos);

            if (dist <= data.meleeRange)
            {
                TryMelee(playerPos);
            }
            else if (dist <= data.flankCloseRange)
            {
                MoveTowards(playerPos); // committed charge
            }
            else
            {
                Vector3 toPlayer = playerPos - transform.position;
                toPlayer.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized) * _flankSide;
                MoveTowards(playerPos + side * data.flankOffset);
            }
        }

        private void TryMelee(Vector3 target)
        {
            FaceFlat(target);
            if (Time.time < _nextMeleeTime) return;
            _nextMeleeTime = Time.time + data.meleeCooldown;

            if (data.attackStyle == EnemyData.AttackStyle.Smash)
            {
                StartCoroutine(SmashRoutine());
                return;
            }

            // Swipe: instant hit.
            var dmg = _player.GetComponentInChildren<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                Vector3 dir = (target - transform.position).normalized;
                dmg.TakeDamage(new DamageInfo(data.meleeDamage, target, dir, source: gameObject));
            }
        }

        /// <summary>
        /// Brute ground slam: a readable crouch telegraph (the dodge window),
        /// then an AoE hit + shockwave ring. The Brute is rooted throughout, so
        /// clearing smashRadius during the wind-up is a guaranteed dodge.
        /// </summary>
        private IEnumerator SmashRoutine()
        {
            _smashing = true;

            // Telegraph: crouch down and bulk out.
            Vector3 crouch = Vector3.Scale(_baseScale, new Vector3(1.3f, 0.55f, 1.3f));
            float t = 0f;
            while (t < 1f && !_dead)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, data.smashWindup);
                transform.localScale = Vector3.Lerp(_baseScale, crouch, t);
                yield return null;
            }

            if (!_dead)
            {
                // SLAM. Pop back up and punish anyone still inside the ring.
                transform.localScale = _baseScale;
                HillbillyAlienShooter.Utils.LowPolyFactory.BuildShockwave(
                    transform.position, data.smashRadius, 0.45f);

                if (_player != null && FlatDistance(transform.position, _player.position) <= data.smashRadius)
                {
                    var dmg = _player.GetComponentInChildren<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                    {
                        Vector3 dir = (_player.position - transform.position).normalized;
                        dmg.TakeDamage(new DamageInfo(data.meleeDamage, _player.position, dir,
                            force: 8f, source: gameObject));
                    }
                }
            }

            _smashing = false;
        }

        private void MoveTowards(Vector3 worldTarget)
        {
            Vector3 flatTarget = new Vector3(worldTarget.x, transform.position.y, worldTarget.z);

            // "Annoying paths": sway sideways across the approach line so buckshot
            // has to be led. Amplitude/frequency come from data (0 = straight).
            if (data.weaveAmplitude > 0.01f)
            {
                Vector3 dir = flatTarget - transform.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
                    float sway = Mathf.Sin(Time.time * data.weaveFrequency * Mathf.PI * 2f + _weavePhase);
                    flatTarget += right * (sway * data.weaveAmplitude);
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, flatTarget, data.moveSpeed * Time.deltaTime);
            FaceFlat(flatTarget);
        }

        private void FaceFlat(Vector3 worldTarget)
        {
            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        // -------------------------------------------------------------------
        // Damage / death juice
        // -------------------------------------------------------------------
        private void OnDamaged(DamageInfo info)
        {
            if (_dead) return;
            _hitFlash?.Flash();

            // Don't fight the smash telegraph over the transform's scale.
            if (_smashing) return;

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

            // Roll the tech drop while the corpse is still warm.
            if (Random.value < data.techDropChance)
                HillbillyAlienShooter.Utils.LowPolyFactory.BuildTechPickup(
                    transform.position + Vector3.up * 0.6f, data.techAmount);

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
    }
}
