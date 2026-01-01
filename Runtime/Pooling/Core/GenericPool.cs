using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Thread-safe generic object pool.
    /// Supports any class type with parameterless constructor.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool.</typeparam>
    public class GenericPool<T> : IPoolProvider<T> where T : class, new()
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly Dictionary<uint, T> _active = new Dictionary<uint, T>();
        private readonly object _lock = new object();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        
        private uint _nextId = 1;
        private int _peakActiveCount;

        private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

        /// <summary>Number of active (in-use) objects.</summary>
        public int ActiveCount
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _active.Count;
                return _active.Count;
            }
        }

        /// <summary>Number of available (pooled) objects.</summary>
        public int AvailableCount
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _available.Count;
                return _available.Count;
            }
        }

        /// <summary>Peak number of simultaneously active objects.</summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Creates a new generic pool.
        /// </summary>
        /// <param name="factory">Optional custom factory function.</param>
        /// <param name="onGet">Optional callback when object is retrieved.</param>
        /// <param name="onRelease">Optional callback when object is released.</param>
        /// <param name="initialCapacity">Number of objects to pre-allocate.</param>
        public GenericPool(
            Func<T> factory = null,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            int initialCapacity = 0)
        {
            _factory = factory ?? (() => new T());
            _onGet = onGet;
            _onRelease = onRelease;

            if (initialCapacity > 0)
            {
                Warmup(initialCapacity);
            }
        }

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        public PoolHandle<T> Get()
        {
            if (IsThreadSafe)
            {
                lock (_lock) return GetInternal();
            }
            return GetInternal();
        }

        private PoolHandle<T> GetInternal()
        {
            T instance;
            
            if (_available.Count > 0)
            {
                instance = _available.Pop();
            }
            else
            {
                instance = _factory();
            }

            var id = _nextId++;
            _active[id] = instance;
            
            if (_active.Count > _peakActiveCount)
                _peakActiveCount = _active.Count;

            // Call IPoolable.OnSpawn if implemented
            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            _onGet?.Invoke(instance);

            return new PoolHandle<T>(id, instance, 0, Time.realtimeSinceStartup);
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public void Release(PoolHandle<T> handle)
        {
            if (!handle.IsValid) return;

            if (IsThreadSafe)
            {
                lock (_lock) ReleaseInternal(handle);
            }
            else
            {
                ReleaseInternal(handle);
            }
        }

        private void ReleaseInternal(PoolHandle<T> handle)
        {
            if (!_active.TryGetValue(handle.Id, out var instance))
            {
                Debug.LogWarning($"[GenericPool] Attempted to release unknown handle: {handle.Id}");
                return;
            }

            _active.Remove(handle.Id);

            // Call IPoolable.OnDespawn if implemented
            if (instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            _onRelease?.Invoke(instance);

            _available.Push(instance);
        }

        /// <summary>
        /// Pre-allocates objects in the pool.
        /// </summary>
        public void Warmup(int count)
        {
            if (IsThreadSafe)
            {
                lock (_lock) WarmupInternal(count);
            }
            else
            {
                WarmupInternal(count);
            }
        }

        private void WarmupInternal(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = _factory();
                
                // Call OnDespawn to ensure proper initial state
                if (instance is IPoolable poolable)
                {
                    poolable.OnDespawn();
                }
                
                _available.Push(instance);
            }
        }

        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        public void Clear()
        {
            if (IsThreadSafe)
            {
                lock (_lock) ClearInternal();
            }
            else
            {
                ClearInternal();
            }
        }

        private void ClearInternal()
        {
            // Dispose active objects if they implement IDisposable
            foreach (var instance in _active.Values)
            {
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            }
            _active.Clear();

            // Dispose available objects
            while (_available.Count > 0)
            {
                var instance = _available.Pop();
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
