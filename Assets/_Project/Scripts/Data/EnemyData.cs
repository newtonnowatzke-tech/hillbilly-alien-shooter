using UnityEngine;

namespace HillbillyAlienShooter.Data
{
    /// <summary>
    /// Tuning data for an alien archetype. Packet 1.1 only ships the "Little Alien"
    /// scout, but this SO is designed so Medium / Large / Brute variants (Packet
    /// 2.1 / 2.2) are just new asset instances — no new code required.
    /// </summary>
    [CreateAssetMenu(menuName = "Hillbilly/Enemy Data", fileName = "EnemyData_LittleAlien")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Little Alien";
        [Tooltip("Body tint for the low-poly placeholder mesh.")]
        public Color bodyTint = new Color(0.45f, 1f, 0.35f); // toxic alien green

        [Header("Stats")]
        public float maxHealth = 24f;
        [Tooltip("Ground movement speed (m/s).")]
        public float moveSpeed = 3.2f;

        [Header("Abduction")]
        [Tooltip("Distance at which the alien latches a beam onto a cow.")]
        public float grabRange = 2.0f;
        [Tooltip("Fraction of a cow abducted per second while beaming (1 = instant).")]
        public float abductRatePerSecond = 0.45f;

        [Header("Melee (only when no cattle remain)")]
        public float meleeRange = 1.6f;
        public float meleeDamage = 8f;
        public float meleeCooldown = 1.1f;

        [Header("Rewards")]
        public int scoreValue = 100;

        /// <summary>Runtime fallback so a spawner works with no asset assigned.</summary>
        public static EnemyData CreateDefault()
        {
            var d = CreateInstance<EnemyData>();
            d.name = "EnemyData_DefaultAlien (runtime)";
            return d;
        }
    }
}
