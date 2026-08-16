using System.Collections.Generic;
using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// Spawns and recycles projectiles. This is the concrete, non-generic component that owns a
    /// <see cref="Pool{T}"/> — the arrangement that lets the pool stay generic while still having
    /// its prefab and size assigned in the Inspector.
    /// </summary>
    /// <remarks>
    /// The player and the boss each get their own instance, configured with their own prefab
    /// and hit mask, rather than sharing one global pool.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;

        [Tooltip("Created up front. If CreatedCount climbs well past this during play, raise it.")]
        [SerializeField, Min(1)] private int initialSize = 32;

        [Tooltip("Optional parent for the spawned instances, purely to keep the hierarchy tidy.")]
        [SerializeField] private Transform container;

        private Pool<Projectile> _pool;
        private readonly List<Projectile> _active = new List<Projectile>();

        /// <summary>How many projectiles from this pool are currently in the air.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// How fast this pool's projectiles travel, so an attack can lead a moving target.
        /// </summary>
        public float ProjectileSpeed => prefab != null ? prefab.Speed : 0f;

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(ProjectilePool)} has no projectile prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new Pool<Projectile>(prefab, container != null ? container : transform, initialSize);
        }

        /// <summary>Fires a projectile from <paramref name="origin"/> along <paramref name="direction"/>.</summary>
        public Projectile Spawn(Vector2 origin, Vector2 direction)
        {
            var projectile = _pool.Get();

            _active.Add(projectile);
            projectile.Launch(this, origin, direction);

            return projectile;
        }

        /// <summary>Takes a projectile out of play. Called by the projectile itself.</summary>
        public void Despawn(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            _active.Remove(projectile);
            _pool.Return(projectile);
        }

        /// <summary>
        /// Clears every projectile still in the air. Used when a boss phase transition begins,
        /// so the player is not killed by shots from a phase that has already ended.
        /// </summary>
        public void DespawnAll()
        {
            // Iterate backwards because Despawn removes from the list being walked.
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                Despawn(_active[i]);
            }
        }
    }
}
