using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Component attached to pooled GameObjects.
    /// Tracks pool membership and provides lifecycle callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour, IPoolable
    {
        private uint _handleId;
        private int _poolId;
        private float _spawnTime;
        private bool _isActive;

        /// <summary>Handle ID of this pooled object.</summary>
        public uint HandleId => _handleId;

        /// <summary>Pool ID this object belongs to.</summary>
        public int PoolId => _poolId;

        /// <summary>Time when this object was spawned.</summary>
        public float SpawnTime => _spawnTime;

        /// <summary>Time since spawn in seconds.</summary>
        public float TimeSinceSpawn => _isActive ? Time.realtimeSinceStartup - _spawnTime : 0f;

        /// <summary>Whether this object is currently spawned (active in pool).</summary>
        public bool IsSpawned => _isActive;

        /// <summary>
        /// Initializes this pooled object. Called by PrefabPool.
        /// </summary>
        internal void Initialize(uint handleId, int poolId)
        {
            _handleId = handleId;
            _poolId = poolId;
            _spawnTime = Time.realtimeSinceStartup;
            _isActive = true;
        }

        /// <summary>
        /// Called when spawned from pool.
        /// </summary>
        public virtual void OnSpawn()
        {
            _isActive = true;
        }

        /// <summary>
        /// Called when returned to pool.
        /// </summary>
        public virtual void OnDespawn()
        {
            _isActive = false;
            _handleId = 0;
        }

        /// <summary>
        /// Returns this object to its pool.
        /// </summary>
        public void Despawn()
        {
            if (_isActive && _poolId != 0)
            {
                var handle = new PoolHandle<GameObject>(_handleId, gameObject, _poolId, _spawnTime);
                Pool.Despawn(handle);
            }
        }

        /// <summary>
        /// Returns this object to its pool after a delay.
        /// </summary>
        public void DespawnAfter(float delay)
        {
            if (_isActive)
            {
                var handle = new PoolHandle<GameObject>(_handleId, gameObject, _poolId, _spawnTime);
                Timers.Timer.Delay(delay, () =>
                {
                    if (_isActive) // Check if still active when delay completes
                    {
                        Pool.Despawn(handle);
                    }
                });
            }
        }

        private void OnDestroy()
        {
            // Object was destroyed outside of pool system
            _isActive = false;
        }
    }
}
