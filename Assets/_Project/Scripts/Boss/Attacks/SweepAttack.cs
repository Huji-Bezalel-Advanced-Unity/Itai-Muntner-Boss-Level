using System.Collections;
using UnityEngine;

namespace BossLevel.Boss.Attacks
{
    /// <summary>
    /// Sweeps a stream of shots across an arc, forming a moving wall the player must cross.
    /// </summary>
    /// <remarks>
    /// The aim is locked once, at the start. A sweep that re-aimed would simply follow the
    /// player and stop being a pattern to read — the whole point is that it is going somewhere
    /// predictable and the player has to choose which side of it to be on.
    /// </remarks>
    [CreateAssetMenu(fileName = "Sweep", menuName = "Boss Level/Attacks/Sweep")]
    public class SweepAttack : BossAttack
    {
        [SerializeField, Min(2)] private int shotCount = 12;

        [Tooltip("Total angle covered, centred on where the player was when the sweep began.")]
        [SerializeField, Range(0f, 360f)] private float sweepDegrees = 70f;

        [SerializeField, Min(0.05f)] private float duration = 1.2f;

        [Tooltip("Sweep from above the player downwards, rather than from below upwards.")]
        [SerializeField] private bool sweepDownwards = true;

        public override IEnumerator Execute(BossContext context)
        {
            var centreAngle = context.AngleToPlayer();
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
    }
}
