using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.Catalyst.Scenes.Networking;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Networking.Backends.Mock
{
    /// <summary>
    /// Mock network backend for testing without actual network.
    /// Logs all operations and can simulate local message delivery.
    /// </summary>
    public class MockNetworkBackend : INetworkBackend, INetworkLifecycle,
        IConnectionBackend, ISceneNetworkBackend, IPoolNetworkBackend
    {
        private readonly Dictionary<ushort, Action<byte[], ulong>> _handlers = new Dictionary<ushort, Action<byte[], ulong>>();
        private bool _isServer;
        private bool _isClient;
        private bool _isConnected;

        public bool IsServer => _isServer;
        public bool IsClient => _isClient;
        public bool IsConnected => _isConnected;
        public bool SupportsNativeGameObjectReplication => false;

        #region Module Backend Implementations

        void IConnectionBackend.Initialize() => Debug.Log("[MockNetworkBackend] Connection initialized");

        async Task ISceneNetworkBackend.LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[MockNetworkBackend] Simulating LoadSceneAsync '{sceneName}' ({mode})");
            await Task.Yield();
        }

        void IPoolNetworkBackend.SynchronizeInstance(GameObject instance, uint networkId)
        {
            Debug.Log($"[MockNetworkBackend] Synchronized instance {instance.name} with NetworkId {networkId}");
        }

        #endregion

        /// <summary>
        /// Simulated local client ID.
        /// </summary>
        public ulong LocalClientId { get; set; } = 0;

        /// <summary>
        /// Simulated server client ID.
        /// </summary>
        public ulong ServerClientId { get; set; } = 0;

        private readonly List<(ushort Type, byte[] Data, NetworkTarget Target)> _sentMessages = new List<(ushort, byte[], NetworkTarget)>();

        /// <summary>
        /// List of all messages sent through this backend.
        /// </summary>
        public IReadOnlyList<(ushort Type, byte[] Data, NetworkTarget Target)> SentMessages => _sentMessages;

        public bool EnableLoopback { get; set; } = true;

        /// <summary>
        /// Creates a mock backend with specified state.
        /// </summary>
        public MockNetworkBackend(bool isServer = true, bool isClient = true, bool isConnected = true)
        {
            _isServer = isServer;
            _isClient = isClient;
            _isConnected = isConnected;
        }

        public void Initialize()
        {
            Debug.Log("[MockNetworkBackend] Initialized");
        }

        public void Shutdown()
        {
            _handlers.Clear();
            Debug.Log("[MockNetworkBackend] Shutdown");
        }      

        public void Send(ushort msgType, byte[] data, NetworkTarget target, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            _sentMessages.Add((msgType, data, target));
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[MockNetworkBackend] Sent {msgType} to {target} ({delivery})");
            }
            else
            {
                Debug.Log($"[MockNetworkBackend] Send msgType={msgType}, {data.Length} bytes, target={target} ({delivery})");
            }
        }



        public void RegisterHandler(ushort msgType, Action<byte[], ulong> handler)
        {
            _handlers[msgType] = handler;
            Debug.Log($"[MockNetworkBackend] Registered handler for msgType={msgType}");
        }

        public void UnregisterHandler(ushort msgType)
        {
            _handlers.Remove(msgType);
            Debug.Log($"[MockNetworkBackend] Unregistered handler for msgType={msgType}");
        }

        public void SendToClient(ushort msgType, byte[] data, ulong clientId, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (clientId == LocalClientId) return;
            _sentMessages.Add((msgType, data, NetworkTarget.Clients));

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[MockNetworkBackend] Sent {msgType} to Client {clientId} ({delivery})");
            }
            else
            {
                Debug.Log($"[MockNetworkBackend] SendToClient msgType={msgType}, {data.Length} bytes, clientId={clientId} ({delivery})");
            }
        }

        public void SendToClients(ushort msgType, byte[] data, ulong[] clientIds, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            foreach (var clientId in clientIds)
            {
                if (clientId == LocalClientId) continue;
                _sentMessages.Add((msgType, data, NetworkTarget.Clients));
            }

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[MockNetworkBackend] Sent {msgType} to {clientIds.Length} Clients ({delivery})");
            }
            else
            {
                Debug.Log($"[MockNetworkBackend] SendToClients msgType={msgType}, {data.Length} bytes, clients={clientIds.Length} ({delivery})");
            }
        }

        /// <summary>
        /// Simulates receiving a message (for testing).
        /// </summary>
        public void SimulateReceive(ushort msgType, byte[] data, ulong senderId = 0)
        {
            if (_handlers.TryGetValue(msgType, out var handler))
            {
                handler.Invoke(data, senderId);
            }
        }


        /// <summary>
        /// Alias for SimulateReceive to match test expectations.
        /// </summary>
        public void TriggerReceive(ushort msgType, byte[] data, ulong senderId = 0) => SimulateReceive(msgType, data, senderId);

        /// <summary>
        /// Sets the server state.
        /// </summary>
        public void SetServerState(bool isServer)
        {
            _isServer = isServer;
        }

        /// <summary>
        /// Sets the client state.
        /// </summary>
        public void SetClientState(bool isClient)
        {
            _isClient = isClient;
        }

        /// <summary>
        /// Sets the connected state.
        /// </summary>
        public void SetConnectedState(bool isConnected)
        {
            _isConnected = isConnected;
        }

        #region INetworkLifecycle

        public bool StartServer(ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            _isServer = true;
            _isClient = false;
            _isConnected = true;
            Debug.Log($"[MockNetworkBackend] Started Server on port {port} ({transport})");
            App.Get<NetworkManager>().NotifyConnected();
            return true;
        }

        public bool StartClient(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            _isServer = false;
            _isClient = true;
            _isConnected = true;
            Debug.Log($"[MockNetworkBackend] Connected to {address}:{port} ({transport})");
            App.Get<NetworkManager>().NotifyConnected();
            return true;
        }

        public bool StartHost(ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            _isServer = true;
            _isClient = true;
            _isConnected = true;
            Debug.Log($"[MockNetworkBackend] Started Host on port {port} ({transport})");
            App.Get<NetworkManager>().NotifyConnected();
            return true;
        }

        public void Stop()
        {
            _isServer = false;
            _isClient = false;
            _isConnected = false;
            Debug.Log("[MockNetworkBackend] Stopped");
            App.Get<NetworkManager>().NotifyDisconnected();
        }

        #endregion
    }
}
