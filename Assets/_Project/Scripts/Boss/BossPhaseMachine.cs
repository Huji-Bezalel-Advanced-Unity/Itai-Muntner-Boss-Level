using System;
using System.Collections.Generic;

namespace BossLevel.Boss
{
    /// <summary>
    /// Tracks which phase of the fight the boss is in, based on how much health it has left.
    /// </summary>
    /// <remarks>
    /// This deals in thresholds and indices only, and knows nothing about what a phase contains.
    /// That keeps it plain C# — testable in milliseconds without creating a single asset — and
    /// leaves the caller to map an index onto its own data.
    /// <para>
    /// <b>It advances at most one phase per call.</b> A single large hit can cross two thresholds
    /// at once, and the naive check would jump straight from phase one to phase three, skipping
    /// a transition the player was owed. Callers advance in a loop, so both transitions play, in
    /// order. It also never moves backwards, so healing the boss cannot rewind the fight.
    /// </para>
    /// </remarks>
    public class BossPhaseMachine
    {
        private readonly IReadOnlyList<float> _thresholds;

        /// <param name="thresholds">
        /// Health fractions at which each phase begins, ordered first to last and descending.
        /// </param>
        public BossPhaseMachine(IReadOnlyList<float> thresholds)
        {
            if (thresholds == null || thresholds.Count == 0)
            {
                throw new ArgumentException("A boss needs at least one phase.", nameof(thresholds));
            }

            _thresholds = thresholds;
            CurrentIndex = 0;
        }

        /// <summary>Index of the phase currently in effect.</summary>
        public int CurrentIndex { get; private set; }

        public int PhaseCount => _thresholds.Count;

        public bool IsFinalPhase => CurrentIndex == _thresholds.Count - 1;

        /// <summary>Raised with the new index each time the phase actually changes.</summary>
        public event Action<int> PhaseChanged;

        /// <summary>
        /// Moves forward one phase if the given health fraction calls for it.
        /// </summary>
        /// <returns>
        /// True if the phase changed. Call again in a loop until it returns false, so that a hit
        /// crossing several thresholds plays every transition rather than skipping to the last.
        /// </returns>
        public bool TryAdvance(float healthFraction)
        {
            var target = PhaseIndexFor(healthFraction);

            if (target <= CurrentIndex)
            {
                return false;
            }

            CurrentIndex++;
            PhaseChanged?.Invoke(CurrentIndex);
            return true;
        }

        /// <summary>The phase the given health fraction belongs to, ignoring current position.</summary>
        public int PhaseIndexFor(float healthFraction)
        {
            // The correct phase is the deepest one whose threshold the boss has already fallen
            // to. Walking forward from the start keeps that true regardless of how the
            // thresholds are spaced.
            var index = 0;

            for (var i = 0; i < _thresholds.Count; i++)
            {
                if (healthFraction <= _thresholds[i])
                {
                    index = i;
                }
            }

            return index;
        }
    }
}
