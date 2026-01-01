using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Handles network synchronization for pooled objects.
    /// </summary>
    public class PoolNetworkHandler : Networking.INetworkMessageHandler
    {
        private readonly Dictionary<uint, NetworkPoolData> _pools = new Dictionary<uint, NetworkPoolData>();
        private readonly Dictionary<uint, PoolHandle<GameObject>> _idToHandle = new Dictionary<uint, PoolHandle<GameObject>>();
        private uint _nextId = 1;
        private bool _connected;

        /// <summary>Fired when a spawn message is received.</summary>
        public event Action<Networking.PoolSpawnMessage> OnSpawnReceived;

        /// <summary>Fired when a despawn is received.</summary>
        public event Action<uint> OnDespawnReceived;

        public void OnRegistered()
        {
            Networking.NetworkManager.On<Networking.PoolSpawnMessage>(HandleSpawn);
            Networking.NetworkManager.On<Networking.PoolDespawnMessage>(HandleDespawn);
        }

        public void OnUnregistered()
        {
            Networking.NetworkManager.Off<Networking.PoolSpawnMessage>(HandleSpawn);
            Networking.NetworkManager.Off<Networking.PoolDespawnMessage>(HandleDespawn);
            Clear();
        }

        public void OnNetworkConnected() => _connected = true;
        public void OnNetworkDisconnected() => _connected = false;

        /// <summary>
        /// Registers a pooled object for networking.
        /// </summary>
        public uint Register(PoolHandle<GameObject> handle, bool serverAuth = true, uint id = 0)
        {
            if (!handle.IsValid) return 0;

            if (id == 0) id = _nextId++;

            _pools[handle.Id] = new NetworkPoolData { Id = id, Handle = handle, ServerAuth = serverAuth };
            _idToHandle[id] = handle;

            return id;
        }

        /// <summary>
        /// Unregisters a pooled object.
        /// </summary>
        public void Unregister(PoolHandle<GameObject> handle)
        {
            if (_pools.TryGetValue(handle.Id, out var data))
            {
                _idToHandle.Remove(data.Id);
                _pools.Remove(handle.Id);
            }
        }

        /// <summary>
        /// Gets the network ID for a handle.
        /// </summary>
        public uint GetId(PoolHandle<GameObject> handle)
            => _pools.TryGetValue(handle.Id, out var d) ? d.Id : 0;

        /// <summary>
        /// Gets the handle for a network ID.
        /// </summary>
        public PoolHandle<GameObject> GetHandle(uint id)
            => _idToHandle.TryGetValue(id, out var h) ? h : PoolHandle<GameObject>.None;

        /// <summary>
        /// Broadcasts a spawn to specified targets.
        /// </summary>
        public void BroadcastSpawn(PoolHandle<GameObject> handle, GameObject prefab, Vector3 pos, Quaternion rot, Networking.NetworkTarget target = Networking.NetworkTarget.Clients)
        {
            var id = GetId(handle);
            if (id == 0 || !Networking.NetworkManager.IsConnected) return;

            var msg = new Networking.PoolSpawnMessage
            {
                NetworkId = id,
                PrefabHash = prefab.GetInstanceID(),
                Position = pos,
                Rotation = rot
            };
            Networking.NetworkManager.Send(msg, target);
        }

        /// <summary>
        /// Broadcasts a despawn to specified targets.
        /// </summary>
        public void BroadcastDespawn(PoolHandle<GameObject> handle, Networking.NetworkTarget target = Networking.NetworkTarget.Clients)
        {
            var id = GetId(handle);
            if (id == 0) return;

            if (Networking.NetworkManager.IsConnected)
            {
                Networking.NetworkManager.Send(new Networking.PoolDespawnMessage { NetworkId = id }, target);
            }

            Unregister(handle);
        }

        private void HandleSpawn(Networking.PoolSpawnMessage msg)
        {
            OnSpawnReceived?.Invoke(msg);
        }

        private void HandleDespawn(Networking.PoolDespawnMessage msg)
        {
            var handle = GetHandle(msg.NetworkId);
            if (handle.IsValid) Pool.Despawn(handle);
            OnDespawnReceived?.Invoke(msg.NetworkId);
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear()
        {
            _pools.Clear();
            _idToHandle.Clear();
        }

        private struct NetworkPoolData
        {
            public uint Id;
            public PoolHandle<GameObject> Handle;
            public bool ServerAuth;
        }
    }

    /// <summary>
    /// Network spawn data.
    /// </summary>
    [Serializable]
    public struct NetworkSpawnData
    {
        public uint NetworkId;
        public int PrefabId;
        public Vector3 Position;
        public Quaternion Rotation;
    }
}
