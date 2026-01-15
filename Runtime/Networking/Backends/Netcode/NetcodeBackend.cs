#if UNITY_NETCODE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NetcodeMgr = Unity.Netcode.NetworkManager;
using Eraflo.Catalyst.Pooling;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Scenes.Networking;

namespace Eraflo.Catalyst.Networking.Backends.Netcode
{
    /// <summary>
    /// Network backend implementation for Unity Netcode for GameObjects.
    /// </summary>
    public class NetcodeBackend : INetworkBackend, INetworkLifecycle,
        IConnectionBackend, ISceneNetworkBackend, IPoolNetworkBackend
    {
        private readonly Dictionary<ushort, Action<byte[], ulong>> _handlers = new Dictionary<ushort, Action<byte[], ulong>>();

        private NetcodeConnectionHandler _connectionHandler;
        private NetcodeSceneHandler _sceneHandler;
        private NetcodePrefabHandler _prefabHandler;

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

            // Initialize specialized handlers
            _connectionHandler = new NetcodeConnectionHandler(NetcodeMgr.Singleton);
            _sceneHandler = new NetcodeSceneHandler(NetcodeMgr.Singleton);
            _prefabHandler = new NetcodePrefabHandler(NetcodeMgr.Singleton);

            _connectionHandler.Initialize();

            if (NetcodeMgr.Singleton.CustomMessagingManager != null)
            {
                NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage += HandleUnnamedMessage;
            }

            NetcodeMgr.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetcodeMgr.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log("[NetcodeBackend] Initialized");
            }
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

            var ngoDelivery = MapDelivery(delivery);
            var writer = CreateWriter(msgType, data);

            try
            {
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
            finally
            {
                writer.Dispose();
            }
        }

        private Unity.Netcode.FastBufferWriter CreateWriter(ushort msgType, byte[] data)
        {
            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp);
            writer.WriteBytesSafe(fullData);
            return writer;
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
            reader.ReadBytesSafe(ref fullData, (int)length);

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

        public void SpawnPlayer(ulong clientId, Vector3? position = null, Quaternion? rotation = null)
        {
            if (NetcodeMgr.Singleton == null || !NetcodeMgr.Singleton.IsServer) return;

            var playerPrefab = NetcodeMgr.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogWarning("[NetcodeBackend] No Player Prefab configured in NetworkManager.");
                return;
            }

            var instance = GameObject.Instantiate(playerPrefab, position ?? Vector3.zero, rotation ?? Quaternion.identity);
            var netObj = instance.GetComponent<Unity.Netcode.NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId, true);
        }

        #region Module Backend Implementations

        void IConnectionBackend.Initialize() => _connectionHandler?.Initialize();

        async Task ISceneNetworkBackend.LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (_sceneHandler != null) await _sceneHandler.LoadSceneAsync(sceneName, mode);
        }

        void IPoolNetworkBackend.SynchronizeInstance(GameObject instance, uint networkId)
        {
            _prefabHandler?.SynchronizeInstance(instance, networkId);
        }

        #endregion

        public ulong LocalClientId => NetcodeMgr.Singleton?.LocalClientId ?? 0;
        public ulong ServerClientId => NetcodeMgr.Singleton != null ? Unity.Netcode.NetworkManager.ServerClientId : 0;

        public void SendToClient(ushort msgType, byte[] data, ulong clientId, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected || !IsServer) return;
            if (clientId == LocalClientId) return; // Handled by NetworkManager loopback

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
            var localClientId = LocalClientId;

            foreach (var clientId in clientIds)
            {
                if (clientId == localClientId) continue; // Handled by NetworkManager loopback

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
            bool success = NetcodeMgr.Singleton.StartServer();
            if (success) EnsureMessagingSubscribed();
            return success;
        }

        public bool StartClient(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (NetcodeMgr.Singleton == null) return false;
            ConfigureTransport(address, port, transport);
            bool success = NetcodeMgr.Singleton.StartClient();
            if (success) EnsureMessagingSubscribed();
            return success;
        }

        public bool StartHost(ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (NetcodeMgr.Singleton == null) return false;
            ConfigureTransport(null, port, transport);
            bool success = NetcodeMgr.Singleton.StartHost();
            if (success) EnsureMessagingSubscribed();
            return success;
        }

        private void EnsureMessagingSubscribed()
        {
            if (NetcodeMgr.Singleton?.CustomMessagingManager != null)
            {
                // Unsubscribe first to prevent duplicates
                NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage -= HandleUnnamedMessage;
                NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage += HandleUnnamedMessage;
            }
        }

        private void ConfigureTransport(string address, ushort port, NetworkTransportType transport)
        {
            if (NetcodeMgr.Singleton?.NetworkConfig?.NetworkTransport is Unity.Netcode.Transports.UTP.UnityTransport utp)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    utp.ConnectionData.Address = address;
                }
                utp.ConnectionData.Port = port;
            }
        }

        public void Stop()
        {
            if (NetcodeMgr.Singleton != null)
            {
                NetcodeMgr.Singleton.Shutdown();
            }
            App.Get<NetworkManager>()?.NotifyDisconnected();
        }

        #endregion
    }
}
#endif