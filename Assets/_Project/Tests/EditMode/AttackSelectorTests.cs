using System.Collections.Generic;
using BossLevel.Boss;
using BossLevel.Boss.Attacks;
using NUnit.Framework;
using UnityEngine;

namespace BossLevel.Tests
{
    /// <summary>
    /// Covers the selection rules a player would notice if they broke — chiefly that the boss
    /// never repeats an attack back to back, including across the seam where the bag refills.
    /// </summary>
    public class AttackSelectorTests
    {
        private const int Seed = 12345;

        private List<BossAttack> _attacks;

        [SetUp]
        public void SetUp()
        {
            _attacks = new List<BossAttack>
            {
                MakeAttack("A"),
                MakeAttack("B"),
                MakeAttack("C"),
                MakeAttack("D"),
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var attack in _attacks)
            {
                Object.DestroyImmediate(attack);
            }

            _attacks = null;
        }

        private static BossAttack MakeAttack(string name)
        {
            var attack = ScriptableObject.CreateInstance<SpreadShotAttack>();
            attack.name = name;
            return attack;
        }

        [Test]
        public void Constructor_RejectsAnEmptyAttackList()
        {
            Assert.Throws<System.ArgumentException>(
                () => new AttackSelector(new List<BossAttack>()));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<System.ArgumentException>(() => new AttackSelector(null));
        }

        [Test]
        public void Next_DealsEveryAttackOnceBeforeRepeatingAny()
        {
            var selector = new AttackSelector(_attacks, new System.Random(Seed));
            var drawn = new List<BossAttack>();

            for (var i = 0; i < _attacks.Count; i++)
            {
                drawn.Add(selector.Next());
            }

            CollectionAssert.AreEquivalent(_attacks, drawn);
        }

        [Test]
        public void Next_NeverRepeatsTheSameAttackTwiceInARow()
        {
            // Many seeds, many draws: the failure this guards against is a refill happening to
            // put the attack that just ran at the front of the new bag, which only shows up
            // occasionally.
            for (var seed = 0; seed < 50; seed++)
            {
                var selector = new AttackSelector(_attacks, new System.Random(seed));
                BossAttack previous = null;

                for (var draw = 0; draw < 200; draw++)
                {
                    var current = selector.Next();

                    Assert.AreNotSame(previous, current,
                        $"Attack repeated back to back on seed {seed}, draw {draw}.");

                    previous = current;
                }
            }
        }

        [Test]
        public void Next_WithASingleAttack_KeepsReturningIt()
        {
            var single = new List<BossAttack> { _attacks[0] };
            var selector = new AttackSelector(single, new System.Random(Seed));

            // The no-repeat rule cannot apply when there is nothing else to choose, and the
            // selector must not deadlock trying to satisfy it.
            for (var i = 0; i < 10; i++)
            {
                Assert.AreSame(_attacks[0], selector.Next());
            }
        }

        [Test]
        public void Next_RespectsWeightingByCountingDuplicateEntries()
        {
            // "A" listed three times against one "B" should come up roughly three times as often.
            var weighted = new List<BossAttack>
            {
                _attacks[0], _attacks[0], _attacks[0], _attacks[1],
            };

            var selector = new AttackSelector(weighted, new System.Random(Seed));

            var countOfA = 0;
            const int draws = 400;

            for (var i = 0; i < draws; i++)
            {
                if (selector.Next() == _attacks[0])
                {
                    countOfA++;
                }
            }

            Assert.AreEqual(draws * 0.75f, countOfA, draws * 0.05f);
        }

        [Test]
        public void Next_NeverReturnsNull()
        {
            var selector = new AttackSelector(_attacks, new System.Random(Seed));

            for (var i = 0; i < 100; i++)
            {
                Assert.IsNotNull(selector.Next());
            }
        }
    }
}
