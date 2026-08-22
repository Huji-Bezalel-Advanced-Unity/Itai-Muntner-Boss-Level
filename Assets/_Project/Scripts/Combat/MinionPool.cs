using System;
using System.Collections.Generic;
using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Combat
{
    /// <summary>
    /// Spawns and recycles the boss's minions.
    /// </summary>
    /// <remarks>
    /// <see cref="ActiveCount"/> is the interesting part: the boss reads it before deciding to
    /// summon again, so the arena fills to a pressure rather than to a wall.
    /// </remarks>
    [DisallowMultipleComponent]
    public class MinionPool : MonoBehaviour
    {
        [SerializeField] private Minion prefab;

        [SerializeField, Min(1)] private int initialSize = 6;

        [Tooltip("Optional parent for the spawned instances, purely to keep the hierarchy tidy.")]
        [SerializeField] private Transform container;

        private Pool<Minion> _pool;
        private readonly List<Minion> _active = new List<Minion>();

        /// <summary>How many minions are currently hunting the player.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Raised with the position where a minion appeared.</summary>
        /// <remarks>
        /// Announced rather than acted upon, so the presentation lives in the feel layer and
        /// this stays a pool. It also avoids the combat code depending on the effects code,
        /// which already depends on combat.
        /// </remarks>
        public event Action<Vector2> Spawned;

        /// <summary>Raised with the position where a minion was killed or burst.</summary>
        public event Action<Vector2> Despawned;

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(MinionPool)} has no minion prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new Pool<Minion>(prefab, container != null ? container : transform, initialSize);
        }

        /// <summary>Releases a minion at <paramref name="position"/> to hunt <paramref name="target"/>.</summary>
        public Minion Spawn(Vector2 position, ITarget target)
        {
            var minion = _pool.Get();

            _active.Add(minion);
            minion.Launch(this, target, position);

            Spawned?.Invoke(position);

            return minion;
        }

        /// <summary>Takes a minion out of play. Called by the minion when it dies or bursts.</summary>
        public void Despawn(Minion minion)
        {
            if (minion == null)
            {
                return;
            }

            // Read before returning, because a pooled instance is moved before its next use.
            var restingPlace = (Vector2)minion.transform.position;

            _active.Remove(minion);
            _pool.Return(minion);

            Despawned?.Invoke(restingPlace);
        }

        /// <summary>
        /// Clears the arena of minions. Used when the fight ends, so nothing left hunting can
        /// take away a result that has already been decided.
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
