using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Fires a fan of projectiles centred on the player, all at once.
    /// </summary>
    /// <remarks>
    /// The bread-and-butter attack: it punishes standing still and is dodged by moving to either
    /// edge of the fan. Widening the arc while adding shots keeps the gaps the same; widening it
    /// without adding shots opens them up.
    /// </remarks>
    [CreateAssetMenu(fileName = "SpreadShot", menuName = "Boss Level/Attacks/Spread Shot")]
    public class SpreadShotAttack : BossAttack
    {
        [SerializeField, Min(1)] private int bulletCount = 5;

        [Tooltip("Total width of the fan, centred on the player.")]
        [SerializeField, Range(0f, 360f)] private float arcDegrees = 60f;

        public override IEnumerator Execute(BossContext context)
        {
            var centreAngle = context.AngleToPlayer();

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
    }
}
