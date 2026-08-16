using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Sweeps a stream of shots across an arc, forming a moving wall to cross.
    /// </summary>
    /// <remarks>
    /// The aim is locked once, at the start. A sweep that re-aimed would simply follow the
    /// player and stop being a pattern to read — the point is that it goes somewhere
    /// predictable and the player has to choose which side of it to be on.
    /// </remarks>
    [CreateAssetMenu(fileName = "Sweep", menuName = "Boss Level/Attacks/Sweep")]
    public class SweepAttack : BossAttack
    {
        [SerializeField, Min(2)] private int shotCount = 14;

        [Tooltip("Total angle covered, centred on where the sweep was aimed when it began.")]
        [SerializeField, Range(0f, 360f)] private float sweepDegrees = 70f;

        [SerializeField, Min(0.05f)] private float duration = 1.1f;

        [Tooltip("Sweep downwards through the player rather than upwards.")]
        [SerializeField] private bool sweepDownwards = true;

        public override IEnumerator Execute(BossContext context)
        {
            var centreAngle = context.AimAngle();
            var halfSweep = sweepDegrees * 0.5f;

            var startAngle = centreAngle + (sweepDownwards ? halfSweep : -halfSweep);
            var endAngle = centreAngle + (sweepDownwards ? -halfSweep : halfSweep);

            var interval = duration / (shotCount - 1);

            for (var i = 0; i < shotCount; i++)
            {
                var progress = (float)i / (shotCount - 1);
                context.FireAtAngle(Mathf.Lerp(startAngle, endAngle, progress));

                if (i < shotCount - 1)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        public override float Suitability(BossContext context)
        {
            // Hardest to answer in mid-air, where the player has already committed to an arc and
            // cannot simply reverse out of the way — but it still has to reach them.
            return (context.TargetIsGrounded ? 0.5f : 1f) * context.LineOfSightFactor;
        }
    }
}
