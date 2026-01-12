#if UNITY_NETCODE
using System;
using System.Collections.Generic;
using UnityEngine;
using NetcodeMgr = Unity.Netcode.NetworkManager;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Networking.Backends
{
    /// <summary>
    /// Network backend implementation for Unity Netcode for GameObjects.
    /// </summary>
    public class NetcodeBackend : INetworkBackend, INetworkLifecycle
    {
        private readonly Dictionary<ushort, Action<byte[], ulong>> _handlers = new Dictionary<ushort, Action<byte[], ulong>>();

        public bool IsServer => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsServer;
        public bool IsClient => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsClient;
        public bool IsConnected => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsConnectedClient;
        public bool SupportsNativeGameObjectReplication => true;

        public void Initialize()
        {
            if (NetcodeMgr.Singleton == null)
            {
                Debug.LogWarning("[NetcodeBackend] NetworkManager.Singleton is null");
                return;
            }

            NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage += HandleUnnamedMessage;
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log("[NetcodeBackend] Initialized");
            }

            NetcodeMgr.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetcodeMgr.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void HandleClientConnected(ulong id)
        {
            App.Get<NetworkManager>().NotifyClientConnected(id);
        }

        private void HandleClientDisconnected(ulong id)
        {
            App.Get<NetworkManager>().NotifyClientDisconnected(id);
        }

        public void Shutdown()
        {
            if (NetcodeMgr.Singleton != null)
            {
                if (NetcodeMgr.Singleton.CustomMessagingManager != null)
                    NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage -= HandleUnnamedMessage;
                
                NetcodeMgr.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetcodeMgr.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
            
            _handlers.Clear();
        }

        public void Send(ushort msgType, byte[] data, NetworkTarget target, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            var ngoDelivery = MapDelivery(delivery);

            using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
            {
                writer.WriteBytesSafe(fullData);

                switch (target)
                {
                    case NetworkTarget.All:
                        SendToAll(writer, ngoDelivery);
                        break;
                    case NetworkTarget.Others:
                        SendToOthers(writer, ngoDelivery);
                        break;
                    case NetworkTarget.Server:
                        SendToServer(writer, ngoDelivery);
                        break;
                    case NetworkTarget.Clients:
                        SendToClients(writer, ngoDelivery);
                        break;
                }
            }
        }

        private Unity.Netcode.NetworkDelivery MapDelivery(Eraflo.Catalyst.Networking.NetworkDelivery delivery)
        {
            switch (delivery)
            {
                case Eraflo.Catalyst.Networking.NetworkDelivery.Unreliable: return Unity.Netcode.NetworkDelivery.Unreliable;
                case Eraflo.Catalyst.Networking.NetworkDelivery.Reliable: return Unity.Netcode.NetworkDelivery.Reliable;
                case Eraflo.Catalyst.Networking.NetworkDelivery.UnreliableSequenced: return Unity.Netcode.NetworkDelivery.UnreliableSequenced;
                case Eraflo.Catalyst.Networking.NetworkDelivery.ReliableSequenced: return Unity.Netcode.NetworkDelivery.ReliableSequenced;
                case Eraflo.Catalyst.Networking.NetworkDelivery.ReliableFragmented: return Unity.Netcode.NetworkDelivery.ReliableFragmentedSequenced;
                default: return Unity.Netcode.NetworkDelivery.Reliable;
            }
        }

        private void SendToAll(Unity.Netcode.FastBufferWriter writer, Unity.Netcode.NetworkDelivery delivery)
        {
            if (IsServer)
            {
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                        clientId, writer, delivery);
                }
            }
            else
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, delivery);
            }
        }

        private void SendToOthers(Unity.Netcode.FastBufferWriter writer, Unity.Netcode.NetworkDelivery delivery)
        {
            if (IsServer)
            {
                var localClientId = NetcodeMgr.Singleton.LocalClientId;
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    if (clientId != localClientId)
                    {
                        NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                            clientId, writer, delivery);
                    }
                }
            }
            else
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, delivery);
            }
        }

        private void SendToServer(Unity.Netcode.FastBufferWriter writer, Unity.Netcode.NetworkDelivery delivery)
        {
            if (!IsServer)
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, delivery);
            }
        }

        private void SendToClients(Unity.Netcode.FastBufferWriter writer, Unity.Netcode.NetworkDelivery delivery)
        {
            if (IsServer)
            {
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetcodeMgr.Singleton.LocalClientId)
                    {
                        NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                            clientId, writer, delivery);
                    }
                }
            }
        }

        private void HandleUnnamedMessage(ulong senderId, Unity.Netcode.FastBufferReader reader)
        {
            var length = reader.Length - reader.Position;
            var fullData = new byte[length];
            reader.ReadBytesSafe(ref fullData, length);

            if (fullData.Length < 2) return;

            ushort msgType = (ushort)((fullData[0] << 8) | fullData[1]);

            var data = new byte[fullData.Length - 2];
            Buffer.BlockCopy(fullData, 2, data, 0, data.Length);

            if (_handlers.TryGetValue(msgType, out var handler))
            {
                try { handler.Invoke(data, senderId); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void RegisterHandler(ushort msgType, Action<byte[], ulong> handler)
        {
            _handlers[msgType] = handler;
        }

        public void UnregisterHandler(ushort msgType)
        {
            _handlers.Remove(msgType);
        }

        public ulong LocalClientId => NetcodeMgr.Singleton?.LocalClientId ?? 0;

        public void SendToClient(ushort msgType, byte[] data, ulong clientId, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected || !IsServer) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            var ngoDelivery = MapDelivery(delivery);

            using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
            {
                writer.WriteBytesSafe(fullData);
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    clientId, writer, ngoDelivery);
            }
        }

        public void SendToClients(ushort msgType, byte[] data, ulong[] clientIds, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected || !IsServer) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            var ngoDelivery = MapDelivery(delivery);

            foreach (var clientId in clientIds)
            {
                using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
                {
                    writer.WriteBytesSafe(fullData);
                    NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                        clientId, writer, ngoDelivery);
                }
            }
        }

        #region INetworkLifecycle

        public bool StartServer(ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (NetcodeMgr.Singleton == null) return false;
            ConfigureTransport(null, port, transport);
            return NetcodeMgr.Singleton.StartServer();
        }

        public bool StartClient(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (NetcodeMgr.Singleton == null) return false;
            ConfigureTransport(address, port, transport);
            return NetcodeMgr.Singleton.StartClient();
        }

        public bool StartHost(ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (NetcodeMgr.Singleton == null) return false;
            ConfigureTransport(null, port, transport);
            return NetcodeMgr.Singleton.StartHost();
        }

        private void ConfigureTransport(string address, ushort port, NetworkTransportType transportType)
        {
            var ut = NetcodeMgr.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (ut == null) return;

            if (!string.IsNullOrEmpty(address)) ut.ConnectionData.Address = address;
            ut.ConnectionData.Port = port;

            switch (transportType)
            {
                case NetworkTransportType.UDP:
                    ut.ConnectionData.ServerListenAddress = "0.0.0.0";
                    // NGO's UnityTransport usually defaults to UDP/Relay
                    break;
                case NetworkTransportType.TCP:
                    // UnityTransport doesn't support raw TCP easily, usually uses UDP/WSS
                    Debug.LogWarning("[NetcodeBackend] UnityTransport has limited TCP support. Using default.");
                    break;
                case NetworkTransportType.WebSocket:
                    // Typical setup for WebGL or specialized WS transports
                    Debug.LogWarning("[NetcodeBackend] WebSocket requested. Ensure UnityTransport is configured for WSS.");
                    break;
            }
        }

        public void Stop()
        {
            if (NetcodeMgr.Singleton != null)
                NetcodeMgr.Singleton.Shutdown();
        }

        public void SynchronizeInstance(GameObject instance, uint networkId)
        {
            if (instance == null) return;
            
            var no = instance.GetComponent<Unity.Netcode.NetworkObject>();
            if (no != null && !no.IsSpawned)
            {
                // Register our prefab handler for this prefab if not already done
                // This ensures NGO uses Catalyst's Pool on clients during replication
                RegisterPrefabHandler(no);

                no.Spawn(true); 
            }
        }

        private readonly HashSet<uint> _registeredPrefabs = new HashSet<uint>();
        
        private void RegisterPrefabHandler(Unity.Netcode.NetworkObject no)
        {
            uint prefabId = no.PrefabIdHash;
            
            if (_registeredPrefabs.Contains(prefabId)) return;

            var networkMgr = Unity.Netcode.NetworkManager.Singleton;
            if (networkMgr != null && networkMgr.PrefabHandler != null)
            {
                networkMgr.PrefabHandler.AddHandler(no, new CatalystPrefabHandler(no.gameObject));
                _registeredPrefabs.Add(prefabId);
            }
        }

        private class CatalystPrefabHandler : Unity.Netcode.INetworkPrefabInstanceHandler
        {
            private readonly GameObject _prefab;
            public CatalystPrefabHandler(GameObject prefab) => _prefab = prefab;

            public Unity.Netcode.NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var handle = App.Get<Pool>().SpawnObject(_prefab, position, rotation);
                return handle.Instance.GetComponent<Unity.Netcode.NetworkObject>();
            }

            public void Destroy(Unity.Netcode.NetworkObject networkObject)
            {
                var pool = App.Get<Pool>();
                pool.DespawnDynamic(networkObject.gameObject);
            }
        }

        #endregion
    }
}
#endif
