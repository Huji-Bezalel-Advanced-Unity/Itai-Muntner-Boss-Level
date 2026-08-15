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
    /// The helpers here exist so an attack reads as its own shape — "fire five shots across a
    /// sixty degree fan" — rather than as trigonometry mixed with pool plumbing.
    /// </para>
    /// </remarks>
    public class BossContext
    {
        private readonly ProjectilePool _projectiles;

        public BossContext(Transform boss, Transform muzzle, Transform player, ProjectilePool projectiles)
        {
            Boss = boss;
            Muzzle = muzzle;
            Player = player;
            _projectiles = projectiles;
        }

        /// <summary>The boss itself. Useful for attacks anchored to its body rather than its muzzle.</summary>
        public Transform Boss { get; }

        /// <summary>Where the boss's shots come from.</summary>
        public Transform Muzzle { get; }

        public Transform Player { get; }

        public Vector2 BossPosition => Boss.position;

        public Vector2 MuzzlePosition => Muzzle.position;

        public Vector2 PlayerPosition => Player.position;

        /// <summary>
        /// The angle in degrees from the muzzle to the player, measured from world right.
        /// </summary>
        /// <remarks>
        /// Read this at the moment a shot fires, not at the start of the attack, if the attack
        /// should track the player. Read it once up front if it should not.
        /// </remarks>
        public float AngleToPlayer()
        {
            var toPlayer = PlayerPosition - MuzzlePosition;
            return Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
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
        /// Fires from somewhere other than the muzzle — used by attacks that come from above or
        /// along the ground rather than out of the boss's body.
        /// </summary>
        public void FireFrom(Vector2 origin, Vector2 direction)
        {
            _projectiles.Spawn(origin, direction);
        }

        /// <summary>Converts an angle in degrees, measured from world right, into a unit vector.</summary>
        public static Vector2 DirectionFromAngle(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}
