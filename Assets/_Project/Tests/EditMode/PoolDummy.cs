using BossLevel.Common;
using UnityEngine;

namespace BossLevel.Tests
{
    /// <summary>
    /// A minimal poolable component used only by <see cref="PoolTests"/>. It records how often
    /// the pool called back into it, which is what the tests assert on.
    /// </summary>
    public class PoolDummy : MonoBehaviour, IPoolable
    {
        public int SpawnCount { get; private set; }

        public int DespawnCount { get; private set; }

        public void OnSpawn()
        {
            SpawnCount++;
        }

        public void OnDespawn()
        {
            DespawnCount++;
        }
    }
}
