using BossLevel.Common;
using UnityEngine;

namespace BossLevel.TestSupport
{
    /// <summary>
    /// A minimal poolable component used only by the pool tests. It records how often the pool
    /// called back into it, which is what those tests assert on.
    /// </summary>
    /// <remarks>
    /// This lives in <c>BossLevel.TestSupport</c> rather than alongside the tests because the
    /// test assembly is editor-only, and Unity refuses to add a component whose type comes from
    /// an editor assembly — <c>AddComponent</c> fails with "it is an editor script". Test
    /// support is therefore a normal runtime assembly, kept out of player builds by the
    /// <c>UNITY_INCLUDE_TESTS</c> define constraint instead.
    /// </remarks>
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
