namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Interface for objects that can be pooled.
    /// Implement this to receive spawn/despawn callbacks.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// Use this to reset state and enable the object.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when the object is returned to the pool.
        /// Use this to cleanup and disable the object.
        /// </summary>
        void OnDespawn();
    }

    /// <summary>
    /// Interface for pool providers.
    /// Implement this to create custom pool implementations.
    /// </summary>
    /// <typeparam name="T">Type of objects in the pool.</typeparam>
    public interface IPoolProvider<T> where T : class
    {
        /// <summary>Gets an object from the pool.</summary>
        PoolHandle<T> Get();

        /// <summary>Returns an object to the pool.</summary>
        void Release(PoolHandle<T> handle);

        /// <summary>Pre-allocates objects in the pool.</summary>
        void Warmup(int count);

        /// <summary>Clears all objects from the pool.</summary>
        void Clear();

        /// <summary>Number of active (in-use) objects.</summary>
        int ActiveCount { get; }

        /// <summary>Number of available (pooled) objects.</summary>
        int AvailableCount { get; }
    }
}
