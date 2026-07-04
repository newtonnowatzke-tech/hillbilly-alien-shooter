namespace HillbillyAlienShooter.Combat
{
    /// <summary>
    /// Anything the shotgun (or later, explosions/melee) can hurt implements this.
    /// The weapon never needs to know whether it hit an alien, a barrel, or a
    /// future destructible fence — it just asks "are you damageable?".
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Current health &gt; 0.</summary>
        bool IsAlive { get; }

        void TakeDamage(in DamageInfo info);
    }
}
