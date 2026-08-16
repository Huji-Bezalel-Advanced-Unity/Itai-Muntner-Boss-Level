using System.Collections.Generic;
using UnityEngine;

namespace BossLevel.Boss.Data
{
    /// <summary>
    /// A complete boss: how much health it has, and the phases it moves through.
    /// </summary>
    /// <remarks>
    /// Health lives here rather than on the scene component so the boss is described in one
    /// place, and so a second boss is a second asset rather than a second prefab to keep in sync.
    /// </remarks>
    [CreateAssetMenu(fileName = "Boss", menuName = "Boss Level/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHealth = 300;

        [Tooltip("Ordered from first to last. Thresholds should descend — 1, then 0.66, then " +
                 "0.33 for a three-phase fight.")]
        [SerializeField] private List<BossPhase> phases = new List<BossPhase>();

        public int MaxHealth => maxHealth;

        public IReadOnlyList<BossPhase> Phases => phases;

        /// <summary>
        /// The health thresholds of each phase, in order.
        /// </summary>
        /// <remarks>
        /// Built on demand rather than exposed as a stored list, because
        /// <see cref="BossPhaseMachine"/> only cares about the numbers — keeping it ignorant of
        /// what a phase actually contains is what makes it testable without any assets.
        /// </remarks>
        public IReadOnlyList<float> GetPhaseThresholds()
        {
            var thresholds = new List<float>(phases.Count);

            foreach (var phase in phases)
            {
                thresholds.Add(phase != null ? phase.HealthThreshold : 0f);
            }

            return thresholds;
        }

        /// <summary>
        /// Warns when the phase thresholds are not in descending order.
        /// </summary>
        /// <remarks>
        /// Out-of-order thresholds do not throw — they simply make the fight jump phases at
        /// nonsensical moments, or skip straight to the last one on the first frame. That is
        /// hard to recognise while playing and trivial to spot here.
        /// </remarks>
        private void OnValidate()
        {
            for (var i = 1; i < phases.Count; i++)
            {
                if (phases[i] == null || phases[i - 1] == null)
                {
                    continue;
                }

                if (phases[i].HealthThreshold >= phases[i - 1].HealthThreshold)
                {
                    Debug.LogWarning(
                        $"{name}: phase {i} ({phases[i].name}) has a threshold of " +
                        $"{phases[i].HealthThreshold}, which is not below phase {i - 1} " +
                        $"({phases[i - 1].HealthThreshold}). Thresholds must descend.", this);
                }
            }
        }
    }
}
