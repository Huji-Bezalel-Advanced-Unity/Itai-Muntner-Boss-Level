using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Releases small enemies that drift towards the player until they are killed.
    /// </summary>
    /// <remarks>
    /// The only attack that leaves something behind. Everything else resolves and is gone, so
    /// the fight is a series of separate problems; minions turn it into a situation the player
    /// is managing, because ignoring one costs them the arena a piece at a time.
    /// <para>
    /// It is also the only attack that competes for the player's shots. Time spent clearing
    /// minions is time the boss is not taking damage, which is a more interesting cost than
    /// simply doing more damage would be.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "SummonMinions", menuName = "Boss Level/Attacks/Summon Minions")]
    public class SummonMinionsAttack : BossAttack
    {
        [SerializeField, Min(1)] private int minionCount = 2;

        [SerializeField, Min(0f)] private float delayBetweenSummons = 0.3f;

        [Tooltip("Where minions appear relative to the boss. They should emerge from it rather " +
                 "than materialise next to the player.")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(-1.5f, 1f);

        [SerializeField, Min(0f)] private float spawnSpread = 1.2f;

        [Tooltip("Once this many are already hunting, the boss stops summoning. A crowded arena " +
                 "is not pressure, it is noise — and it makes the fight unwinnable rather than hard.")]
        [SerializeField, Min(1)] private int comfortableCrowd = 4;

        public override IEnumerator Execute(BossContext context)
        {
            var origin = context.BossPosition + spawnOffset;

            for (var i = 0; i < minionCount; i++)
            {
                var scatter = new Vector2(
                    Random.Range(-spawnSpread, spawnSpread),
                    Random.Range(-spawnSpread, spawnSpread));

                context.SpawnMinion(origin + scatter);

                if (i < minionCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSummons);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // Falls away as the arena fills, so minions accumulate to a pressure and stop.
            // Needs no line of sight — the minions find their own way there.
            var crowding = Mathf.InverseLerp(0f, comfortableCrowd, context.ActiveMinionCount);

            return Mathf.Lerp(0.9f, 0.05f, crowding);
        }
    }
}
