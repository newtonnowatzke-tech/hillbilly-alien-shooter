using UnityEngine;

namespace HillbillyAlienShooter.Data
{
    /// <summary>
    /// One alien-tech shotgun upgrade (Packet 2.3). Instant upgrades (duration 0)
    /// fire once; timed upgrades run on a clock and STACK when re-rolled — each
    /// stack refreshes and extends the timer, and some effects scale per stack.
    /// The "wild pool" the player rolls from is just a list of these assets, so
    /// new upgrades are new data files.
    /// </summary>
    [CreateAssetMenu(menuName = "Hillbilly/Upgrade Data", fileName = "UpgradeData_New")]
    public class UpgradeData : ScriptableObject
    {
        public enum UpgradeType
        {
            ExtraAmmo,        // instant: +amount reserve shells
            FastReload,       // timed:   reload time × amount (e.g. 0.45)
            ExplosiveShells,  // timed:   pellet hits detonate (radius = amount)
            RapidFire,        // timed:   fire cooldown × amount (e.g. 0.45)
            DurationExtender  // instant: +amount seconds to every running upgrade
        }

        [Header("Identity")]
        public string displayName = "Mystery Gizmo";
        [Tooltip("Hillbilly one-liner shown in the pickup toast.")]
        public string flavor = "Ain't got a clue what this does!";
        public UpgradeType type = UpgradeType.ExtraAmmo;

        [Header("Effect")]
        [Tooltip("Meaning depends on type: shells added / time multiplier / blast radius / seconds added.")]
        public float amount = 12f;
        [Tooltip("Extra damage dealt by each explosion (ExplosiveShells only).")]
        public float explosionDamage = 10f;
        [Tooltip("Seconds the upgrade lasts. 0 = instant effect.")]
        public float duration = 0f;
        [Tooltip("Maximum times this can stack while active.")]
        public int maxStacks = 3;

        [Header("Wild pool")]
        [Tooltip("Relative chance of being rolled from the pool.")]
        public float weight = 1f;

        public bool IsInstant => duration <= 0f;

        /// <summary>Runtime fallback pool so the system works with no assets wired.</summary>
        public static UpgradeData[] CreateDefaultPool()
        {
            return new[]
            {
                Make("Extra Shells", "Found a box o' shells in the truck!", UpgradeType.ExtraAmmo, 12f, 0f),
                Make("Greased Lightnin'", "Slicker'n a greased pig!", UpgradeType.FastReload, 0.45f, 20f),
                Make("Boomstick Rounds", "Now THAT'S a boomstick!", UpgradeType.ExplosiveShells, 2.2f, 15f),
                Make("Hair Trigger", "Faster'n gossip at church!", UpgradeType.RapidFire, 0.45f, 15f),
                Make("Moonshine Timer", "Time moves slower after moonshine.", UpgradeType.DurationExtender, 8f, 0f),
            };
        }

        private static UpgradeData Make(string name, string flavorText, UpgradeType t, float amt, float dur)
        {
            var u = CreateInstance<UpgradeData>();
            u.name = $"UpgradeData_{name} (runtime)";
            u.displayName = name;
            u.flavor = flavorText;
            u.type = t;
            u.amount = amt;
            u.duration = dur;
            return u;
        }
    }
}
