using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Sends shockwaves along the ground that have to be jumped over.
    /// </summary>
    /// <remarks>
    /// The only attack answered with the jump button rather than by walking, which is what stops
    /// the fight becoming a purely horizontal dodging exercise. It also interacts with the
    /// platforms: standing on one is a valid answer, at the cost of manoeuvring room.
    /// </remarks>
    [CreateAssetMenu(fileName = "Slam", menuName = "Boss Level/Attacks/Slam")]
    public class SlamAttack : BossAttack
    {
        [Tooltip("Height of the shockwave relative to the boss's own origin. Should sit at the " +
                 "arena floor.")]
        [SerializeField] private float groundOffset = -1.5f;

        [SerializeField, Min(1)] private int waveCount = 2;

        [Tooltip("Gap between waves. Long enough to land from the first jump, short enough that " +
                 "the second still demands one.")]
        [SerializeField, Min(0f)] private float delayBetweenWaves = 0.55f;

        [Tooltip("How far above the wave the player can be and still be considered to be on the " +
                 "same floor. Standing on a platform puts them above it, where it slides past " +
                 "harmlessly.")]
        [SerializeField, Min(0f)] private float sameFloorTolerance = 1.5f;

        public override IEnumerator Execute(BossContext context)
        {
            // The boss is anchored on the right, so its shockwaves always travel left into the
            // player's half of the arena.
            var groundY = context.BossPosition.y + groundOffset;
            var origin = new Vector2(context.BossPosition.x, groundY);

            for (var i = 0; i < waveCount; i++)
            {
                context.FireFrom(origin, Vector2.left);

                if (i < waveCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenWaves);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // A wave along the floor is free to ignore if the player is already in the air.
            if (!context.TargetIsGrounded)
            {
                return 0.05f;
            }

            // Standing on a platform counts as grounded but is not on the wave's floor — it
            // would slide past underneath. Checking the height stops the boss confusing "has
            // their feet down" with "is somewhere this can reach".
            var waveHeight = context.BossPosition.y + groundOffset;
            var heightAboveWave = context.TargetPosition.y - waveHeight;

            return heightAboveWave <= sameFloorTolerance ? 1f : 0.1f;
        }
    }
}
