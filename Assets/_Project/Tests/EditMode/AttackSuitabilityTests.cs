using System.Collections.Generic;
using BossLevel.Boss;
using BossLevel.Boss.Attacks;
using BossLevel.TestSupport;
using NUnit.Framework;
using UnityEngine;

namespace BossLevel.Tests
{
    /// <summary>
    /// Pins the boss's judgement — which attack it thinks fits which situation.
    /// </summary>
    /// <remarks>
    /// These are the rules that make the boss read as deliberate rather than random, and they
    /// are easy to invert by accident while tuning: a sign flipped in one <c>Lerp</c> turns a
    /// boss that punishes camping into one that rewards it, and nothing about that failure looks
    /// like a bug while playing. It simply feels weak.
    /// </remarks>
    public class AttackSuitabilityTests
    {
        /// <summary>Comfortably above the mobility yardstick, so it reads as "running".</summary>
        private static readonly Vector2 Sprinting = new Vector2(8f, 0f);

        private readonly List<Object> _created = new List<Object>();

        private StubTarget _target;

        [SetUp]
        public void SetUp()
        {
            _target = new StubTarget
            {
                Position = Vector2.zero,
                Velocity = Vector2.zero,
                IsGrounded = true,
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        /// <remarks>
        /// The boss transform, muzzle and pool are left null deliberately: suitability only ever
        /// consults the target, and supplying the rest would mean building a scene to assert a
        /// rule that has nothing to do with one.
        /// </remarks>
        private BossContext Context()
        {
            return new BossContext(null, null, _target, null);
        }

        private T MakeAttack<T>() where T : BossAttack
        {
            var attack = ScriptableObject.CreateInstance<T>();
            _created.Add(attack);
            return attack;
        }

        [Test]
        public void Slam_IsPreferredAgainstAGroundedTargetAndAllButDiscardedAgainstAnAirborneOne()
        {
            var slam = MakeAttack<SlamAttack>();

            _target.IsGrounded = true;
            var grounded = slam.Suitability(Context());

            _target.IsGrounded = false;
            var airborne = slam.Suitability(Context());

            Assert.Greater(grounded, 0.9f, "A shockwave should be the obvious pick on the floor.");
            Assert.Less(airborne, 0.1f, "A player already in the air simply flies over it.");
        }

        [Test]
        public void Sweep_IsPreferredAgainstAnAirborneTarget()
        {
            var sweep = MakeAttack<SweepAttack>();

            _target.IsGrounded = false;
            var airborne = sweep.Suitability(Context());

            _target.IsGrounded = true;
            var grounded = sweep.Suitability(Context());

            Assert.Greater(airborne, grounded,
                "A committed jump arc is the hardest thing to steer out of a sweep.");
        }

        [Test]
        public void AimedBurst_IsPreferredAgainstAStationaryTarget()
        {
            var burst = MakeAttack<AimedBurstAttack>();

            var stationary = burst.Suitability(Context());

            _target.Velocity = Sprinting;
            var moving = burst.Suitability(Context());

            Assert.Greater(stationary, moving,
                "Standing still is the habit a tracking attack exists to punish.");
        }

        [Test]
        public void SpreadShot_IsPreferredAgainstAMovingTarget()
        {
            var spread = MakeAttack<SpreadShotAttack>();

            var stationary = spread.Suitability(Context());

            _target.Velocity = Sprinting;
            var moving = spread.Suitability(Context());

            Assert.Greater(moving, stationary,
                "Covering an area is worth most against someone with room to run.");
        }

        [Test]
        public void Rain_IsPreferredAgainstAPlayerCampingOnTheFloor()
        {
            var rain = MakeAttack<RainAttack>();

            var camping = rain.Suitability(Context());

            _target.Velocity = Sprinting;
            var running = rain.Suitability(Context());

            _target.Velocity = Vector2.zero;
            _target.IsGrounded = false;
            var airborne = rain.Suitability(Context());

            Assert.Greater(camping, running);
            Assert.Greater(camping, airborne, "Rain lands where a jumping player no longer is.");
        }

        [Test]
        public void EverySuitabilityStaysWithinItsRange()
        {
            var attacks = new BossAttack[]
            {
                MakeAttack<SpreadShotAttack>(),
                MakeAttack<AimedBurstAttack>(),
                MakeAttack<SweepAttack>(),
                MakeAttack<RainAttack>(),
                MakeAttack<SlamAttack>(),
            };

            foreach (var grounded in new[] { true, false })
            {
                foreach (var velocity in new[] { Vector2.zero, Sprinting })
                {
                    _target.IsGrounded = grounded;
                    _target.Velocity = velocity;

                    foreach (var attack in attacks)
                    {
                        var score = attack.Suitability(Context());

                        // Scores are compared directly against each other, so one drifting out
                        // of range would quietly dominate every choice the boss makes.
                        Assert.That(score, Is.InRange(0f, 1f),
                            $"{attack.GetType().Name} scored {score}.");
                    }
                }
            }
        }
    }
}
