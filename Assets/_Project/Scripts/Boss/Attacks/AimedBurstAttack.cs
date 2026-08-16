using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Fires a short burst straight at the player, re-aiming between each shot.
    /// </summary>
    /// <remarks>
    /// Because it re-aims every shot, this attack tracks: the player must keep moving for the
    /// whole burst rather than sidestepping once. That makes it the boss's answer to a player
    /// holding position and trading shots — the habit that otherwise makes standing still the
    /// strongest strategy in the fight.
    /// </remarks>
    [CreateAssetMenu(fileName = "AimedBurst", menuName = "Boss Level/Attacks/Aimed Burst")]
    public class AimedBurstAttack : BossAttack
    {
        [SerializeField, Min(1)] private int shotCount = 4;

        [SerializeField, Min(0f)] private float delayBetweenShots = 0.16f;

        [Tooltip("Random wobble on each shot. A perfectly accurate tracking attack is unfair at " +
                 "close range; a little scatter gives the player somewhere to be.")]
        [SerializeField, Range(0f, 30f)] private float aimJitterDegrees = 3f;

        public override IEnumerator Execute(BossContext context)
        {
            for (var i = 0; i < shotCount; i++)
            {
                // Aim is read inside the loop rather than before it, which is what makes the
                // burst follow the player instead of committing to where they started.
                var jitter = Random.Range(-aimJitterDegrees, aimJitterDegrees);
                context.FireAtAngle(context.AimAngle() + jitter);

                if (i < shotCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenShots);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // Pinpoint and relentless, so it is at its best against a player who is holding
            // still — and nearly wasted on one already sprinting out of the way.
            return Mathf.Lerp(1f, 0.4f, context.TargetMobility);
        }
    }
}
