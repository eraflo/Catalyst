using UnityEngine;
using Eraflo.UnityImportPackage.Networking;

namespace Eraflo.UnityImportPackage.Pooling
{
    /// <summary>
    /// Extension methods for networked pooling.
    /// </summary>
    public static class PoolNetworkExtensions
    {
        /// <summary>
        /// Spawns and registers for networking.
        /// </summary>
        public static (PoolHandle<GameObject> handle, uint networkId) SpawnNetworked(
            GameObject prefab, Vector3 position, Quaternion rotation = default, 
            bool serverAuth = true, NetworkTarget target = NetworkTarget.Clients)
        {
            var handle = Pool.Spawn(prefab, position, rotation);
            var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
            
            if (handler == null) return (handle, 0);
            
            var networkId = handler.Register(handle, serverAuth);
            handler.BroadcastSpawn(handle, prefab, position, rotation, target);
            
            return (handle, networkId);
        }

        /// <summary>
        /// Spawns locally without network broadcast.
        /// </summary>
        public static PoolHandle<GameObject> SpawnLocal(GameObject prefab, Vector3 position, Quaternion rotation = default)
        {
            return Pool.Spawn(prefab, position, rotation);
        }

        /// <summary>
        /// Despawns with network broadcast.
        /// </summary>
        public static void DespawnNetworked(this PoolHandle<GameObject> handle, NetworkTarget target = NetworkTarget.Clients)
        {
            var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
            handler?.BroadcastDespawn(handle, target);
            Pool.Despawn(handle);
        }

        /// <summary>
        /// Registers an existing handle for networking.
        /// </summary>
        public static uint RegisterNetworked(this PoolHandle<GameObject> handle, bool serverAuth = true)
        {
            var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
            return handler?.Register(handle, serverAuth) ?? 0;
        }

        /// <summary>
        /// Unregisters from networking.
        /// </summary>
        public static void UnregisterNetworked(this PoolHandle<GameObject> handle)
        {
            var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
            handler?.Unregister(handle);
        }

        /// <summary>
        /// Gets the network ID.
        /// </summary>
        public static uint GetNetworkId(this PoolHandle<GameObject> handle)
        {
            var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
            return handler?.GetId(handle) ?? 0;
        }
    }
}
