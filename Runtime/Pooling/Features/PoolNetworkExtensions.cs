using UnityEngine;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Extension methods for unified networked pooling.
    /// </summary>
    public static class PoolNetworkExtensions
    {
        /// <summary>
        /// Spawns a GameObject networked.
        /// </summary>
        public static PoolHandle<GameObject> SpawnNetworked(
            this Pool pool, GameObject prefab, Vector3 position, Quaternion rotation = default,
            byte[] data = null, NetworkTarget target = NetworkTarget.Clients)
        {
            var handle = pool.SpawnObject(prefab, position, rotation);
            var handler = App.Get<NetworkManager>()?.Handlers.Get<PoolNetworkHandler>();
            
            if (handler != null && handle.IsValid)
            {
                handler.SpawnNetworked(handle.Instance, prefab.name, position, rotation, data, target);
            }
            
            return handle;
        }

        /// <summary>
        /// Spawns a C# object networked.
        /// </summary>
        public static PoolHandle<T> GetFromPoolNetworked<T>(this Pool pool, byte[] data = null, NetworkTarget target = NetworkTarget.Clients) where T : class, new()
        {
            var handle = pool.GetFromPool<T>();
            var handler = App.Get<NetworkManager>()?.Handlers.Get<PoolNetworkHandler>();
            
            if (handler != null && handle.IsValid)
            {
                handler.SpawnNetworked(handle.Instance, typeof(T).FullName, default, default, data, target);
            }
            
            return handle;
        }

        /// <summary>
        /// Despawns an object across the network.
        /// </summary>
        public static void DespawnNetworked<T>(this PoolHandle<T> handle, NetworkTarget target = NetworkTarget.Clients) where T : class
        {
            if (!handle.IsValid) return;

            var handler = App.Get<NetworkManager>()?.Handlers.Get<PoolNetworkHandler>();
            handler?.DespawnNetworked(handle.Instance, target);

            // Local cleanup
            var pool = App.Get<Pool>();
            if (handle.Instance is GameObject go)
                pool.DespawnObject(new PoolHandle<GameObject>(handle.Id, go, handle.PoolId, handle.SpawnTime));
            else if (handle.Instance is T classInstance)
                pool.DespawnDynamic(classInstance);
        }

        /// <summary>
        /// Allows deconstructing a PoolHandle into (handle, networkId).
        /// </summary>
        public static void Deconstruct<T>(this PoolHandle<T> handle, out PoolHandle<T> outHandle, out uint networkId) where T : class
        {
            outHandle = handle;
            var handler = App.Get<NetworkManager>()?.Handlers.Get<PoolNetworkHandler>();
            networkId = handler?.GetId(handle.Instance) ?? 0;
        }
    }
}
