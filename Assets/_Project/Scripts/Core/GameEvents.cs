using System;
using UnityEngine;

namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// Static, engine-light event bus that decouples gameplay systems from the
    /// UI and from each other. Systems RAISE events; listeners (HUD, GameManager,
    /// audio, etc.) SUBSCRIBE. Payloads are deliberately limited to primitives,
    /// Unity structs, and <see cref="GameObject"/> so this class stays free of
    /// dependencies on any specific gameplay type — that keeps the dependency
    /// graph clean and prevents circular references.
    ///
    /// Convention: subscribe in OnEnable, unsubscribe in OnDisable. Because
    /// Awake/OnEnable always run before any Start, a listener that subscribes in
    /// OnEnable will still catch the "initial state" broadcasts that systems fire
    /// in their Start().
    /// </summary>
    public static class GameEvents
    {
        // ---------------------------------------------------------------
        // Game flow
        // ---------------------------------------------------------------
        public static event Action<GameState> GameStateChanged;
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);

        // ---------------------------------------------------------------
        // Waves
        // ---------------------------------------------------------------
        /// <summary>Fired when a wave begins. Arg: 1-based wave number.</summary>
        public static event Action<int> WaveStarted;
        public static void RaiseWaveStarted(int waveNumber) => WaveStarted?.Invoke(waveNumber);

        /// <summary>Fired when every enemy in a wave is gone. Arg: 1-based wave number.</summary>
        public static event Action<int> WaveCompleted;
        public static void RaiseWaveCompleted(int waveNumber) => WaveCompleted?.Invoke(waveNumber);

        /// <summary>Live count of enemies currently alive in the scene.</summary>
        public static event Action<int> EnemyCountChanged;
        public static void RaiseEnemyCountChanged(int aliveCount) => EnemyCountChanged?.Invoke(aliveCount);

        /// <summary>Fired when an enemy is killed by the player. Args: world position, score value.</summary>
        public static event Action<Vector3, int> EnemyKilled;
        public static void RaiseEnemyKilled(Vector3 position, int scoreValue) => EnemyKilled?.Invoke(position, scoreValue);

        /// <summary>Running alien-tech total changed (pickups collected / tech spent). Arg: new total.</summary>
        public static event Action<int> TechChanged;
        public static void RaiseTechChanged(int total) => TechChanged?.Invoke(total);

        // ---------------------------------------------------------------
        // Upgrades (Packet 2.3)
        // ---------------------------------------------------------------
        /// <summary>Short centre-screen message (upgrade acquired, not enough tech, ...).</summary>
        public static event Action<string> UpgradeToast;
        public static void RaiseUpgradeToast(string message) => UpgradeToast?.Invoke(message);

        /// <summary>
        /// A timed upgrade was activated, stacked, or extended.
        /// Args: (display name, seconds remaining, stack count). The HUD counts the
        /// remaining time down locally between these events.
        /// </summary>
        public static event Action<string, float, int> UpgradeChanged;
        public static void RaiseUpgradeChanged(string name, float remaining, int stacks) => UpgradeChanged?.Invoke(name, remaining, stacks);

        /// <summary>A timed upgrade ran out. Arg: display name.</summary>
        public static event Action<string> UpgradeExpired;
        public static void RaiseUpgradeExpired(string name) => UpgradeExpired?.Invoke(name);

        // ---------------------------------------------------------------
        // Cattle
        // ---------------------------------------------------------------
        /// <summary>Fired whenever cattle tallies change. Args: (saved/alive, taken, total).</summary>
        public static event Action<int, int, int> CattleCountsChanged;
        public static void RaiseCattleCountsChanged(int saved, int taken, int total) => CattleCountsChanged?.Invoke(saved, taken, total);

        // ---------------------------------------------------------------
        // Player
        // ---------------------------------------------------------------
        /// <summary>Args: (current, max) health.</summary>
        public static event Action<float, float> PlayerHealthChanged;
        public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);

        public static event Action PlayerDied;
        public static void RaisePlayerDied() => PlayerDied?.Invoke();

        // ---------------------------------------------------------------
        // Interaction & horse
        // ---------------------------------------------------------------
        /// <summary>
        /// The best available interaction prompt near the player, or null/empty
        /// when there is nothing to interact with (HUD hides the prompt).
        /// </summary>
        public static event Action<string> InteractPromptChanged;
        public static void RaiseInteractPromptChanged(string prompt) => InteractPromptChanged?.Invoke(prompt);

        /// <summary>Human-readable status of the horse for the HUD (e.g. "Buttercup: followin' you").</summary>
        public static event Action<string> HorseStateChanged;
        public static void RaiseHorseStateChanged(string status) => HorseStateChanged?.Invoke(status);

        // ---------------------------------------------------------------
        // Weapon
        // ---------------------------------------------------------------
        public static event Action WeaponFired;
        public static void RaiseWeaponFired() => WeaponFired?.Invoke();

        /// <summary>Args: (rounds in magazine, rounds in reserve).</summary>
        public static event Action<int, int> AmmoChanged;
        public static void RaiseAmmoChanged(int magazine, int reserve) => AmmoChanged?.Invoke(magazine, reserve);

        /// <summary>Arg: true when a reload begins, false when it finishes.</summary>
        public static event Action<bool> ReloadStateChanged;
        public static void RaiseReloadStateChanged(bool isReloading) => ReloadStateChanged?.Invoke(isReloading);

        /// <summary>
        /// Clears every subscription. Call on hard scene resets to guarantee no
        /// stale delegates survive a domain reload with "Enter Play Mode Options"
        /// (fast play mode) enabled.
        /// </summary>
        public static void ResetAll()
        {
            GameStateChanged = null;
            WaveStarted = null;
            WaveCompleted = null;
            EnemyCountChanged = null;
            EnemyKilled = null;
            TechChanged = null;
            UpgradeToast = null;
            UpgradeChanged = null;
            UpgradeExpired = null;
            CattleCountsChanged = null;
            PlayerHealthChanged = null;
            PlayerDied = null;
            InteractPromptChanged = null;
            HorseStateChanged = null;
            WeaponFired = null;
            AmmoChanged = null;
            ReloadStateChanged = null;
        }
    }
}
