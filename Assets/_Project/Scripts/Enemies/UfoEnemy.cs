using System.Collections;
using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;

namespace HillbillyAlienShooter.Enemies
{
    /// <summary>
    /// Scout saucer (Packet 2.1): a dish-shaped UFO that hovers at altitude,
    /// glides to the nearest cow, and pours an abduction beam straight down —
    /// unlike ground rustlers, it can't be body-blocked; it has to be SHOT DOWN.
    ///
    /// Behaviour:
    ///  • Cruise at hoverHeight with an ominous bob, banking slightly into turns.
    ///  • Cow available → park above it, lock the cone beam, abduct.
    ///  • No cattle → circle the farm menacingly (support fire arrives in 2.2).
    ///  • Death → spin out of the sky, crash, guaranteed-ish tech drop.
    ///
    /// Weak points + support fire are Packet 2.2; this packet establishes the
    /// airborne threat and the beam pressure.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class UfoEnemy : MonoBehaviour
    {
        [Header("Data (optional — falls back to defaults)")]
        [SerializeField] private EnemyData data;

        private Health _health;
        private LineRenderer _beam;
        private bool _dead;
        private float _bobPhase;
        private float _orbitAngle;              // idle circling when no cattle remain
        private Quaternion _baseTilt = Quaternion.identity;
        private Vector3 _lastPos;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (data == null)
            {
                data = EnemyData.CreateDefault();
                data.role = EnemyData.EnemyRole.Saucer;
            }
            _health.SetMaxHealth(data.maxHealth);
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            _orbitAngle = Random.Range(0f, Mathf.PI * 2f);
            _lastPos = transform.position;
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

        /// <summary>Assign tuning data at spawn time (used by the factory/spawner).</summary>
        public void Configure(EnemyData enemyData)
        {
            data = enemyData != null ? enemyData : EnemyData.CreateDefault();
            if (_health == null) _health = GetComponent<Health>();
            _health.SetMaxHealth(data.maxHealth);
        }

        private void Update()
        {
            if (_dead) return;

            var cow = HillbillyAlienShooter.Livestock.Cattle.FindNearest(transform.position);

            // Pick where to hover: over a cow, or on a lazy patrol circle.
            Vector3 goalXZ;
            if (cow != null)
            {
                goalXZ = cow.transform.position;
            }
            else
            {
                _orbitAngle += 0.25f * Time.deltaTime;
                goalXZ = new Vector3(Mathf.Cos(_orbitAngle), 0f, Mathf.Sin(_orbitAngle)) * 12f;
            }

            float bob = Mathf.Sin(Time.time * 1.4f + _bobPhase) * data.hoverBobAmplitude;
            Vector3 desired = new Vector3(goalXZ.x, data.hoverHeight + bob, goalXZ.z);
            transform.position = Vector3.MoveTowards(transform.position, desired, data.moveSpeed * Time.deltaTime);

            BankIntoMotion();

            // Beam only when parked close enough above the cow.
            bool beaming = false;
            if (cow != null)
            {
                Vector3 flat = transform.position - cow.transform.position;
                flat.y = 0f;
                if (flat.magnitude <= data.beamLockRadius)
                {
                    cow.Beam(data.abductRatePerSecond);
                    beaming = true;
                    SetBeam(true, cow.transform.position);
                }
            }
            if (!beaming) SetBeam(false, Vector3.zero);
        }

        /// <summary>Cosmetic: lean a few degrees into the direction of travel.</summary>
        private void BankIntoMotion()
        {
            Vector3 vel = (transform.position - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastPos = transform.position;

            Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
            Quaternion target = _baseTilt;
            if (flatVel.sqrMagnitude > 0.2f)
            {
                Vector3 dir = flatVel.normalized;
                target = Quaternion.Euler(dir.z * 8f, 0f, -dir.x * 8f);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 4f * Time.deltaTime);
        }

        // -------------------------------------------------------------------
        // Damage / death
        // -------------------------------------------------------------------
        private void OnDamaged(DamageInfo info)
        {
            if (_dead) return;
            // A quick dip + jolt so hits read clearly against the night sky.
            transform.position += Vector3.down * 0.15f + Random.insideUnitSphere * 0.05f;
        }

        private void OnDied(Health _)
        {
            if (_dead) return;
            _dead = true;
            SetBeam(false, Vector3.zero);
            GameEvents.RaiseEnemyKilled(transform.position, data.scoreValue);
            StartCoroutine(CrashRoutine());
        }

        private IEnumerator CrashRoutine()
        {
            // Spin out of the sky, drop tech at the crash site, poof.
            float fallSpeed = 0f;
            float spin = 240f;
            float elapsed = 0f;

            while (elapsed < 3f && transform.position.y > 0.4f)
            {
                elapsed += Time.deltaTime;
                fallSpeed += 14f * Time.deltaTime;
                spin += 500f * Time.deltaTime;
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
                transform.Rotate(Vector3.forward, 40f * Time.deltaTime, Space.Self);
                yield return null;
            }

            if (Random.value < data.techDropChance)
                HillbillyAlienShooter.Utils.LowPolyFactory.BuildTechPickup(
                    new Vector3(transform.position.x, 0.6f, transform.position.z), data.techAmount);

            // Shrink-poof (real explosion VFX in Packet 4.3).
            float t = 0f;
            Vector3 from = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 5f;
                transform.localScale = Vector3.Lerp(from, Vector3.zero, t);
                yield return null;
            }
            Destroy(gameObject);
        }

        // -------------------------------------------------------------------
        // Beam FX — a cone of light: narrow at the belly, wide at the ground
        // -------------------------------------------------------------------
        private void SetupBeam()
        {
            var go = new GameObject("AbductionBeam");
            go.transform.SetParent(transform, false);
            _beam = go.AddComponent<LineRenderer>();
            _beam.useWorldSpace = true;
            _beam.positionCount = 2;
            _beam.startWidth = 0.5f;   // at the saucer belly
            _beam.endWidth = 2.6f;     // flooding the cow below
            _beam.numCapVertices = 0;
            _beam.material = new Material(Shader.Find("Sprites/Default"));
            _beam.startColor = new Color(0.4f, 1f, 0.8f, 0.75f);
            _beam.endColor = new Color(0.4f, 1f, 0.8f, 0.12f);
            _beam.enabled = false;
        }

        private void SetBeam(bool on, Vector3 cowPos)
        {
            if (_beam == null) return;
            _beam.enabled = on;
            if (!on) return;
            _beam.SetPosition(0, transform.position + Vector3.down * 0.4f);
            _beam.SetPosition(1, new Vector3(cowPos.x, Mathf.Max(0.05f, cowPos.y - 0.6f), cowPos.z));
        }
    }
}
