using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.Player;

namespace HillbillyAlienShooter.Weapons
{
    /// <summary>
    /// Hitscan double-barrel shotgun. Fires a cone of pellets from the camera,
    /// applies <see cref="DamageInfo"/> to anything <see cref="IDamageable"/>, and
    /// manages a magazine + reserve with a timed reload. Stats come from a
    /// <see cref="WeaponData"/> SO (with a code fallback so it runs unconfigured).
    ///
    /// FX are intentionally cheap for Packet 1.1: a muzzle-flash toggle and a
    /// single reused <see cref="LineRenderer"/> that draws every pellet tracer for
    /// one shot, then hides. Explosive/rapid-fire upgrades land in Packet 2.3.
    /// </summary>
    public class Shotgun : MonoBehaviour
    {
        [Header("Data (optional — falls back to sensible defaults)")]
        [SerializeField] private WeaponData weaponData;

        [Header("Scene references (auto-found if empty)")]
        [Tooltip("Aim origin & direction. Defaults to the main camera.")]
        [SerializeField] private Transform aimSource;
        [Tooltip("Where tracers/flash originate visually. Defaults to aimSource.")]
        [SerializeField] private Transform muzzle;
        [Tooltip("Optional object toggled on for a frame when firing.")]
        [SerializeField] private GameObject muzzleFlash;

        [Header("Hit detection")]
        [Tooltip("Which layers pellets can hit. Default = Everything.")]
        [SerializeField] private LayerMask hitMask = ~0;

        // Runtime ammo state.
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }

        private PlayerInputHandler _input;
        private WeaponUpgradeController _upgrades; // optional — null means no upgrade system
        private float _nextFireTime;
        private LineRenderer _tracer;
        private bool _gameActive = true;

        // Scratch collections reused across shots (no per-shot allocations).
        private readonly HashSet<IDamageable> _explosionVictims = new HashSet<IDamageable>();
        private readonly Collider[] _explosionHits = new Collider[24];

        private void Awake()
        {
            _input = GetComponentInParent<PlayerInputHandler>();
            _upgrades = GetComponentInParent<WeaponUpgradeController>();

            if (weaponData == null)
                weaponData = WeaponData.CreateDefault();

            Magazine = weaponData.magazineSize;
            Reserve = weaponData.reserveAmmo;

            SetupTracer();
        }

        private void Start()
        {
            // The player camera is created during another component's Awake, so we
            // resolve the aim source here in Start (after all Awakes have run).
            ResolveAimSource();

            // Populate the HUD on load.
            GameEvents.RaiseAmmoChanged(Magazine, Reserve);
        }

        /// <summary>
        /// Aim from the head-height camera PIVOT rather than the camera itself:
        /// they share orientation, but in third person the camera sits behind the
        /// player — raycasting from it could shoot things between the camera and
        /// the hillbilly's back. The pivot is immune to that.
        /// </summary>
        private void ResolveAimSource()
        {
            if (aimSource == null)
            {
                var pc = GetComponentInParent<PlayerController>();
                if (pc != null && pc.CameraPivot != null) aimSource = pc.CameraPivot;
                else if (Camera.main != null) aimSource = Camera.main.transform;
            }
            if (muzzle == null)
                muzzle = aimSource;
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        // No firing from the pause menu / end screens (a Resume click must not
        // discharge the shotgun into whatever's behind the button).
        private void OnGameStateChanged(GameState state) => _gameActive = state == GameState.Playing;

        private void Update()
        {
            if (!_gameActive || _input == null) return;

            if (_input.ReloadPressedThisFrame)
                TryReload();

            if (_input.FirePressedThisFrame)
                TryFire();
        }

        // -------------------------------------------------------------------
        // Firing
        // -------------------------------------------------------------------
        private void TryFire()
        {
            if (IsReloading || Time.time < _nextFireTime) return;

            if (Magazine <= 0)
            {
                // Auto-reload on an empty click (feels good, avoids dead trigger).
                TryReload();
                return;
            }

            // Rapid Fire upgrade shrinks the cooldown live.
            float cooldown = weaponData.fireCooldown * (_upgrades != null ? _upgrades.FireCooldownMultiplier : 1f);
            _nextFireTime = Time.time + cooldown;
            Magazine--;

            FirePellets();

            GameEvents.RaiseWeaponFired();
            GameEvents.RaiseAmmoChanged(Magazine, Reserve);

            if (muzzleFlash != null)
                StartCoroutine(FlashRoutine());
        }

        private void FirePellets()
        {
            if (aimSource == null) ResolveAimSource();
            if (aimSource == null) return;

            Vector3 origin = aimSource.position;
            Vector3 forward = aimSource.forward;
            int pellets = Mathf.Max(1, weaponData.pelletsPerShot);

            _tracer.positionCount = pellets * 2;

            // Boomstick Rounds: pellet impacts detonate. We gather hit points and
            // detonate ONCE per shot at their centroid, hitting each victim once —
            // eight pellet-explosions per pull would be pure chaos (and unfair).
            float blastRadius = _upgrades != null ? _upgrades.ExplosiveRadius : 0f;
            Vector3 hitCentroid = Vector3.zero;
            int hitCount = 0;

            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ApplyConeSpread(forward, weaponData.spreadAngle);
                Vector3 endPoint = origin + dir * weaponData.range;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, weaponData.range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    endPoint = hit.point;
                    hitCentroid += hit.point;
                    hitCount++;

                    // Look for a damageable on the collider or its parents.
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null && damageable.IsAlive)
                    {
                        var info = new DamageInfo(
                            amount: weaponData.damagePerPellet,
                            hitPoint: hit.point,
                            hitDirection: dir,
                            hitNormal: hit.normal,
                            force: weaponData.impactForce,
                            source: gameObject);
                        damageable.TakeDamage(info);
                    }
                }

