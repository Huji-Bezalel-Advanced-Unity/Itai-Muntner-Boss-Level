using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Fires a fan of projectiles at the player, all at once.
    /// </summary>
    /// <remarks>
    /// The bread-and-butter attack. It covers ground rather than picking a spot, so it is the
    /// answer to a player who is already moving: the fan goes where they are heading as well as
    /// where they are. Widening the arc while adding shots keeps the gaps the same size;
    /// widening it without adding shots opens them up.
    /// </remarks>
    [CreateAssetMenu(fileName = "SpreadShot", menuName = "Boss Level/Attacks/Spread Shot")]
    public class SpreadShotAttack : BossAttack
    {
        [SerializeField, Min(1)] private int bulletCount = 5;

        [Tooltip("Total width of the fan. Narrower is deadlier — the shots stay together at the " +
                 "distance they actually arrive at.")]
        [SerializeField, Range(0f, 360f)] private float arcDegrees = 45f;

        public override IEnumerator Execute(BossContext context)
        {
            var centreAngle = context.AimAngle();

            // A single shot has no gap to divide, so it goes straight down the middle.
            if (bulletCount == 1)
            {
                context.FireAtAngle(centreAngle);
                yield break;
            }

            var step = arcDegrees / (bulletCount - 1);
            var firstAngle = centreAngle - arcDegrees * 0.5f;

            for (var i = 0; i < bulletCount; i++)
            {
                context.FireAtAngle(firstAngle + step * i);
            }
        }

        public override float Suitability(BossContext context)
        {
            // Covering an area is worth most against someone with somewhere to run to.
            return Mathf.Lerp(0.45f, 1f, context.TargetMobility);
        }
    }
}
