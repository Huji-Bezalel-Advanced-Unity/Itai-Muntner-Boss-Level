using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Fires a short burst of shots straight at the player, re-aiming between each one.
    /// </summary>
    /// <remarks>
    /// Because it re-aims every shot, this attack tracks — standing still is fatal and the
    /// player has to keep moving through the whole burst. That makes it the natural counterpart
    /// to the spread, which punishes movement into the fan.
    /// </remarks>
    [CreateAssetMenu(fileName = "AimedBurst", menuName = "Boss Level/Attacks/Aimed Burst")]
    public class AimedBurstAttack : BossAttack
    {
        [SerializeField, Min(1)] private int shotCount = 3;

        [SerializeField, Min(0f)] private float delayBetweenShots = 0.18f;

        [Tooltip("Random wobble on each shot. A perfectly accurate tracking attack is unfair " +
                 "at close range; a little scatter gives the player somewhere to be.")]
        [SerializeField, Range(0f, 30f)] private float aimJitterDegrees = 3f;

        public override IEnumerator Execute(BossContext context)
        {
            for (var i = 0; i < shotCount; i++)
            {
                // Aim is read here rather than before the loop, which is what makes it track.
                var jitter = Random.Range(-aimJitterDegrees, aimJitterDegrees);
                context.FireAtAngle(context.AngleToPlayer() + jitter);

                if (i < shotCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenShots);
                }
            }
        }
    }
}
