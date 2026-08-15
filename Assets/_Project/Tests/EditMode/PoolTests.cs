using BossLevel.Common;
using NUnit.Framework;
using UnityEngine;

namespace BossLevel.Tests
{
    /// <summary>
    /// Verifies that the pool actually reuses instances rather than quietly creating new ones,
    /// which is the whole point of having it, and that a double return cannot corrupt it.
    /// </summary>
    public class PoolTests
    {
        private const int InitialSize = 2;

        private GameObject _prefabObject;
        private Pool<PoolDummy> _pool;

        [SetUp]
        public void SetUp()
        {
            _prefabObject = new GameObject("PoolDummyPrefab");
            var prefab = _prefabObject.AddComponent<PoolDummy>();

            _pool = new Pool<PoolDummy>(prefab, null, InitialSize);
        }

        [TearDown]
        public void TearDown()
        {
            // Clones are parented to nothing, so collect them by type rather than by hierarchy.
            foreach (var dummy in Object.FindObjectsByType<PoolDummy>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(dummy.gameObject);
            }

            _prefabObject = null;
        }

        [Test]
        public void Constructor_CreatesTheRequestedNumberOfIdleInstances()
        {
            Assert.AreEqual(InitialSize, _pool.IdleCount);
            Assert.AreEqual(InitialSize, _pool.CreatedCount);
        }

        [Test]
        public void Constructor_RejectsANullPrefab()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new Pool<PoolDummy>(null, null, 1));
        }

        [Test]
        public void Get_ReturnsAnActiveInstanceAndAnnouncesTheSpawn()
        {
            var instance = _pool.Get();

            Assert.IsTrue(instance.gameObject.activeSelf);
            Assert.AreEqual(1, instance.SpawnCount);
            Assert.AreEqual(InitialSize - 1, _pool.IdleCount);
        }

        [Test]
        public void Return_DeactivatesTheInstanceAndAnnouncesTheDespawn()
        {
            var instance = _pool.Get();
            _pool.Return(instance);

            Assert.IsFalse(instance.gameObject.activeSelf);
            Assert.AreEqual(1, instance.DespawnCount);
            Assert.AreEqual(InitialSize, _pool.IdleCount);
        }

        [Test]
        public void Get_ReusesAReturnedInstanceInsteadOfCreatingANewOne()
        {
            var first = _pool.Get();
            _pool.Return(first);
            var second = _pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(InitialSize, _pool.CreatedCount, "The pool created a new instance instead of reusing.");
        }

        [Test]
        public void Get_GrowsThePoolWhenNoInstancesAreIdle()
        {
            for (var i = 0; i < InitialSize; i++)
            {
                _pool.Get();
            }

            var extra = _pool.Get();

            Assert.IsNotNull(extra);
            Assert.AreEqual(InitialSize + 1, _pool.CreatedCount);
        }

        [Test]
        public void Return_IgnoresAnInstanceThatIsAlreadyPooled()
        {
            var instance = _pool.Get();

            _pool.Return(instance);
            _pool.Return(instance);

            // A double return happens when a projectile is hit and expires on the same frame.
            // Without the guard the pool would hand the same instance out twice at once.
            Assert.AreEqual(InitialSize, _pool.IdleCount);
            Assert.AreEqual(1, instance.DespawnCount);
        }

        [Test]
        public void Return_IgnoresNull()
        {
            Assert.DoesNotThrow(() => _pool.Return(null));
            Assert.AreEqual(InitialSize, _pool.IdleCount);
        }
    }
}
