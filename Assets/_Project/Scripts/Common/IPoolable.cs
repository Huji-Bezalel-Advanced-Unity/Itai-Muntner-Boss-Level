namespace BossLevel.Common
{
    /// <summary>
    /// Implemented by components that are reused by a <see cref="Pool{T}"/> instead of being
    /// created and destroyed.
    /// </summary>
    /// <remarks>
    /// These are deliberately not called <c>Reset</c>. <see cref="UnityEngine.MonoBehaviour"/>
    /// already defines a <c>Reset</c> callback, which Unity invokes in the editor whenever a
    /// component is added or reset from its context menu — so pooling logic under that name
    /// would also run at authoring time, which is confusing and occasionally destructive.
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>
        /// Called just after the pool hands this instance out. Put it back into a known-good
        /// starting state here: clear velocity, reset timers, restore visuals.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called just before this instance returns to the pool. Release anything held while
        /// active here: stop coroutines, clear trails, unsubscribe from events.
        /// </summary>
        void OnDespawn();
    }
}
