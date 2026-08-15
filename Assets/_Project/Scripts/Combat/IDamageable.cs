namespace BossLevel.Combat
{
    /// <summary>
    /// Anything a projectile or hitbox can damage.
    /// </summary>
    /// <remarks>
    /// Attackers depend on this interface rather than on <see cref="Health"/> directly, so a
    /// shield, a breakable prop, or a boss part can take hits without each needing a full
    /// health component — and so a projectile never has to ask what it just hit.
    /// </remarks>
    public interface IDamageable
    {
        /// <summary>False once this target has been killed. Attackers use it to skip dead targets.</summary>
        bool IsAlive { get; }

        /// <summary>Applies damage. Implementations are expected to ignore non-positive amounts.</summary>
        void TakeDamage(int amount);
    }
}
