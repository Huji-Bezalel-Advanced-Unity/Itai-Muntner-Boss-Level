using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Marks patches of ground beneath the player, which erupt a moment later.
    /// </summary>
    /// <remarks>
    /// The boss's answer to cover. Every other attack it has must travel from the boss to the
    /// player, so a single platform in between defeats all of them at once. This one does not
    /// travel — it appears where the player is standing, and no amount of geometry helps.
    /// <para>
    /// It also demands a different skill. The projectile attacks ask which way to dodge; this
    /// asks whether the player is still where they were a second ago, which is exactly the
    /// question worth asking of someone who has found a spot they like.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Eruption", menuName = "Boss Level/Attacks/Eruption")]
    public class EruptionAttack : BossAttack
    {
        [SerializeField, Min(1)] private int eruptionCount = 4;

        [Tooltip("Gap between eruptions. Short enough to keep the player running, long enough " +
                 "that each one can be escaped.")]
        [SerializeField, Min(0f)] private float delayBetweenEruptions = 0.4f;

        [Tooltip("Random scatter around the aim point, so a straight run in one direction is " +
                 "not a guaranteed escape.")]
        [SerializeField, Min(0f)] private float scatter = 1.2f;

        public override IEnumerator Execute(BossContext context)
        {
            for (var i = 0; i < eruptionCount; i++)
            {
                // Aiming rather than reading the position means later eruptions land where the
                // player is running to, so a single committed sprint does not outpace the whole
                // sequence.
                var aim = context.AimPoint();
                var offset = new Vector2(Random.Range(-scatter, scatter), 0f);

                context.SpawnHazard(aim + offset);

                if (i < eruptionCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenEruptions);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // Worth using at any time, and the only thing worth using when the player has put
            // something solid between themselves and the boss.
            return context.HasLineOfSightToTarget ? 0.55f : 1f;
        }
    }
}
