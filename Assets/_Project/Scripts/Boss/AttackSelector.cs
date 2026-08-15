using System.Collections.Generic;
using BossLevel.Boss.Attacks;

namespace BossLevel.Boss
{
    /// <summary>
    /// Chooses the boss's next attack using a shuffle bag rather than a plain random roll.
    /// </summary>
    /// <remarks>
    /// Uniform random selection will cheerfully pick the same attack three times running, and a
    /// player reads that as the boss being broken rather than as bad luck. A shuffle bag deals
    /// every attack once before repeating any of them, so variety is guaranteed while the order
    /// stays unpredictable.
    /// <para>
    /// Listing an attack twice in a phase weights it, because it simply goes into the bag twice.
    /// </para>
    /// <para>
    /// Plain C# with no Unity dependencies beyond the attack type, so it can be unit-tested
    /// without entering play mode. The random source is injectable for the same reason.
    /// </para>
    /// </remarks>
    public class AttackSelector
    {
        private readonly IReadOnlyList<BossAttack> _attacks;
        private readonly System.Random _random;
        private readonly List<BossAttack> _bag = new List<BossAttack>();

        private BossAttack _lastDrawn;

        /// <param name="attacks">The attacks available. Repeat an entry to weight it.</param>
        /// <param name="random">Injectable for deterministic tests. Defaults to a fresh source.</param>
        public AttackSelector(IReadOnlyList<BossAttack> attacks, System.Random random = null)
        {
            if (attacks == null || attacks.Count == 0)
            {
                throw new System.ArgumentException(
                    "A boss needs at least one attack to choose from.", nameof(attacks));
            }

            _attacks = attacks;
            _random = random ?? new System.Random();
        }

        /// <summary>Draws the next attack, refilling and reshuffling the bag when it runs dry.</summary>
        public BossAttack Next()
        {
            if (_bag.Count == 0)
            {
                Refill();
            }

            var lastIndex = _bag.Count - 1;
            var attack = _bag[lastIndex];
            _bag.RemoveAt(lastIndex);

            _lastDrawn = attack;
            return attack;
        }

        private void Refill()
        {
            _bag.Clear();
            _bag.AddRange(_attacks);
            Shuffle();

            // A bag guarantees variety within itself but says nothing about the seam between one
            // bag and the next, where the last draw of one and the first of the next can be the
            // same attack. That is the one repeat a player would actually notice, so close it.
            var nextIndex = _bag.Count - 1;

            if (_bag.Count > 1 && _bag[nextIndex] == _lastDrawn)
            {
                var swapWith = _random.Next(nextIndex);
                (_bag[swapWith], _bag[nextIndex]) = (_bag[nextIndex], _bag[swapWith]);
            }
        }

        private void Shuffle()
        {
            // Fisher-Yates.
            for (var i = _bag.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
    }
}
