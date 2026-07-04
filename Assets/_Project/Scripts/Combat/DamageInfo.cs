using UnityEngine;

namespace HillbillyAlienShooter.Combat
{
    /// <summary>
    /// Immutable bundle of context passed with every hit. Using a struct (rather
    /// than a bare float) means we can add knockback, hit FX, headshots, and
    /// damage-type reactions later WITHOUT changing the <see cref="IDamageable"/>
    /// signature again.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly Vector3 HitDirection; // direction the projectile/pellet travelled
        public readonly float Force;          // impulse for knockback / squash-stretch
        public readonly GameObject Source;    // who dealt the damage (may be null)

        public DamageInfo(
            float amount,
            Vector3 hitPoint = default,
            Vector3 hitDirection = default,
            Vector3 hitNormal = default,
            float force = 0f,
            GameObject source = null)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            HitNormal = hitNormal;
            Force = force;
            Source = source;
        }
    }
}
