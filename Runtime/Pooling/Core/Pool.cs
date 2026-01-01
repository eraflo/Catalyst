using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Static facade for the pooling system.
    /// Provides unified access to generic and prefab pools.
    /// </summary>
    public static class Pool
    {
        private static readonly Dictionary<Type, object> _genericPools = new Dictionary<Type, object>();
        private static readonly Dictionary<int, PrefabPool> _prefabPools = new Dictionary<int, PrefabPool>();
        private static readonly ConcurrentQueue<Action> _pendingOperations = new ConcurrentQueue<Action>();
        private static readonly object _lock = new object();
        
        private static bool _initialized;
        private static PoolMetrics _metrics;

        /// <summary>Pool system metrics.</summary>
        public static PoolMetrics Metrics => _metrics ??= new PoolMetrics();

        private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;
        private static bool IsMainThread => PackageRuntime.IsMainThread;

        #region Initialization

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _metrics = new PoolMetrics();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    Shutdown();
                }
            };
        }
#endif

        private static void Shutdown()
        {
            ClearAll();
            _initialized = false;
            _metrics = null;
        }

        #endregion

        #region Generic Pool

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <typeparam name="T">Type of object to get.</typeparam>
        /// <returns>Handle to the pooled object.</returns>
        public static PoolHandle<T> Get<T>() where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            var handle = pool.Get();
            Metrics.RecordSpawn();
            return handle;
        }

        /// <summary>
        /// Gets an object from the pool with initialization action.
        /// </summary>
        public static PoolHandle<T> Get<T>(Action<T> initialize) where T : class, new()
        {
            var handle = Get<T>();
            initialize?.Invoke(handle.Instance);
            return handle;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="handle">Handle from Get().</param>
        public static void Release<T>(PoolHandle<T> handle) where T : class, new()
        {
            if (!handle.IsValid) return;

            if (!IsMainThread)
            {
                _pendingOperations.Enqueue(() => ReleaseInternal(handle));
                return;
            }

            ReleaseInternal(handle);
        }

        private static void ReleaseInternal<T>(PoolHandle<T> handle) where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            pool.Release(handle);
            Metrics.RecordDespawn();
        }

        /// <summary>
        /// Pre-allocates objects in a generic pool.
        /// </summary>
        public static void Warmup<T>(int count) where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            pool.Warmup(count);
        }

        /// <summary>
        /// Clears a specific generic pool.
        /// </summary>
        public static void Clear<T>() where T : class, new()
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (_genericPools.TryGetValue(typeof(T), out var pool))
                    {
                        ((GenericPool<T>)pool).Clear();
                        _genericPools.Remove(typeof(T));
                    }
                }
            }
            else
            {
                if (_genericPools.TryGetValue(typeof(T), out var pool))
                {
                    ((GenericPool<T>)pool).Clear();
                    _genericPools.Remove(typeof(T));
                }
            }
        }

        private static GenericPool<T> GetOrCreateGenericPool<T>() where T : class, new()
        {
            var type = typeof(T);

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (!_genericPools.TryGetValue(type, out var pool))
                    {
                        pool = new GenericPool<T>();
                        _genericPools[type] = pool;
                    }
                    return (GenericPool<T>)pool;
                }
            }

            if (!_genericPools.TryGetValue(type, out var existingPool))
            {
                existingPool = new GenericPool<T>();
                _genericPools[type] = existingPool;
            }
            return (GenericPool<T>)existingPool;
        }

        #endregion

        #region Prefab Pool

        /// <summary>
        /// Spawns a GameObject from the pool.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation (default identity).</param>
        /// <param name="parent">Optional parent transform.</param>
        /// <returns>Handle to the spawned GameObject.</returns>
        public static PoolHandle<GameObject> Spawn(GameObject prefab, Vector3 position, Quaternion? rotation = null, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[Pool] Cannot spawn null prefab");
                return PoolHandle<GameObject>.None;
            }

            var pool = GetOrCreatePrefabPool(prefab);
            var handle = pool.Spawn(position, rotation ?? Quaternion.identity, parent);
            Metrics.RecordSpawn();
            return handle;
        }

        /// <summary>
        /// Spawns a GameObject from the pool at origin.
        /// </summary>
        public static PoolHandle<GameObject> Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero);
        }

        /// <summary>
        /// Spawns a GameObject that auto-despawns after duration.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <param name="position">World position.</param>
        /// <param name="duration">Time in seconds before auto-despawn.</param>
        /// <param name="rotation">World rotation (default identity).</param>
        /// <returns>Handle to the spawned GameObject.</returns>
        public static PoolHandle<GameObject> SpawnTimed(GameObject prefab, Vector3 position, float duration, Quaternion? rotation = null)
        {
            var handle = Spawn(prefab, position, rotation);
            
            if (handle.IsValid)
            {
                // Use Timer system for auto-release
                Timers.Timer.Delay(duration, () => Despawn(handle));
            }
            
            return handle;
        }

        /// <summary>
        /// Returns a GameObject to the pool.
        /// </summary>
        /// <param name="handle">Handle from Spawn().</param>
        public static void Despawn(PoolHandle<GameObject> handle)
        {
            if (!handle.IsValid) return;

            if (!IsMainThread)
            {
                _pendingOperations.Enqueue(() => DespawnInternal(handle));
                return;
            }

            DespawnInternal(handle);
        }

        private static void DespawnInternal(PoolHandle<GameObject> handle)
        {
            if (!_prefabPools.TryGetValue(handle.PoolId, out var pool))
            {
                Debug.LogWarning($"[Pool] Unknown pool for handle: {handle.PoolId}");
                return;
            }

            pool.Despawn(handle);
            Metrics.RecordDespawn();
        }

        /// <summary>
        /// Pre-allocates GameObjects in a prefab pool.
        /// </summary>
        public static void Warmup(GameObject prefab, int count)
        {
            if (prefab == null) return;
            var pool = GetOrCreatePrefabPool(prefab);
            pool.Warmup(count);
        }

        /// <summary>
        /// Clears a specific prefab pool.
        /// </summary>
        public static void Clear(GameObject prefab)
        {
            if (prefab == null) return;
            var poolId = prefab.GetInstanceID();

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (_prefabPools.TryGetValue(poolId, out var pool))
                    {
                        pool.Clear();
                        _prefabPools.Remove(poolId);
                    }
                }
            }
            else
            {
                if (_prefabPools.TryGetValue(poolId, out var pool))
                {
                    pool.Clear();
                    _prefabPools.Remove(poolId);
                }
            }
        }

        private static PrefabPool GetOrCreatePrefabPool(GameObject prefab)
        {
            var poolId = prefab.GetInstanceID();

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (!_prefabPools.TryGetValue(poolId, out var pool))
                    {
                        pool = new PrefabPool(prefab, poolId);
                        _prefabPools[poolId] = pool;
                    }
                    return pool;
                }
            }

            if (!_prefabPools.TryGetValue(poolId, out var existingPool))
            {
                existingPool = new PrefabPool(prefab, poolId);
                _prefabPools[poolId] = existingPool;
            }
            return existingPool;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Clears all pools.
        /// </summary>
        public static void ClearAll()
        {
            if (IsThreadSafe)
            {
                lock (_lock) ClearAllInternal();
            }
            else
            {
                ClearAllInternal();
            }
        }

        private static void ClearAllInternal()
        {
            foreach (var pool in _prefabPools.Values)
            {
                pool.Clear();
            }
            _prefabPools.Clear();
            _genericPools.Clear();
            
            while (_pendingOperations.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Processes pending operations from other threads.
        /// Called automatically by Unity's update loop.
        /// </summary>
        internal static void ProcessPendingOperations()
        {
            while (_pendingOperations.TryDequeue(out var operation))
            {
                operation?.Invoke();
            }
        }

        /// <summary>
        /// Gets info about all active pools for debugging.
        /// </summary>
        public static List<PoolDebugInfo> GetPoolDebugInfo()
        {
            var result = new List<PoolDebugInfo>();

            if (IsThreadSafe) lock (_lock) CollectDebugInfo(result);
            else CollectDebugInfo(result);

            return result;
        }

        private static void CollectDebugInfo(List<PoolDebugInfo> result)
        {
            foreach (var kvp in _prefabPools)
            {
                result.Add(new PoolDebugInfo
                {
                    PoolId = kvp.Key,
                    PoolName = kvp.Value.PrefabName,
                    Type = PoolType.Prefab,
                    ActiveCount = kvp.Value.ActiveCount,
                    AvailableCount = kvp.Value.AvailableCount
                });
            }

            foreach (var kvp in _genericPools)
            {
                var provider = kvp.Value as IPoolProvider<object>;
                result.Add(new PoolDebugInfo
                {
                    PoolId = kvp.Key.GetHashCode(),
                    PoolName = kvp.Key.Name,
                    Type = PoolType.Generic,
                    ActiveCount = (int)(kvp.Value.GetType().GetProperty("ActiveCount")?.GetValue(kvp.Value) ?? 0),
                    AvailableCount = (int)(kvp.Value.GetType().GetProperty("AvailableCount")?.GetValue(kvp.Value) ?? 0)
                });
            }
        }

        #endregion
    }

    /// <summary>Type of pool.</summary>
    public enum PoolType
    {
        Generic,
        Prefab
    }

    /// <summary>Debug info for a pool.</summary>
    public struct PoolDebugInfo
    {
        public int PoolId;
        public string PoolName;
        public PoolType Type;
        public int ActiveCount;
        public int AvailableCount;
    }
}
