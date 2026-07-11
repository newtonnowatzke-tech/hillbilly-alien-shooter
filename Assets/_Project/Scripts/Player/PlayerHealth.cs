using UnityEngine;
using HillbillyAlienShooter.Combat;
using HillbillyAlienShooter.Core;

namespace HillbillyAlienShooter.Player
{
    /// <summary>
    /// Bridges the reusable <see cref="Health"/> component to the game-wide event
    /// bus for the player specifically: broadcasts HP for the HUD and fires
    /// PlayerDied so the GameManager can trigger a loss.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("HP patched up during each between-wave breather (Packet 3.1).")]
        [SerializeField] private float healPerWaveClear = 20f;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            GameEvents.WaveCompleted += OnWaveCompleted;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            GameEvents.WaveCompleted -= OnWaveCompleted;
        }

        /// <summary>Rest-period patch-up: bandages between waves, not during them.</summary>
        private void OnWaveCompleted(int wave, int total)
        {
            if (!_health.IsAlive || healPerWaveClear <= 0f) return;
            _health.Heal(healPerWaveClear);
            GameEvents.RaisePlayerHealthChanged(_health.Current, _health.Max);
        }

        private void Start()
        {
            // Push the initial value so the HUD populates on load.
            GameEvents.RaisePlayerHealthChanged(_health.Current, _health.Max);
        }

        private void OnDamaged(DamageInfo info)
        {
            GameEvents.RaisePlayerHealthChanged(_health.Current, _health.Max);
        }

        private void OnDied(Health _)
        {
            GameEvents.RaisePlayerHealthChanged(0f, _health.Max);
            GameEvents.RaisePlayerDied();
        }
    }
}
