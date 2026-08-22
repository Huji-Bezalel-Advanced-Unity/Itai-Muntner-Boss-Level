using System;
using System.Collections.Generic;
using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// Spawns and recycles volcanic vents.
    /// </summary>
    /// <remarks>
    /// Deliberately a sibling of <see cref="ProjectilePool"/> and <see cref="MinionPool"/> rather
    /// than a shared generic base. Unity cannot attach a generic MonoBehaviour, so a common base
    /// would have to be non-generic and cast at every call site — trading a little duplication
    /// for indirection the reader has to unpick. Three short concrete pools read better than one
    /// clever one.
    /// </remarks>
    [DisallowMultipleComponent]
    public class VolcanoPool : MonoBehaviour
    {
        [SerializeField] private VolcanoHazard prefab;

        [SerializeField, Min(1)] private int initialSize = 6;

        [Tooltip("Optional parent for the spawned instances, purely to keep the hierarchy tidy.")]
        [SerializeField] private Transform container;

        private Pool<VolcanoHazard> _pool;
        private readonly List<VolcanoHazard> _active = new List<VolcanoHazard>();

        /// <summary>How many vents are currently warning or erupting.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Raised with the position where a vent began smouldering.</summary>
        /// <remarks>
        /// Announced rather than acted upon, so the presentation lives in the feel layer. It is
        /// also the only way a vent can reach the effect pools at all: a prefab cannot hold a
        /// reference to a scene object, and the pools are scene objects.
        /// </remarks>
        public event Action<Vector2> Opened;

        /// <summary>Raised at the moment a vent actually erupts.</summary>
        public event Action<Vector2> Erupted;

        /// <summary>Called by a vent when it fires, so the pool can announce it.</summary>
        public void NotifyErupted(Vector2 position)
        {
            Erupted?.Invoke(position);
        }

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(VolcanoPool)} has no hazard prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new Pool<VolcanoHazard>(prefab, container != null ? container : transform, initialSize);
        }

        /// <summary>Opens a vent at <paramref name="groundPosition"/> and starts its warning.</summary>
        public VolcanoHazard Spawn(Vector2 groundPosition)
        {
            var hazard = _pool.Get();

            _active.Add(hazard);
            hazard.Trigger(this, groundPosition);

            Opened?.Invoke(groundPosition);

            return hazard;
        }

        /// <summary>Takes a vent out of play. Called by the vent itself once it is spent.</summary>
        public void Despawn(VolcanoHazard hazard)
        {
            if (hazard == null)
            {
                return;
            }

            _active.Remove(hazard);
            _pool.Return(hazard);
        }

        /// <summary>
        /// Closes every vent still pending. Used when the fight ends, so a warning that has
        /// already been given cannot erupt after the result is settled.
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
