using BossLevel.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BossLevel.Tests
{
    /// <summary>
    /// Covers the rules that are easy to get subtly wrong and hard to notice in play testing —
    /// chiefly that death happens exactly once no matter how much damage arrives at once.
    /// </summary>
    public class HealthTests
    {
        private const int StartingHealth = 100;

        private GameObject _owner;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("HealthTestOwner");
            _health = _owner.AddComponent<Health>();

            // Set the maximum explicitly rather than relying on Awake, which edit mode does not
            // guarantee to run. This keeps the tests independent of that detail.
            _health.ResetTo(StartingHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void ResetTo_StartsAtFullHealthAndAlive()
        {
            Assert.AreEqual(StartingHealth, _health.Current);
            Assert.AreEqual(StartingHealth, _health.Max);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _health.TakeDamage(30);

            Assert.AreEqual(70, _health.Current);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_ClampsAtZeroRatherThanGoingNegative()
        {
            _health.TakeDamage(StartingHealth * 5);

            Assert.AreEqual(0, _health.Current);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void Damaged_ReportsTheAmountActuallyApplied()
        {
            var reported = 0;
            _health.Damaged += amount => reported = amount;

            _health.TakeDamage(StartingHealth * 5);

            // Overkill is clamped, so listeners such as damage numbers do not show a lie.
            Assert.AreEqual(StartingHealth, reported);
        }

        [Test]
        public void Died_FiresExactlyOnceEvenWhenDamagedAgainAfterwards()
        {
            var deathCount = 0;
            _health.Died += () => deathCount++;

            _health.TakeDamage(StartingHealth);
            _health.TakeDamage(StartingHealth);
            _health.TakeDamage(1);

            Assert.AreEqual(1, deathCount, "Death sequence would have run more than once.");
        }

        [Test]
        public void TakeDamage_IsIgnoredWhileInvulnerable()
        {
            _health.IsInvulnerable = true;
            _health.TakeDamage(50);

            Assert.AreEqual(StartingHealth, _health.Current);
        }

        [Test]
        public void TakeDamage_IgnoresNonPositiveAmounts()
        {
            _health.TakeDamage(0);
            _health.TakeDamage(-25);

            Assert.AreEqual(StartingHealth, _health.Current);
        }

        [Test]
        public void Heal_RestoresHealthButNotAboveMaximum()
        {
            _health.TakeDamage(40);
            _health.Heal(100);

            Assert.AreEqual(StartingHealth, _health.Current);
        }

        [Test]
        public void Heal_DoesNotResurrectTheDead()
        {
            _health.TakeDamage(StartingHealth);
            _health.Heal(50);

            Assert.AreEqual(0, _health.Current);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void Fraction_TracksRemainingHealth()
        {
            _health.TakeDamage(25);

            Assert.AreEqual(0.75f, _health.Fraction, 0.0001f);
        }
    }
}
