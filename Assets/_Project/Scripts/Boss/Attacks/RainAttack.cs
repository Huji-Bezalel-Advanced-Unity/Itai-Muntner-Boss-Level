using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Drops projectiles from above, scattered around wherever the player is standing.
    /// </summary>
    /// <remarks>
    /// Every other attack comes from the boss and is dodged by moving away from it. This one
    /// arrives from overhead, so backing into a corner stops being a safe answer and the player
    /// has to keep using the whole arena.
    /// <para>
    /// Drops are placed relative to the player rather than to fixed arena coordinates, which
    /// keeps the asset independent of how the level is laid out.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Rain", menuName = "Boss Level/Attacks/Rain")]
    public class RainAttack : BossAttack
    {
        [SerializeField, Min(1)] private int dropCount = 8;

        [SerializeField, Min(0f)] private float delayBetweenDrops = 0.12f;

        [Tooltip("How far above the player drops appear. Must clear the ceiling of the arena so " +
                 "they are visible on the way down.")]
        [SerializeField] private float spawnHeight = 7f;

        [Tooltip("Half-width of the band the drops land in, measured from the player.")]
        [SerializeField, Min(0f)] private float horizontalSpread = 4f;

        public override IEnumerator Execute(BossContext context)
        {
            for (var i = 0; i < dropCount; i++)
            {
                // Re-read the player each drop so the rain follows them across the arena.
                var playerPosition = context.PlayerPosition;
                var offset = Random.Range(-horizontalSpread, horizontalSpread);

                var origin = new Vector2(playerPosition.x + offset, playerPosition.y + spawnHeight);
                context.FireFrom(origin, Vector2.down);

                if (i < dropCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenDrops);
                }
            }
        }
    }
}
