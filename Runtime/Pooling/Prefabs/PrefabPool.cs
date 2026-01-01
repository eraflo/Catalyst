using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Pool for prefab GameObjects.
    /// Handles instantiation, activation, and parenting.
    /// </summary>
    public class PrefabPool
    {
        private readonly GameObject _prefab;
        private readonly int _poolId;
        private readonly Stack<GameObject> _available = new Stack<GameObject>();
        private readonly Dictionary<uint, GameObject> _active = new Dictionary<uint, GameObject>();
        private readonly object _lock = new object();
        
        private Transform _poolRoot;
        private uint _nextId = 1;
        private int _peakActiveCount;

        /// <summary>Name of the prefab.</summary>
        public string PrefabName => _prefab != null ? _prefab.name : "Unknown";

        /// <summary>Pool identifier.</summary>
        public int PoolId => _poolId;

        /// <summary>Number of active objects.</summary>
        public int ActiveCount
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _active.Count;
                return _active.Count;
            }
        }

        /// <summary>Number of available objects.</summary>
        public int AvailableCount
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _available.Count;
                return _available.Count;
            }
        }

        /// <summary>Peak active count.</summary>
        public int PeakActiveCount => _peakActiveCount;

        private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

        public PrefabPool(GameObject prefab, int poolId)
        {
            _prefab = prefab;
            _poolId = poolId;
            CreatePoolRoot();
        }

        private void CreatePoolRoot()
        {
            var rootGo = new GameObject($"[Pool] {PrefabName}");
            _poolRoot = rootGo.transform;
            Object.DontDestroyOnLoad(rootGo);
        }

        /// <summary>
        /// Spawns an object from the pool.
        /// </summary>
        public PoolHandle<GameObject> Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (IsThreadSafe)
            {
                lock (_lock) return SpawnInternal(position, rotation, parent);
            }
            return SpawnInternal(position, rotation, parent);
        }

        private PoolHandle<GameObject> SpawnInternal(Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject instance;

            if (_available.Count > 0)
            {
                instance = _available.Pop();
            }
            else
            {
                instance = Object.Instantiate(_prefab, _poolRoot);
                
                // Add PooledObject component if not present
                if (!instance.TryGetComponent<PooledObject>(out _))
                {
                    instance.AddComponent<PooledObject>();
                }
            }

            // Configure transform
            instance.transform.SetParent(parent ?? _poolRoot);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.SetActive(true);

            var id = _nextId++;
            _active[id] = instance;

            if (_active.Count > _peakActiveCount)
                _peakActiveCount = _active.Count;

            // Initialize PooledObject component
            var pooledObj = instance.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.Initialize(id, _poolId);
            }

            // Call IPoolable.OnSpawn on all components
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnSpawn();
            }

            return new PoolHandle<GameObject>(id, instance, _poolId, Time.realtimeSinceStartup);
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public void Despawn(PoolHandle<GameObject> handle)
        {
            if (!handle.IsValid) return;

            if (IsThreadSafe)
            {
                lock (_lock) DespawnInternal(handle);
            }
            else
            {
                DespawnInternal(handle);
            }
        }

        private void DespawnInternal(PoolHandle<GameObject> handle)
        {
            if (!_active.TryGetValue(handle.Id, out var instance))
            {
                Debug.LogWarning($"[PrefabPool] Attempted to despawn unknown handle: {handle.Id}");
                return;
            }

            _active.Remove(handle.Id);

            if (instance == null)
            {
                // Object was destroyed
                return;
            }

            // Call IPoolable.OnDespawn on all components
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnDespawn();
            }

            // Deactivate and reparent
            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);

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
                var instance = Object.Instantiate(_prefab, _poolRoot);
                
                if (!instance.TryGetComponent<PooledObject>(out _))
                {
                    instance.AddComponent<PooledObject>();
                }

                instance.SetActive(false);

                // Call OnDespawn for initial state
                var poolables = instance.GetComponentsInChildren<IPoolable>(true);
                foreach (var poolable in poolables)
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
            // Destroy active objects
            foreach (var instance in _active.Values)
            {
                if (instance != null)
                    Object.Destroy(instance);
            }
            _active.Clear();

            // Destroy available objects
            while (_available.Count > 0)
            {
                var instance = _available.Pop();
                if (instance != null)
                    Object.Destroy(instance);
            }

            // Destroy pool root
            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
        }
    }
}
