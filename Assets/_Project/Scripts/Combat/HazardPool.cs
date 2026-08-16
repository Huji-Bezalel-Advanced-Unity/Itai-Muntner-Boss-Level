using System.Collections.Generic;
using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// Spawns and recycles ground hazards.
    /// </summary>
    /// <remarks>
    /// Deliberately a sibling of <see cref="ProjectilePool"/> rather than a shared generic base.
    /// Unity cannot attach a generic MonoBehaviour, so a shared base would have to be non-generic
    /// and cast, trading a little duplication for indirection the reader has to unpick. Two short
    /// concrete pools are easier to read than one clever one.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HazardPool : MonoBehaviour
    {
        [SerializeField] private GroundHazard prefab;

        [SerializeField, Min(1)] private int initialSize = 8;

        [Tooltip("Optional parent for the spawned instances, purely to keep the hierarchy tidy.")]
        [SerializeField] private Transform container;

        private Pool<GroundHazard> _pool;
        private readonly List<GroundHazard> _active = new List<GroundHazard>();

        /// <summary>How many hazards are currently warning or striking.</summary>
        public int ActiveCount => _active.Count;

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(HazardPool)} has no hazard prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new Pool<GroundHazard>(prefab, container != null ? container : transform, initialSize);
        }

        /// <summary>Places a hazard at <paramref name="position"/> and starts its warning.</summary>
        public GroundHazard Spawn(Vector2 position)
        {
            var hazard = _pool.Get();

            _active.Add(hazard);
            hazard.Trigger(this, position);

            return hazard;
        }

        /// <summary>Takes a hazard out of play. Called by the hazard itself once it is spent.</summary>
        public void Despawn(GroundHazard hazard)
        {
            if (hazard == null)
            {
                return;
            }

            _active.Remove(hazard);
            _pool.Return(hazard);
        }

        /// <summary>
        /// Clears every hazard still pending, so a warning left over from the previous phase
        /// cannot strike during a transition in which the boss is supposed to be harmless.
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
