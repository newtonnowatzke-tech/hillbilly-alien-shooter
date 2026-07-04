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
        /// <summary>
        /// What this enemy fundamentally does:
        ///  Rustler — goes for cattle, beams them up from the ground (Little Alien).
        ///  Hunter  — hunts the player with a flanking approach (Medium Alien).
        ///  Saucer  — airborne UFO that hovers above cattle and beams them skyward.
        /// </summary>
        public enum EnemyRole { Rustler, Hunter, Saucer }

        [Header("Identity")]
        public string displayName = "Little Alien";
        public EnemyRole role = EnemyRole.Rustler;
        [Tooltip("Body tint for the low-poly placeholder mesh.")]
        public Color bodyTint = new Color(0.45f, 1f, 0.35f); // toxic alien green
        [Tooltip("Uniform scale multiplier for the placeholder body (Medium ≈ 1.3).")]
        public float bodyScale = 1f;

        [Header("Stats")]
        public float maxHealth = 24f;
        [Tooltip("Ground movement speed (m/s). Horizontal drift speed for Saucers.")]
        public float moveSpeed = 3.2f;

        [Header("Weave — 'annoying paths' (0 = walk straight)")]
        [Tooltip("Lateral sway distance in metres while approaching a target.")]
        public float weaveAmplitude = 0f;
        [Tooltip("Sway cycles per second.")]
        public float weaveFrequency = 0f;

        [Header("Hunter flanking (role = Hunter)")]
        [Tooltip("How far to the player's side the approach curves (m).")]
        public float flankOffset = 5f;
        [Tooltip("Within this distance of the player, drop the flank and charge straight in.")]
        public float flankCloseRange = 4.5f;

        [Header("Saucer hover (role = Saucer)")]
        [Tooltip("Cruise altitude above the ground (m).")]
        public float hoverHeight = 9f;
        [Tooltip("Vertical bob amplitude for that ominous UFO float.")]
        public float hoverBobAmplitude = 0.4f;
        [Tooltip("Horizontal distance to a cow within which the beam locks on.")]
        public float beamLockRadius = 1.6f;

        [Header("Tech drops")]
        [Range(0f, 1f)]
        [Tooltip("Probability of dropping alien tech on death.")]
        public float techDropChance = 0.25f;
        [Tooltip("How much tech a drop is worth.")]
        public int techAmount = 1;

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
