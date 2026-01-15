using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Eraflo.Catalyst;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// API for the pooling system.
    /// Can be used as a service via Service Locator.
    /// Provides unified access to generic and prefab pools.
    /// </summary>
    [Service(Priority = 14)]
    public class Pool : IGameService, IUpdatable
    {
        private readonly Dictionary<Type, object> _genericPools = new Dictionary<Type, object>();
        private readonly Dictionary<int, PrefabPool> _prefabPools = new Dictionary<int, PrefabPool>();
        private readonly Dictionary<object, object> _instanceToHandle = new Dictionary<object, object>(); // Real tracking
        private readonly ConcurrentQueue<Action> _pendingOperations = new ConcurrentQueue<Action>();
        private readonly object _lock = new object();
        
        private bool _initialized;
        private PoolMetrics _metrics;

        /// <summary>Pool system metrics.</summary>
        public PoolMetrics Metrics => _metrics ??= new PoolMetrics();

        private bool IsThreadSafe => PackageRuntime.IsThreadSafe;
        private bool IsMainThread => PackageRuntime.IsMainThread;

        #region IGameService & Lifecycle

        void IGameService.Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _metrics = new PoolMetrics();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
#endif
        }

        void IGameService.Shutdown()
        {
            ClearAllInternal();
            _initialized = false;
            _metrics = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
#endif
        }

        void IUpdatable.OnUpdate()
        {
            ProcessPendingOperations();
        }

#if UNITY_EDITOR
        private void OnEditorPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                ((IGameService)this).Shutdown();
            }
        }
#endif

        #endregion


        #region Generic Pool Methods

        public PoolHandle<T> GetFromPool<T>() where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            var handle = pool.Get();
            _instanceToHandle[handle.Instance] = handle;
            Metrics.RecordSpawn();
            return handle;
        }

        public PoolHandle<T> GetFromPool<T>(Action<T> initialize) where T : class, new()
        {
            var handle = GetFromPool<T>();
            initialize?.Invoke(handle.Instance);
            return handle;
        }

        public void ReleaseToPool<T>(PoolHandle<T> handle) where T : class, new()
        {
            if (!handle.IsValid) return;

            if (!IsMainThread)
            {
                _pendingOperations.Enqueue(() => ReleaseInternal(handle));
                return;
            }

            ReleaseInternal(handle);
        }

        private void ReleaseInternal<T>(PoolHandle<T> handle) where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            pool.Release(handle);
            Metrics.RecordDespawn();
        }

        public void WarmupPool<T>(int count) where T : class, new()
        {
            var pool = GetOrCreateGenericPool<T>();
            pool.Warmup(count);
        }

        public void ClearPool<T>() where T : class, new()
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

        public object GetFromPoolDynamic(Type type)
        {
            var method = GetType().GetMethod("GetOrCreateGenericPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generic = method.MakeGenericMethod(type);
            var pool = generic.Invoke(this, null);
            
            var getMethod = pool.GetType().GetMethod("Get");
            var handle = getMethod.Invoke(pool, null);
            var instance = handle.GetType().GetProperty("Instance").GetValue(handle);
            
            _instanceToHandle[instance] = handle;
            return instance;
        }

        public void DespawnDynamic(object instance)
        {
            if (instance == null) return;
            
            if (_instanceToHandle.TryGetValue(instance, out var handle))
            {
                if (instance is GameObject go)
                {
                    DespawnObject((PoolHandle<GameObject>)handle);
                }
                else
                {
                    var type = instance.GetType();
                    var pool = GetOrCreateGenericPoolDynamic(type);
                    var releaseMethod = pool.GetType().GetMethod("Release", new[] { handle.GetType() });
                    releaseMethod?.Invoke(pool, new[] { handle });
                    _instanceToHandle.Remove(instance);
                }
            }
        }

        private object GetOrCreateGenericPoolDynamic(Type type)
        {
            var method = GetType().GetMethod("GetOrCreateGenericPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generic = method.MakeGenericMethod(type);
            return generic.Invoke(this, null);
        }

        private GenericPool<T> GetOrCreateGenericPool<T>() where T : class, new()
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

        #region Prefab Pool Methods

        public PoolHandle<GameObject> SpawnObject(GameObject prefab, Vector3 position, Quaternion? rotation = null, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[Pool] Cannot spawn null prefab");
                return PoolHandle<GameObject>.None;
            }

            var pool = GetOrCreatePrefabPool(prefab);
            var handle = pool.Spawn(position, rotation ?? Quaternion.identity, parent);
            _instanceToHandle[handle.Instance] = handle;
            Metrics.RecordSpawn();
            return handle;
        }

        public PoolHandle<GameObject> SpawnObjectTimed(GameObject prefab, Vector3 position, float duration, Quaternion? rotation = null)
        {
            var handle = SpawnObject(prefab, position, rotation);
            
            if (handle.IsValid)
            {
                // Use the new Timer service for auto-release
                App.Get<Timers.Timer>().CreateDelay(duration, () => DespawnObject(handle));
            }
            
            return handle;
        }

        public void DespawnObject(PoolHandle<GameObject> handle)
        {
            if (!handle.IsValid) return;

            if (!IsMainThread)
            {
                _pendingOperations.Enqueue(() => DespawnInternal(handle));
                return;
            }

            DespawnInternal(handle);
        }

        private void DespawnInternal(PoolHandle<GameObject> handle)
        {
            if (!_prefabPools.TryGetValue(handle.PoolId, out var pool))
            {
                Debug.LogWarning($"[Pool] Unknown pool for handle: {handle.PoolId}");
                return;
            }

            _instanceToHandle.Remove(handle.Instance);
            pool.Despawn(handle);
            Metrics.RecordDespawn();
        }

        public void WarmupObject(GameObject prefab, int count)
        {
            if (prefab == null) return;
            var pool = GetOrCreatePrefabPool(prefab);
            pool.Warmup(count);
        }

        public void ClearObject(GameObject prefab)
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

        public GameObject ResolvePrefab(string prefabName)
        {
            foreach (var pool in _prefabPools.Values)
            {
                if (pool.PrefabName == prefabName) return pool.Prefab;
            }
            
            // Fallback: try loading from Resources if matching name
            return Resources.Load<GameObject>(prefabName);
        }

        private PrefabPool GetOrCreatePrefabPool(GameObject prefab)
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

        #region Utility Methods

        public void ClearAllPools()
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

        private void ClearAllInternal()
        {
            foreach (var pool in _prefabPools.Values)
            {
                pool.Clear();
            }
            _prefabPools.Clear();
            _genericPools.Clear();
            
            while (_pendingOperations.TryDequeue(out _)) { }
        }

        public void ProcessPendingOperations()
        {
            while (_pendingOperations.TryDequeue(out var operation))
            {
                operation?.Invoke();
            }
        }

        public List<PoolDebugInfo> GetDebugInfo()
        {
            var result = new List<PoolDebugInfo>();

            if (IsThreadSafe) lock (_lock) CollectDebugInfo(result);
            else CollectDebugInfo(result);

            return result;
        }

        private void CollectDebugInfo(List<PoolDebugInfo> result)
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
