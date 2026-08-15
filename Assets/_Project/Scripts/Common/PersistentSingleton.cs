using UnityEngine;

namespace BossLevel.Common
{
    /// <summary>
    /// Base class for a service that must exist exactly once and survive scene loads.
    /// </summary>
    /// <remarks>
    /// Unlike the usual lazy singleton, this never creates itself on first access. Services are
    /// placed in the Bootstrap scene so they have exactly one creation site and one lifetime;
    /// an access before bootstrap has run is a wiring bug, and it is better to see an error
    /// naming the problem than to have a half-configured object quietly appear.
    /// <para>
    /// The <c>where T : PersistentSingleton&lt;T&gt;</c> constraint is what makes the cast in
    /// <see cref="Awake"/> safe: a subclass cannot accidentally declare itself as another type's
    /// singleton.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The concrete subclass. Write it as <c>class Foo : PersistentSingleton&lt;Foo&gt;</c>.</typeparam>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        private static T _instance;

        /// <summary>
        /// The single live instance, or <c>null</c> with an error logged if bootstrap has not
        /// created it yet.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError(
                        $"{typeof(T).Name} was accessed before it existed. " +
                        "Make sure the Bootstrap scene runs first and contains it.");
                }

                return _instance;
            }
        }

        /// <summary>Whether the instance exists, without logging if it does not.</summary>
        public static bool Exists => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // A second copy arrived, usually from re-entering the Bootstrap scene.
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            // DontDestroyOnLoad only applies to root objects, and warns if given a child.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            // Only clear the static if we are the live instance — a duplicate destroying itself
            // in Awake must not blank out the real one.
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
