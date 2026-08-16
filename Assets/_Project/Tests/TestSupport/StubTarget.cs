using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.TestSupport
{
    /// <summary>
    /// An <see cref="ITarget"/> whose values can simply be set, so a test can pose a situation —
    /// airborne, sprinting, standing still — without building a player.
    /// </summary>
    public class StubTarget : ITarget
    {
        public Vector2 Position { get; set; }

        public Vector2 Velocity { get; set; }

        public bool IsGrounded { get; set; } = true;
    }
}
