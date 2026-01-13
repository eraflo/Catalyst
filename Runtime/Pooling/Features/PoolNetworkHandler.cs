using System;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Unified handler for network synchronization of pooled objects (GameObjects and C# classes).
    /// </summary>
    public class PoolNetworkHandler : INetworkMessageHandler
    {
        private NetworkIdManager _idManager;
        private uint _nextId = 1;
        private NetworkManager _network;

        public void OnRegistered()
        {
            _idManager = App.Get<NetworkIdManager>();
            _network = App.Get<NetworkManager>();
            _network.On<PoolNetworkMessage>(HandlePoolMessage);
            _network.On<NetworkStateUpdateMessage>(HandleStateUpdate);
        }

        public void OnUnregistered()
        {
            _network.Off<PoolNetworkMessage>(HandlePoolMessage);
            _network.Off<NetworkStateUpdateMessage>(HandleStateUpdate);
            Clear();
        }

        public void OnNetworkConnected() { }
        public void OnNetworkDisconnected() => Clear();

        /// <summary>
        /// Registers a pooled object for networking.
        /// </summary>
        public void SpawnNetworked<T>(T instance, string poolId, Vector3 pos = default, Quaternion rot = default, byte[] data = null, NetworkTarget target = NetworkTarget.Clients) where T : class
        {
            if (!_network.IsConnected || !_network.IsServer) return;

            uint id = _nextId++;
            RegisterLocal(id, instance);

            // Notify backend if GO
            if (instance is GameObject go)
            {
                _network.Backend.SynchronizeInstance(go, id);
            }

            // Call hooks
            if (instance is INetworkPoolable poolable)
            {
                poolable.OnNetworkSpawn(data);
            }

            // Broadcast (Only if NOT a GameObject or if backend is not authoritative on GO spawning)
            // NGO handles its own replication for GameObjects via NetworkObject.Spawn()
            if (instance is GameObject && _network.Backend.SupportsNativeGameObjectReplication)
            {
                // Native backend will replicate via its own internal system
                return;
            }

            var msg = new PoolNetworkMessage
            {
                NetworkId = id,
                PoolId = poolId,
                IsSpawn = true,
                SpawnData = data,
                Position = pos,
                Rotation = rot
            };
            _network.Send(msg, target);
        }

        /// <summary>
        /// Despawns a networked object across the network.
        /// </summary>
        public void DespawnNetworked<T>(T instance, NetworkTarget target = NetworkTarget.Clients) where T : class
        {
            uint id = GetId(instance);
            if (!_network.IsConnected || id == 0) return;

            // Authority check
            var ownership = App.Get<NetworkOwnershipManager>();
            if (ownership != null && !ownership.HasAuthority(id, AuthorityMode.ServerAuthoritative))
            {
                Debug.LogWarning($"[PoolNetworkHandler] Client tried to despawn object {id} without authority.");
                return;
            }

            if (instance is INetworkPoolable poolable)
            {
                poolable.OnNetworkDespawn();
            }

            if (_network.IsServer)
            {
                var msg = new PoolNetworkMessage
                {
                    NetworkId = id,
                    IsSpawn = false
                };
                _network.Send(msg, target);
            }

            UnregisterLocal(id, instance);
        }

        private void HandleStateUpdate(NetworkStateUpdateMessage msg)
        {
            // Clients handle state updates from server
            if (_network.IsServer) return;

            if (_idManager != null)
            {
                var instance = _idManager.GetObject<object>(msg.NetworkId);
                if (instance is INetworkStateSyncable syncable)
                {
                    syncable.OnNetworkStateUpdate(msg.PropertyName, msg.Data);
                }
            }
        }

        private void HandlePoolMessage(PoolNetworkMessage msg)
        {
            if (_network.IsServer) return;

            var pool = App.Get<Pool>();
            if (msg.IsSpawn)
            {
                object instance = null;
                
                // 1. Try resolving as Prefab
                GameObject prefab = pool.ResolvePrefab(msg.PoolId);
                if (prefab != null)
                {
                    var handle = pool.SpawnObject(prefab, msg.Position, msg.Rotation);
                    instance = handle.Instance;
                    
                    // Client side sync
                    _network.Backend.SynchronizeInstance((GameObject)instance, msg.NetworkId);
                }
                else
                {
                    // 2. Try resolving as C# Type
                    Type type = Type.GetType(msg.PoolId);
                    if (type != null)
                    {
                        instance = pool.GetFromPoolDynamic(type);
                    }
                }

                if (instance != null)
                {
                    RegisterLocal(msg.NetworkId, instance);
                    if (instance is INetworkPoolable poolable)
                    {
                        poolable.OnNetworkSpawn(msg.SpawnData);
                    }
                }
            }
            else
            {
                if (_idManager != null)
                {
                    var instance = _idManager.GetObject<object>(msg.NetworkId);
                    if (instance != null)
                    {
                        if (instance is INetworkPoolable poolable)
                        {
                            poolable.OnNetworkDespawn();
                        }
                        
                        pool.DespawnDynamic(instance);
                        UnregisterLocal(msg.NetworkId, instance);
                    }
                }
            }
        }

        private void RegisterLocal(uint id, object instance)
        {
            _idManager?.Register(id, instance);
        }

        private void UnregisterLocal(uint id, object instance)
        {
            _idManager?.Unregister(instance);
        }

        public uint GetId(object instance)
        {
            return _idManager?.GetId(instance) ?? 0;
        }

        public void Clear()
        {
            _idManager?.Clear();
            _nextId = 1;
        }
    }
}
