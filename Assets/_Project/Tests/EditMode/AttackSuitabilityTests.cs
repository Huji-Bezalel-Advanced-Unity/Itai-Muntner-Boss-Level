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

        /// <summary>The project's Ground layer, used to stand in for cover.</summary>
        private const int GroundLayer = 8;

        private readonly List<Object> _created = new List<Object>();

        private StubTarget _target;
        private Transform _boss;
        private Transform _muzzle;

        [SetUp]
        public void SetUp()
        {
            _boss = MakeObject("Boss", new Vector2(6f, 0f)).transform;
            _muzzle = MakeObject("Muzzle", new Vector2(5f, 0f)).transform;

            _target = new StubTarget
            {
                // On the floor the boss's shockwave travels along, which is the boss origin
                // plus the slam's default ground offset. Sitting exactly on it keeps the
                // "same floor" check clear of its own tolerance boundary.
                Position = new Vector2(0f, -1.5f),
                Velocity = Vector2.zero,
                IsGrounded = true,
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private GameObject MakeObject(string name, Vector2 position)
        {
            var created = new GameObject(name);
            created.transform.position = position;
            _created.Add(created);
            return created;
        }

        private T MakeAttack<T>() where T : BossAttack
        {
            var attack = ScriptableObject.CreateInstance<T>();
            _created.Add(attack);
            return attack;
        }

        /// <remarks>
        /// The pools are left null deliberately: suitability never fires anything, so supplying
        /// them would mean building half a scene to assert a rule that has nothing to do with one.
        /// The sight mask defaults to nothing, which means every shot has a clear line unless a
        /// test arranges otherwise.
        /// </remarks>
        private BossContext Context(LayerMask sightBlockers = default)
        {
            return new BossContext(_boss, _muzzle, _target, null, null, null, sightBlockers);
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
        public void Slam_IsDiscardedAgainstATargetStandingOnARaisedPlatform()
        {
            var slam = MakeAttack<SlamAttack>();

            // Grounded, but well above the floor the wave travels along — it would pass by
            // underneath. Confusing "feet down" with "reachable" is the mistake this prevents.
            _target.IsGrounded = true;
            _target.Position = new Vector2(0f, 4f);

            Assert.Less(slam.Suitability(Context()), 0.2f);
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
        public void CoverMakesTravellingAttacksYieldToEruption()
        {
            var spread = MakeAttack<SpreadShotAttack>();
            var eruption = MakeAttack<EruptionAttack>();

            // Level with the muzzle, so the sight line runs straight through the blocker rather
            // than under it.
            _target.Position = Vector2.zero;

            var wall = MakeObject("Cover", new Vector2(2.5f, 0f));
            wall.layer = GroundLayer;
            wall.AddComponent<BoxCollider2D>();

            // Physics queries read cached transforms, which edit mode does not push
            // automatically after a change.
            Physics2D.SyncTransforms();

            var blocked = Context(1 << GroundLayer);

            Assert.IsFalse(blocked.HasLineOfSightToTarget, "The wall should block the line.");

            // This is the whole point of the mechanism: with something solid in the way, the
            // attacks that must cross the arena stand aside for the one that does not, so a
            // player hiding behind a platform stops being safe.
            Assert.Greater(eruption.Suitability(blocked), spread.Suitability(blocked));
        }

        [Test]
        public void SummonMinions_IsPreferredWhileTheArenaIsEmpty()
        {
            var summon = MakeAttack<SummonMinionsAttack>();

            // With no pool wired up the arena reads as empty, which is the situation summoning
            // is for. The falling-off half of the rule needs live minions and is therefore
            // covered in play rather than here.
            Assert.Greater(summon.Suitability(Context()), 0.8f);
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
                MakeAttack<EruptionAttack>(),
                MakeAttack<SummonMinionsAttack>(),
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
