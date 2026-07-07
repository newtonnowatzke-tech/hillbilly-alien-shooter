using UnityEngine;

namespace HillbillyAlienShooter.Combat
{
    /// <summary>
    /// A bonus-damage hitbox (Packet 2.2: the saucer's glowing dome). Lives on a
    /// child collider and forwards amplified damage to the owner's <see cref="Health"/>.
    /// Because the shotgun resolves <see cref="IDamageable"/> starting from the
    /// collider it hit, a pellet on this child finds the WeakPoint before the
    /// parent's Health — no weapon changes needed.
    /// </summary>
    public class WeakPoint : MonoBehaviour, IDamageable
    {
        [Tooltip("Damage multiplier applied to hits on this collider.")]
        [SerializeField] private float multiplier = 2.5f;
        [SerializeField] private Health target;

        public bool IsAlive => target != null && target.IsAlive;

        public void Configure(Health owner, float damageMultiplier)
        {
            target = owner;
            multiplier = Mathf.Max(1f, damageMultiplier);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (target == null) return;
            var amplified = new DamageInfo(
                info.Amount * multiplier,
                info.HitPoint,
                info.HitDirection,
                info.HitNormal,
                info.Force,
                info.Source);
            target.TakeDamage(amplified);
        }
    }
}
