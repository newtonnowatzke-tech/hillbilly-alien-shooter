using System;
using UnityEngine;

namespace HillbillyAlienShooter.Combat
{
    /// <summary>
    /// Reusable hit-points component. Shared by the player and every enemy so we
    /// never re-implement damage/death logic. Exposes C# events for local
    /// listeners (e.g. an enemy plays a squash animation on Damaged and despawns
    /// on Died); higher-level/global concerns route through <c>GameEvents</c>.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("Starting and maximum hit points.")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Brief invulnerability after taking a hit (seconds). 0 = none.")]
        [SerializeField] private float invulnerabilityTime = 0f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsAlive => Current > 0f;

        /// <summary>Raised on any non-fatal or fatal damage. Passes the hit context.</summary>
        public event Action<DamageInfo> Damaged;
        /// <summary>Raised exactly once, when health first reaches zero.</summary>
        public event Action<Health> Died;

        private float _lastHitTime = -999f;

        private void Awake() => Current = maxHealth;

        /// <summary>Configure max health from data (e.g. an EnemyData SO) at spawn time.</summary>
        public void SetMaxHealth(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill) Current = maxHealth;
            else Current = Mathf.Min(Current, maxHealth);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return;
            if (Time.time - _lastHitTime < invulnerabilityTime) return;

            _lastHitTime = Time.time;
            Current = Mathf.Max(0f, Current - Mathf.Abs(info.Amount));
            Damaged?.Invoke(info);

            if (Current <= 0f)
                Died?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Current = Mathf.Min(maxHealth, Current + Mathf.Abs(amount));
        }

        /// <summary>Restore to full (used on respawn / scene restart).</summary>
        public void ResetHealth() => Current = maxHealth;
    }
}