                // Tracer goes from the visible muzzle to where the pellet landed.
                _tracer.SetPosition(i * 2, muzzle != null ? muzzle.position : origin);
                _tracer.SetPosition(i * 2 + 1, endPoint);
            }

            if (blastRadius > 0f && hitCount > 0)
                Explode(hitCentroid / hitCount, blastRadius, _upgrades.ExplosiveDamage);

            StartCoroutine(TracerRoutine());
        }

        /// <summary>
        /// Boomstick Rounds detonation: AoE damage around the shot's impact
        /// centroid. Hits each enemy once per shot; the player, horse and cattle
        /// are immune (this is an upgrade, not a foot-gun).
        /// </summary>
        private void Explode(Vector3 center, float radius, float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _explosionHits, ~0, QueryTriggerInteraction.Ignore);
            _explosionVictims.Clear();

            for (int i = 0; i < count; i++)
            {
                var col = _explosionHits[i];
                if (col == null) continue;
                if (col.transform.root == transform.root) continue; // never ourselves (or our horse while mounted)
                if (col.GetComponentInParent<HillbillyAlienShooter.Player.PlayerController>() != null) continue;
                if (col.GetComponentInParent<HillbillyAlienShooter.Horse.HorseController>() != null) continue;

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;
                if (!_explosionVictims.Add(damageable)) continue; // once per shot

                Vector3 dir = (col.transform.position - center).normalized;
                damageable.TakeDamage(new DamageInfo(damage, center, dir, force: 4f, source: gameObject));
            }

            // Reuse the slam ring as the blast marker (proper VFX in 4.3).
            HillbillyAlienShooter.Utils.LowPolyFactory.BuildShockwave(center, radius, 0.3f);
        }

        /// <summary>Top up reserve shells (Extra Shells upgrade).</summary>
        public void AddReserve(int shells)
        {
            if (shells <= 0) return;
            Reserve += shells;
            GameEvents.RaiseAmmoChanged(Magazine, Reserve);
        }

        /// <summary>Returns <paramref name="forward"/> rotated by a random offset within a cone.</summary>
        private static Vector3 ApplyConeSpread(Vector3 forward, float halfAngleDeg)
        {
            if (halfAngleDeg <= 0f) return forward;
            // Random point in a disc → tilt the forward vector by up to halfAngle.
            float angle = Random.Range(0f, halfAngleDeg);
            float spin = Random.Range(0f, 360f);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up) * Quaternion.AngleAxis(spin, Vector3.forward);
            // Build a basis around forward and apply the tilt.
            Quaternion look = Quaternion.LookRotation(forward);
            return (look * rot * Vector3.forward).normalized;
        }

        // -------------------------------------------------------------------
        // Reloading
        // -------------------------------------------------------------------
        public void TryReload()
        {
            if (IsReloading) return;
            if (Magazine >= weaponData.magazineSize) return;
            if (Reserve <= 0) return;

            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            GameEvents.RaiseReloadStateChanged(true);

            // Greased Lightnin' upgrade shortens this live.
            float reloadTime = weaponData.reloadTime * (_upgrades != null ? _upgrades.ReloadTimeMultiplier : 1f);
            yield return new WaitForSeconds(reloadTime);

            int needed = weaponData.magazineSize - Magazine;
            int toLoad = Mathf.Min(needed, Reserve);
            Magazine += toLoad;
            Reserve -= toLoad;

            IsReloading = false;
            GameEvents.RaiseReloadStateChanged(false);
            GameEvents.RaiseAmmoChanged(Magazine, Reserve);
        }

        // -------------------------------------------------------------------
        // FX helpers
        // -------------------------------------------------------------------
        private void SetupTracer()
        {
            var go = new GameObject("ShotgunTracer");
            go.transform.SetParent(transform, false);
            _tracer = go.AddComponent<LineRenderer>();
            _tracer.useWorldSpace = true;
            _tracer.widthMultiplier = 0.03f;
            _tracer.numCapVertices = 0;
            _tracer.textureMode = LineTextureMode.Stretch;
            _tracer.material = new Material(Shader.Find("Sprites/Default"));
            _tracer.startColor = new Color(1f, 0.95f, 0.5f, 0.9f); // warm buckshot streak
            _tracer.endColor = new Color(1f, 0.7f, 0.2f, 0f);
            _tracer.positionCount = 0;
            _tracer.enabled = false;
        }

        private IEnumerator TracerRoutine()
        {
            _tracer.enabled = true;
            yield return new WaitForSeconds(0.045f);
            _tracer.enabled = false;
        }

        private IEnumerator FlashRoutine()
        {
            muzzleFlash.SetActive(true);
            yield return new WaitForSeconds(0.04f);
            muzzleFlash.SetActive(false);
        }
    }
}
