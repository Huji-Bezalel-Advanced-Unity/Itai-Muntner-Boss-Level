using BossLevel.Combat;
using UnityEngine;

namespace BossLevel.Boss
{
    /// <summary>
    /// Everything an attack needs to know about the world, handed to it each time it runs.
    /// </summary>
    /// <remarks>
    /// Attacks are ScriptableObject assets and therefore shared between every boss that uses
    /// them, so they must never hold references to scene objects. Passing the world in as an
    /// argument is what keeps them stateless.
    /// <para>
    /// The helpers exist so an attack reads as its own shape — "fire five shots across a sixty
    /// degree fan" — rather than as trigonometry mixed with pool plumbing.
    /// </para>
    /// </remarks>
    public class BossContext
    {
        /// <summary>
        /// How many times the intercept estimate is refined. Two is ample for a projectile that
        /// crosses the arena in well under a second; more would be arithmetic nobody can feel.
        /// </summary>
        private const int PredictionRefinements = 2;

        /// <summary>
        /// Roughly the player's running speed. Used only as the yardstick for
        /// <see cref="TargetMobility"/>, so attacks can ask "is this player moving?" without
        /// each one inventing its own threshold.
        /// </summary>
        private const float BriskSpeed = 6f;

        private readonly ProjectilePool _projectiles;
        private readonly HazardPool _hazards;
        private readonly ITarget _target;
        private readonly LayerMask _sightBlockers;

        public BossContext(
            Transform boss,
            Transform muzzle,
            ITarget target,
            ProjectilePool projectiles,
            HazardPool hazards,
            LayerMask sightBlockers)
        {
            Boss = boss;
            Muzzle = muzzle;
            _target = target;
            _projectiles = projectiles;
            _hazards = hazards;
            _sightBlockers = sightBlockers;
        }

        /// <summary>The boss itself. For attacks anchored to its body rather than its muzzle.</summary>
        public Transform Boss { get; }

        /// <summary>Where the boss's shots come from.</summary>
        public Transform Muzzle { get; }

        /// <summary>
        /// How far ahead of the target to aim: 0 aims where it is, 1 aims where it will be.
        /// Set from the active phase, so a later boss reads the player better than an early one.
        /// </summary>
        public float AimLead { get; set; }

        public Vector2 BossPosition => Boss.position;

        public Vector2 MuzzlePosition => Muzzle.position;

        public Vector2 TargetPosition => _target.Position;

        public Vector2 TargetVelocity => _target.Velocity;

        public float TargetSpeed => _target.Velocity.magnitude;

        /// <summary>Whether the target is standing on something — ground attacks depend on it.</summary>
        public bool TargetIsGrounded => _target.IsGrounded;

        public float DistanceToTarget => Vector2.Distance(MuzzlePosition, TargetPosition);

        /// <summary>
        /// How mobile the target currently is, from 0 (holding still) to 1 (running flat out).
        /// Attacks weigh themselves against this when the boss decides what to use.
        /// </summary>
        public float TargetMobility => Mathf.InverseLerp(0f, BriskSpeed, TargetSpeed);

        /// <summary>
        /// Whether a shot fired from the muzzle would actually reach the target, or whether
        /// something solid is in the way.
        /// </summary>
        public bool HasLineOfSightToTarget =>
            Physics2D.Linecast(MuzzlePosition, TargetPosition, _sightBlockers).collider == null;

        /// <summary>
        /// A multiplier for attacks that have to travel to the target: 1 with a clear shot, low
        /// when cover is in the way.
        /// </summary>
        /// <remarks>
        /// This is what stops the boss emptying its whole repertoire into the underside of a
        /// platform. Attacks that must cross the arena discount themselves when they cannot,
        /// which leaves the ones that do not need a clear line to win the comparison — so
        /// hiding stops being an answer without the player ever being told it is not.
        /// </remarks>
        public float LineOfSightFactor => HasLineOfSightToTarget ? 1f : 0.15f;

        /// <summary>The angle straight at the target's current position, ignoring any lead.</summary>
        public float AngleToTarget()
        {
            return AngleTowards(TargetPosition);
        }

        /// <summary>
        /// The point an attack should aim at, blended between where the target is and where it
        /// is predicted to be by <see cref="AimLead"/>.
        /// </summary>
        /// <remarks>
        /// A boss that leads perfectly every time is not fun — it removes movement as an answer
        /// entirely. Blending gives the fight a dial: an early phase aims sloppily and can be
        /// walked away from, a late phase aims where the player is going.
        /// </remarks>
        public Vector2 AimPoint()
        {
            return Vector2.Lerp(TargetPosition, PredictedInterceptPoint(), Mathf.Clamp01(AimLead));
        }

        /// <summary>The angle towards <see cref="AimPoint"/>. What most attacks should fire along.</summary>
        public float AimAngle()
        {
            return AngleTowards(AimPoint());
        }

        /// <summary>Fires one projectile from the muzzle along <paramref name="direction"/>.</summary>
        public void Fire(Vector2 direction)
        {
            _projectiles.Spawn(MuzzlePosition, direction);
        }

        /// <summary>Fires one projectile from the muzzle at an angle measured from world right.</summary>
        public void FireAtAngle(float degrees)
        {
            Fire(DirectionFromAngle(degrees));
        }

        /// <summary>
        /// Fires from somewhere other than the muzzle — used by attacks that arrive from above
        /// or travel along the ground rather than coming out of the boss's body.
        /// </summary>
        public void FireFrom(Vector2 origin, Vector2 direction)
        {
            _projectiles.Spawn(origin, direction);
        }

        /// <summary>
        /// Marks a patch of ground that will strike after a warning. Does nothing, with a
        /// warning logged, if the boss has no hazard pool wired up.
        /// </summary>
        public void SpawnHazard(Vector2 position)
        {
            if (_hazards == null)
            {
                Debug.LogWarning("An attack asked for a ground hazard but the boss has no hazard pool.");
                return;
            }

            _hazards.Spawn(position);
        }

        /// <summary>Converts an angle in degrees, measured from world right, into a unit vector.</summary>
        public static Vector2 DirectionFromAngle(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>
        /// Where a shot fired now would meet the target if it kept its current velocity.
        /// </summary>
        /// <remarks>
        /// Guess the flight time from the present distance, move the target by it, then
        /// re-measure. Each pass tightens the estimate; it converges quickly because the target
        /// cannot move far compared with how fast the projectile travels.
        /// </remarks>
        private Vector2 PredictedInterceptPoint()
        {
            var projectileSpeed = _projectiles.ProjectileSpeed;
            var origin = MuzzlePosition;
            var position = TargetPosition;

            if (projectileSpeed <= 0f)
            {
                return position;
            }

            var flightTime = Vector2.Distance(origin, position) / projectileSpeed;

            for (var refinement = 0; refinement < PredictionRefinements; refinement++)
            {
                var predicted = position + TargetVelocity * flightTime;
                flightTime = Vector2.Distance(origin, predicted) / projectileSpeed;
            }

            return position + TargetVelocity * flightTime;
        }

        private float AngleTowards(Vector2 point)
        {
            var toPoint = point - MuzzlePosition;
            return Mathf.Atan2(toPoint.y, toPoint.x) * Mathf.Rad2Deg;
        }
    }
}
