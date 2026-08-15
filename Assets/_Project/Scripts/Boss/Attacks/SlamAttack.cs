using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Sends shockwaves along the ground that the player has to jump over.
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
    }
}
