using UnityEngine;

namespace HillbillyAlienShooter.Data
{
    /// <summary>
    /// Tuning data for a hitscan shotgun. Kept in a ScriptableObject so designers
    /// can balance the weapon without touching code, and so the upgrade system in
    /// Packet 2.3 can swap/modify these values at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Hillbilly/Weapon Data", fileName = "WeaponData_Shotgun")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Ol' Reliable";

        [Header("Damage")]
        [Tooltip("Damage dealt by a single pellet.")]
        public float damagePerPellet = 12f;
        [Tooltip("Pellets fired per trigger pull.")]
        public int pelletsPerShot = 8;
        [Tooltip("Half-angle of the shot cone in degrees. Bigger = wider spread.")]
        public float spreadAngle = 6f;
        [Tooltip("Max hitscan distance in metres.")]
        public float range = 45f;
        [Tooltip("Knockback impulse applied to what we hit.")]
        public float impactForce = 6f;

        [Header("Ammo & Timing")]
        public int magazineSize = 6;
        public int reserveAmmo = 30;
        [Tooltip("Minimum seconds between shots.")]
        public float fireCooldown = 0.65f;
        [Tooltip("Seconds to reload a full magazine.")]
        public float reloadTime = 1.7f;

        /// <summary>Runtime fallback so the weapon still works with no asset assigned.</summary>
        public static WeaponData CreateDefault()
        {
            // Default field values above already describe a fun starter shotgun.
            var d = CreateInstance<WeaponData>();
            d.name = "WeaponData_DefaultShotgun (runtime)";
            return d;
        }
    }
}
