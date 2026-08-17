using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Opens volcanic vents beneath the player, which erupt upwards a moment later.
    /// </summary>
    /// <remarks>
    /// Every other attack the boss has travels from the boss to the player, so distance and
    /// anything solid in between work against all of them at once. A vent does not travel — it
    /// opens where the player already is, and the only answer is to leave.
    /// <para>
    /// It also asks a different question. The projectiles ask which way to dodge; a column going
    /// straight up cannot be jumped over, so this asks whether the player is willing to give up
    /// the ground they are standing on. That is the question worth asking of someone who has
    /// settled into a spot they like.
    /// </para>
    /// <para>
    /// The warning belongs to the vent rather than to the boss, and is deliberately long — over
    /// two seconds. This attack should therefore carry a <b>short</b> telegraph of its own, or
    /// the player is warned twice and the whole thing drags.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Eruption", menuName = "Boss Level/Attacks/Eruption")]
    public class EruptionAttack : BossAttack
    {
        [SerializeField, Min(1)] private int ventCount = 3;

        [Tooltip("Gap between vents opening. They overlap, because each carries its own long " +
                 "warning — the player should be tracking two at once.")]
        [SerializeField, Min(0f)] private float delayBetweenVents = 0.8f;

        [Tooltip("Random scatter around the aim point, so running in one direction is not a " +
                 "guaranteed escape from the whole sequence.")]
        [SerializeField, Min(0f)] private float scatter = 1.5f;

        [Tooltip("Height of the arena floor relative to the boss's own origin. Vents open here.")]
        [SerializeField] private float groundOffset = -1.5f;

        public override IEnumerator Execute(BossContext context)
        {
            var groundHeight = context.BossPosition.y + groundOffset;

            for (var i = 0; i < ventCount; i++)
            {
                // Aiming rather than reading the position means later vents open where the
                // player is running to, so a single committed sprint does not outpace the set.
                var aim = context.AimPoint();
                var offset = Random.Range(-scatter, scatter);

                context.SpawnVolcano(new Vector2(aim.x + offset, groundHeight));

                if (i < ventCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenVents);
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
