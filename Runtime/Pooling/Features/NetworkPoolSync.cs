using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Pooling
{
    /// <summary>
    /// Network synchronization layer for pooled objects.
    /// Provides events for spawn/despawn to sync across network.
    /// </summary>
    public static class NetworkPoolSync
    {
        private static readonly Dictionary<uint, NetworkPoolData> _networkData = new Dictionary<uint, NetworkPoolData>();
        private static readonly Dictionary<uint, PoolHandle<GameObject>> _networkIdToHandle = new Dictionary<uint, PoolHandle<GameObject>>();
        private static uint _nextNetworkId = 1;

        /// <summary>
        /// Event fired when a spawn should be synchronized to clients.
        /// </summary>
        public static Action<NetworkSpawnData> OnSpawnRequested;

        /// <summary>
        /// Event fired when a despawn should be synchronized to clients.
        /// </summary>
        public static Action<uint> OnDespawnRequested;

        /// <summary>
        /// Registers a pooled object for network synchronization.
        /// </summary>
        /// <param name="handle">Pool handle to register.</param>
        /// <param name="isServerAuthoritative">Whether server controls this object.</param>
        /// <param name="networkId">Optional custom network ID (auto-generated if 0).</param>
        /// <returns>Network ID for this object.</returns>
        public static uint Register(PoolHandle<GameObject> handle, bool isServerAuthoritative = true, uint networkId = 0)
        {
            if (!handle.IsValid) return 0;

            if (networkId == 0)
            {
                networkId = _nextNetworkId++;
            }

            var data = new NetworkPoolData
            {
                NetworkId = networkId,
                Handle = handle,
                IsServerAuthoritative = isServerAuthoritative
            };

            _networkData[handle.Id] = data;
            _networkIdToHandle[networkId] = handle;

            return networkId;
        }

        /// <summary>
        /// Unregisters a pooled object from network sync.
        /// </summary>
        public static void Unregister(PoolHandle<GameObject> handle)
        {
            if (_networkData.TryGetValue(handle.Id, out var data))
            {
                _networkIdToHandle.Remove(data.NetworkId);
                _networkData.Remove(handle.Id);
            }
        }

        /// <summary>
        /// Gets the network ID for a pool handle.
        /// </summary>
        public static uint GetNetworkId(PoolHandle<GameObject> handle)
        {
            return _networkData.TryGetValue(handle.Id, out var data) ? data.NetworkId : 0;
        }

        /// <summary>
        /// Gets the pool handle for a network ID.
        /// </summary>
        public static PoolHandle<GameObject> GetHandle(uint networkId)
        {
            return _networkIdToHandle.TryGetValue(networkId, out var handle) ? handle : PoolHandle<GameObject>.None;
        }

        /// <summary>
        /// Broadcasts a spawn event to clients.
        /// </summary>
        public static void BroadcastSpawn(PoolHandle<GameObject> handle, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var networkId = GetNetworkId(handle);
            if (networkId == 0) return;

            var spawnData = new NetworkSpawnData
            {
                NetworkId = networkId,
                PrefabId = prefab.GetInstanceID(),
                Position = position,
                Rotation = rotation
            };

            OnSpawnRequested?.Invoke(spawnData);
        }

        /// <summary>
        /// Broadcasts a despawn event to clients.
        /// </summary>
        public static void BroadcastDespawn(PoolHandle<GameObject> handle)
        {
            var networkId = GetNetworkId(handle);
            if (networkId == 0) return;

            OnDespawnRequested?.Invoke(networkId);
            Unregister(handle);
        }

        /// <summary>
        /// Clears all network pool data.
        /// </summary>
        public static void Clear()
        {
            _networkData.Clear();
            _networkIdToHandle.Clear();
        }

        private struct NetworkPoolData
        {
            public uint NetworkId;
            public PoolHandle<GameObject> Handle;
            public bool IsServerAuthoritative;
        }
    }

    /// <summary>
    /// Data for network spawn synchronization.
    /// </summary>
    [Serializable]
    public struct NetworkSpawnData
    {
        public uint NetworkId;
        public int PrefabId;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    // Extension methods for Pool
    public static partial class PoolNetworkExtensions
    {
        /// <summary>
        /// Spawns a GameObject and registers it for network sync.
        /// </summary>
        public static (PoolHandle<GameObject> Handle, uint NetworkId) SpawnNetworked(
            GameObject prefab, 
            Vector3 position, 
            Quaternion? rotation = null,
            bool isServerAuthoritative = true)
        {
            var handle = Pool.Spawn(prefab, position, rotation);
            var networkId = NetworkPoolSync.Register(handle, isServerAuthoritative);
            return (handle, networkId);
        }

        /// <summary>
        /// Despawns a networked GameObject and broadcasts to clients.
        /// </summary>
        public static void DespawnNetworked(PoolHandle<GameObject> handle)
        {
            NetworkPoolSync.BroadcastDespawn(handle);
            Pool.Despawn(handle);
        }
    }
}
