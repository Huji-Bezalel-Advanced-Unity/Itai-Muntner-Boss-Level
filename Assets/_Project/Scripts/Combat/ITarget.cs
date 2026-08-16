using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// What an attacker needs to know about the thing it is shooting at.
    /// </summary>
    /// <remarks>
    /// The boss reads this rather than referencing the player directly, which keeps the boss
    /// independent of how the player happens to be built. It is also what makes a smarter boss
    /// possible at all: aiming at a position alone can only ever hit someone standing still,
    /// whereas velocity allows leading the shot and grounding allows judging whether a
    /// ground-based attack is worth using.
    /// </remarks>
    public interface ITarget
    {
        Vector2 Position { get; }

        /// <summary>Current velocity, used to lead shots instead of aiming where the target was.</summary>
        Vector2 Velocity { get; }

        /// <summary>Whether the target is standing on something.</summary>
        bool IsGrounded { get; }
    }
}
