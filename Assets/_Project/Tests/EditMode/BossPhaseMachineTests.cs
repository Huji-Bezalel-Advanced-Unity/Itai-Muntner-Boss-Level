using System.Collections.Generic;
using BossLevel.Boss;
using NUnit.Framework;

namespace BossLevel.Tests
{
    /// <summary>
    /// Covers the phase rules that fail silently rather than loudly — above all that a single
    /// large hit cannot skip a phase the player was owed.
    /// </summary>
    public class BossPhaseMachineTests
    {
        /// <summary>A conventional three-phase fight: full health, two thirds, one third.</summary>
        private static readonly float[] ThreePhases = { 1f, 0.66f, 0.33f };

        private BossPhaseMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _machine = new BossPhaseMachine(new List<float>(ThreePhases));
        }

        [Test]
        public void Constructor_RejectsAFightWithNoPhases()
        {
            Assert.Throws<System.ArgumentException>(
                () => new BossPhaseMachine(new List<float>()));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<System.ArgumentException>(() => new BossPhaseMachine(null));
        }

        [Test]
        public void StartsInTheFirstPhase()
        {
            Assert.AreEqual(0, _machine.CurrentIndex);
            Assert.AreEqual(3, _machine.PhaseCount);
            Assert.IsFalse(_machine.IsFinalPhase);
        }

        [Test]
        public void PhaseIndexFor_PicksTheDeepestThresholdReached()
        {
            Assert.AreEqual(0, _machine.PhaseIndexFor(1f), "Full health is the first phase.");
            Assert.AreEqual(0, _machine.PhaseIndexFor(0.7f));
            Assert.AreEqual(1, _machine.PhaseIndexFor(0.66f), "Exactly on a threshold enters it.");
            Assert.AreEqual(1, _machine.PhaseIndexFor(0.4f));
            Assert.AreEqual(2, _machine.PhaseIndexFor(0.33f));
            Assert.AreEqual(2, _machine.PhaseIndexFor(0f));
        }

        [Test]
        public void TryAdvance_DoesNothingWhileHealthStaysWithinThePhase()
        {
            Assert.IsFalse(_machine.TryAdvance(0.9f));
            Assert.AreEqual(0, _machine.CurrentIndex);
        }

        [Test]
        public void TryAdvance_MovesForwardWhenAThresholdIsCrossed()
        {
            Assert.IsTrue(_machine.TryAdvance(0.5f));
            Assert.AreEqual(1, _machine.CurrentIndex);
        }

        [Test]
        public void TryAdvance_NeverSkipsAPhaseWhenOneHitCrossesTwoThresholds()
        {
            // The bug this exists to prevent: a hit taking the boss from full health to almost
            // dead jumping straight to the final phase, skipping a transition entirely.
            Assert.IsTrue(_machine.TryAdvance(0.05f));
            Assert.AreEqual(1, _machine.CurrentIndex, "Should step to phase two first.");

            Assert.IsTrue(_machine.TryAdvance(0.05f));
            Assert.AreEqual(2, _machine.CurrentIndex, "Then on to phase three.");

            Assert.IsFalse(_machine.TryAdvance(0.05f), "And then stop.");
            Assert.IsTrue(_machine.IsFinalPhase);
        }

        [Test]
        public void TryAdvance_NeverMovesBackwardsWhenHealthIsRestored()
        {
            _machine.TryAdvance(0.2f);
            _machine.TryAdvance(0.2f);
            Assert.AreEqual(2, _machine.CurrentIndex);

            Assert.IsFalse(_machine.TryAdvance(1f), "Healing must not rewind the fight.");
            Assert.AreEqual(2, _machine.CurrentIndex);
        }

        [Test]
        public void PhaseChanged_FiresOncePerAdvanceWithTheNewIndex()
        {
            var announced = new List<int>();
            _machine.PhaseChanged += index => announced.Add(index);

            _machine.TryAdvance(0.5f);
            _machine.TryAdvance(0.5f);
            _machine.TryAdvance(0.1f);
            _machine.TryAdvance(0.1f);

            CollectionAssert.AreEqual(new[] { 1, 2 }, announced);
        }

        [Test]
        public void ASinglePhaseFightNeverAdvances()
        {
            var machine = new BossPhaseMachine(new List<float> { 1f });

            Assert.IsTrue(machine.IsFinalPhase);
            Assert.IsFalse(machine.TryAdvance(0f));
            Assert.AreEqual(0, machine.CurrentIndex);
        }
    }
}
