using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossLevel.Common
{
    /// <summary>
    /// Reuses component instances instead of creating and destroying them, growing on demand.
    /// </summary>
    /// <remarks>
    /// This is a plain C# class rather than a <see cref="MonoBehaviour"/> on purpose. Unity
    /// cannot attach generic components through the Inspector, so a generic MonoBehaviour pool
    /// could never have its prefab or size assigned and would fail the first time it was used.
    /// Instead, a small non-generic component owns a <c>Pool</c> and exposes a concrete API.
    /// <para>
    /// A boss fight spawns hundreds of projectiles; instantiating each one produces garbage and
    /// a frame hitch at exactly the moment the player is least able to forgive it.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Component on the pooled prefab. Must be able to reset itself.</typeparam>
    public class Pool<T> where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _idle;

        /// <summary>Instances currently idle and ready to hand out.</summary>
        public int IdleCount => _idle.Count;

        /// <summary>
        /// Instances created over this pool's lifetime. If this climbs well above the initial
        /// size during play, the initial size is set too low.
        /// </summary>
        public int CreatedCount { get; private set; }

        /// <param name="prefab">Prefab to clone. Must not be null.</param>
        /// <param name="parent">Optional transform to keep clones tidy in the hierarchy.</param>
        /// <param name="initialSize">How many instances to create up front.</param>
        public Pool(T prefab, Transform parent, int initialSize)
        {
            if (prefab == null)
            {
                throw new System.ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _parent = parent;
            _idle = new Stack<T>(Mathf.Max(initialSize, 1));

            Grow(initialSize);
        }

        /// <summary>
        /// Takes an instance from the pool, creating one if none are idle. The instance is
        /// active and has had <see cref="IPoolable.OnSpawn"/> called on it.
        /// </summary>
        public T Get()
        {
            if (_idle.Count == 0)
            {
                Grow(1);
            }

            var instance = _idle.Pop();
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool. Safe to call on an instance that is already pooled,
        /// which happens when a projectile is hit and expires on the same frame.
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null || !instance.gameObject.activeSelf)
            {
                return;
            }

            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            _idle.Push(instance);
        }

        private void Grow(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(_prefab, _parent);
                instance.gameObject.SetActive(false);
                _idle.Push(instance);
                CreatedCount++;
            }
        }
    }
}
