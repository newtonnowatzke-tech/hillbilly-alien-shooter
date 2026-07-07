using System.Collections.Generic;
using UnityEngine;
using HillbillyAlienShooter.Core;
using HillbillyAlienShooter.Data;
using HillbillyAlienShooter.Player;

namespace HillbillyAlienShooter.Weapons
{
    /// <summary>
    /// The jury-rig bench (Packet 2.3). Press Q to spend alien tech on a WILD
    /// upgrade — a weighted random roll from the pool. Timed upgrades live here
    /// as stacking, refreshing buffs; the <see cref="Shotgun"/> queries the
    /// current multipliers each time it fires or reloads, so upgrades apply and
    /// expire live without touching WeaponData assets.
    ///
    /// Stacking rules: re-rolling an active upgrade adds its full duration and
    /// +1 stack (capped). Multiplier effects stay flat per stack (only the clock
    /// grows); explosive blast radius grows +25% per stack.
    /// </summary>
    public class WeaponUpgradeController : MonoBehaviour
    {
        [Header("Wild pool (falls back to a built-in pool if empty)")]
        [SerializeField] private List<UpgradeData> upgradePool = new List<UpgradeData>();

        [Header("Economy")]
        [Tooltip("Tech cost of one wild roll.")]
        [SerializeField] private int upgradeCost = 5;

        private class ActiveUpgrade
        {
            public UpgradeData Data;
            public float Remaining;
            public int Stacks;
        }

        private readonly List<ActiveUpgrade> _active = new List<ActiveUpgrade>();
        private PlayerInputHandler _input;
        private Shotgun _shotgun;
        private bool _gameActive = true;

        // ---------------------------------------------------------------
        // Multiplier surface consumed by Shotgun
        // ---------------------------------------------------------------
        public float FireCooldownMultiplier => MultiplierFor(UpgradeData.UpgradeType.RapidFire);
        public float ReloadTimeMultiplier => MultiplierFor(UpgradeData.UpgradeType.FastReload);

        /// <summary>0 when explosive rounds are inactive; blast radius otherwise (+25%/stack).</summary>
        public float ExplosiveRadius
        {
            get
            {
                var a = Find(UpgradeData.UpgradeType.ExplosiveShells);
                return a == null ? 0f : a.Data.amount * (1f + 0.25f * (a.Stacks - 1));
            }
        }

        public float ExplosiveDamage
        {
            get
            {
                var a = Find(UpgradeData.UpgradeType.ExplosiveShells);
                return a == null ? 0f : a.Data.explosionDamage;
            }
        }

        // ---------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _shotgun = GetComponent<Shotgun>();
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;
        private void OnGameStateChanged(GameState state) => _gameActive = state == GameState.Playing;

        private void Start()
        {
            GameEvents.RaiseUpgradeToast($"[Q] jury-rig a wild upgrade — {upgradeCost} tech a roll");
        }

        private void Update()
        {
            TickTimers();

            if (_gameActive && _input != null && _input.UpgradePressedThisFrame)
                TryRollUpgrade();
        }

        private void TickTimers()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Remaining -= Time.deltaTime;
                if (_active[i].Remaining <= 0f)
                {
                    GameEvents.RaiseUpgradeExpired(_active[i].Data.displayName);
                    _active.RemoveAt(i);
                }
            }
        }

        // ---------------------------------------------------------------
        // Rolling & applying
        // ---------------------------------------------------------------
        public void TryRollUpgrade()
        {
            if (upgradePool == null || upgradePool.Count == 0)
                upgradePool = new List<UpgradeData>(UpgradeData.CreateDefaultPool());

            if (!TechInventory.TrySpend(upgradeCost))
            {
                GameEvents.RaiseUpgradeToast($"Need {upgradeCost} tech to jury-rig! (got {TechInventory.Count})");
                return;
            }

            Apply(RollFromPool());
        }

        private UpgradeData RollFromPool()
        {
            float total = 0f;
            foreach (var u in upgradePool)
                if (u != null) total += Mathf.Max(0.01f, u.weight);

            float pick = Random.Range(0f, total);
            foreach (var u in upgradePool)
            {
                if (u == null) continue;
                pick -= Mathf.Max(0.01f, u.weight);
                if (pick <= 0f) return u;
            }
            return upgradePool[upgradePool.Count - 1];
        }

        private void Apply(UpgradeData upgrade)
        {
            if (upgrade == null) return;

            switch (upgrade.type)
            {
                case UpgradeData.UpgradeType.ExtraAmmo:
                    _shotgun?.AddReserve(Mathf.RoundToInt(upgrade.amount));
                    GameEvents.RaiseUpgradeToast($"{upgrade.displayName}! {upgrade.flavor}");
                    return;

                case UpgradeData.UpgradeType.DurationExtender:
                    if (_active.Count == 0)
                    {
                        // The gamble half of gambling: nothing to extend.
                        GameEvents.RaiseUpgradeToast($"{upgrade.displayName}... nothin' runnin' to extend!");
                        return;
                    }
                    foreach (var a in _active)
                    {
                        a.Remaining += upgrade.amount;
                        GameEvents.RaiseUpgradeChanged(a.Data.displayName, a.Remaining, a.Stacks);
                    }
                    GameEvents.RaiseUpgradeToast($"{upgrade.displayName}! {upgrade.flavor}");
                    return;

                default: // timed buffs
                    var existing = Find(upgrade.type);
                    if (existing != null)
                    {
                        existing.Stacks = Mathf.Min(existing.Stacks + 1, Mathf.Max(1, upgrade.maxStacks));
                        existing.Remaining += upgrade.duration; // stack = longer AND (sometimes) stronger
                        GameEvents.RaiseUpgradeChanged(existing.Data.displayName, existing.Remaining, existing.Stacks);
                        GameEvents.RaiseUpgradeToast($"{upgrade.displayName} ×{existing.Stacks}! {upgrade.flavor}");
                    }
                    else
                    {
                        var fresh = new ActiveUpgrade { Data = upgrade, Remaining = upgrade.duration, Stacks = 1 };
                        _active.Add(fresh);
                        GameEvents.RaiseUpgradeChanged(upgrade.displayName, fresh.Remaining, 1);
                        GameEvents.RaiseUpgradeToast($"{upgrade.displayName}! {upgrade.flavor}");
                    }
                    return;
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private ActiveUpgrade Find(UpgradeData.UpgradeType type)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Data.type == type) return _active[i];
            return null;
        }

        private float MultiplierFor(UpgradeData.UpgradeType type)
        {
            var a = Find(type);
            return a == null ? 1f : Mathf.Max(0.05f, a.Data.amount);
        }
    }
}
