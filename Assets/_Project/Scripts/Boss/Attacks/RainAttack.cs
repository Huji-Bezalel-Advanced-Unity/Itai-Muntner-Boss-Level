using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Drops projectiles from above, scattered around where the player is heading.
    /// </summary>
    /// <remarks>
    /// Every other attack comes from the boss and is answered by moving away from it. This one
    /// arrives overhead, so retreating to a corner and holding position stops being safe and the
    /// player has to keep using the whole arena.
    /// <para>
    /// Drops are placed relative to the player rather than to fixed arena coordinates, which
    /// keeps the asset independent of how the level is laid out.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Rain", menuName = "Boss Level/Attacks/Rain")]
    public class RainAttack : BossAttack
    {
        [SerializeField, Min(1)] private int dropCount = 10;

        [SerializeField, Min(0f)] private float delayBetweenDrops = 0.11f;

        [Tooltip("How far above the player drops appear. Must clear the top of the camera view " +
                 "so they are visible falling rather than appearing in mid-air.")]
        [SerializeField] private float spawnHeight = 7f;

        [Tooltip("Half-width of the band the drops land in.")]
        [SerializeField, Min(0f)] private float horizontalSpread = 3.5f;

        public override IEnumerator Execute(BossContext context)
        {
            for (var i = 0; i < dropCount; i++)
            {
                // Aiming rather than simply reading the position means the rain lands where the
                // player is running to, so sprinting out from under it is not a free answer.
                var aim = context.AimPoint();
                var offset = Random.Range(-horizontalSpread, horizontalSpread);

                var origin = new Vector2(aim.x + offset, context.TargetPosition.y + spawnHeight);
                context.FireFrom(origin, Vector2.down);

                if (i < dropCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenDrops);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // Useless against someone already airborne — they are above where it lands and will
            // have moved by the time it arrives.
            if (!context.TargetIsGrounded)
            {
                return 0.25f;
            }

            // At its best against a player planted on the floor trading shots.
            return Mathf.Lerp(1f, 0.45f, context.TargetMobility);
        }
    }
}
