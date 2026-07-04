using UnityEngine;

namespace HillbillyAlienShooter.Data
{
    /// <summary>
    /// Tuning data for a horse. One asset = one steed; horse cosmetics in the
    /// stretch-goal phase are just more instances of this with different colors.
    /// </summary>
    [CreateAssetMenu(menuName = "Hillbilly/Horse Data", fileName = "HorseData_Buttercup")]
    public class HorseData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Buttercup";
        [Tooltip("Coat color for the low-poly placeholder mesh.")]
        public Color bodyColor = new Color(0.55f, 0.35f, 0.20f);   // chestnut
        public Color maneColor = new Color(0.25f, 0.17f, 0.10f);   // dark brown
        public Color saddleColor = new Color(0.62f, 0.16f, 0.14f); // barn red

        [Header("Speed")]
        [Tooltip("Top speed at full gallop (m/s). Player on foot is ~6.")]
        public float maxSpeed = 12f;
        [Tooltip("Casual walking speed used when trotting after the player.")]
        public float walkSpeed = 4.5f;
        [Tooltip("Backing-up speed while ridden.")]
        public float reverseSpeed = 2f;

        [Header("Acceleration")]
        [Tooltip("How quickly the horse builds speed (m/s²).")]
        public float acceleration = 7f;
        [Tooltip("Deceleration while braking with S (m/s²).")]
        public float braking = 14f;
        [Tooltip("Natural slow-down when there's no input (m/s²).")]
        public float idleDeceleration = 5f;

        [Header("Turning")]
        [Tooltip("Yaw rate (deg/s) while standing still — horses can pivot.")]
        public float turnSpeedStanding = 140f;
        [Tooltip("Yaw rate (deg/s) at full gallop — wider arcs at speed.")]
        public float turnSpeedAtMax = 65f;

        [Header("Following")]
        [Tooltip("The horse stops following once within this distance (m).")]
        public float followDistance = 3.5f;
        [Tooltip("Beyond this distance the horse breaks into a full gallop.")]
        public float gallopDistance = 14f;
        [Tooltip("If left further behind than this, Buttercup 'knows a shortcut' and teleports near the player.")]
        public float teleportDistance = 45f;

        [Header("Physics")]
        public float gravity = -20f;

        /// <summary>Runtime fallback so a horse works with no asset assigned.</summary>
        public static HorseData CreateDefault()
        {
            var d = CreateInstance<HorseData>();
            d.name = "HorseData_DefaultHorse (runtime)";
            return d;
        }
    }
}
