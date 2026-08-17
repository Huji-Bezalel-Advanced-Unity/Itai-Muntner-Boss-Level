using System.Collections.Generic;
using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Spawns and recycles one-shot particle bursts.
    /// </summary>
    /// <remarks>
    /// Pooled for the same reason projectiles are: impacts happen in the busiest moments of the
    /// fight, which is exactly when a burst of instantiation and garbage collection is least
    /// affordable.
    /// </remarks>
    [DisallowMultipleComponent]
    public class VfxPool : MonoBehaviour
    {
        [SerializeField] private VfxBurst prefab;

        [SerializeField, Min(1)] private int initialSize = 8;

        [Tooltip("Optional parent for the spawned instances, purely to keep the hierarchy tidy.")]
        [SerializeField] private Transform container;

        private Pool<VfxBurst> _pool;
        private readonly List<VfxBurst> _active = new List<VfxBurst>();

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(VfxPool)} has no burst prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new Pool<VfxBurst>(prefab, container != null ? container : transform, initialSize);
        }

        /// <summary>Plays a burst at a position, tinted to whatever it is coming from.</summary>
        public void Play(Vector2 position, Color colour)
        {
            if (!enabled)
            {
                return;
            }

            var burst = _pool.Get();

            _active.Add(burst);
            burst.Play(this, position, colour);
        }

        /// <summary>Takes a burst out of play. Called by the burst once its particles have died.</summary>
        public void Despawn(VfxBurst burst)
        {
            if (burst == null)
            {
                return;
            }

            _active.Remove(burst);
            _pool.Return(burst);
        }
    }
}
