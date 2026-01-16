using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Networking.Features.Culling;
using Eraflo.Catalyst.Pooling;
using Eraflo.Catalyst.Scenes.Networking;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Backends.Mock
{
    /// <summary>
    /// Mock network backend for testing without actual network.
    /// Logs all operations and can simulate local message delivery.
    /// </summary>
    public class MockNetworkBackend : INetworkBackend, INetworkLifecycle,
        IConnectionBackend, ISceneNetworkBackend, IPoolNetworkBackend,
        ISimulationBackend, ICullingBackend
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

        async Task ISceneNetworkBackend.UnloadSceneAsync(UnityEngine.SceneManagement.Scene scene)
        {
            Debug.Log($"[MockNetworkBackend] Simulating UnloadSceneAsync '{scene.name}'");
            await Task.Yield();
        }

        void IPoolNetworkBackend.SynchronizeInstance(GameObject instance, uint networkId)
        {
            Debug.Log($"[MockNetworkBackend] Synchronized instance {instance.name} with NetworkId {networkId}");
        }

        #endregion

        #region ISimulationBackend

        private int _simulatedLatencyMs;
        private float _simulatedPacketLoss;
        private int _simulatedJitterMs;
        private float _mockRTT = 0f;
        private float _mockPacketLoss = 0f;
        private float _mockBandwidthIn = 0f;
        private float _mockBandwidthOut = 0f;

        /// <summary>
        /// Applies simulation parameters (stores for testing verification).
        /// </summary>
        public void ApplySimulationParameters(int latencyMs, float packetLossPercent, int jitterMs)
        {
            _simulatedLatencyMs = latencyMs;
            _simulatedPacketLoss = packetLossPercent;
            _simulatedJitterMs = jitterMs;
            Debug.Log($"[MockNetworkBackend] Simulation: latency={latencyMs}ms, loss={packetLossPercent}%, jitter={jitterMs}ms");
        }

        /// <summary>Gets simulated RTT.</summary>
        public float GetRTT() => _mockRTT;

        /// <summary>Gets simulated packet loss.</summary>
        public float GetPacketLoss() => _mockPacketLoss;

        /// <summary>Gets simulated inbound bandwidth.</summary>
        public float GetBandwidthIn() => _mockBandwidthIn;

        /// <summary>Gets simulated outbound bandwidth.</summary>
        public float GetBandwidthOut() => _mockBandwidthOut;

        /// <summary>Sets mock RTT for testing.</summary>
        public void SetMockRTT(float rtt) => _mockRTT = rtt;

        /// <summary>Sets mock packet loss for testing.</summary>
        public void SetMockPacketLoss(float loss) => _mockPacketLoss = loss;

        /// <summary>Sets mock bandwidth for testing.</summary>
        public void SetMockBandwidth(float inKBps, float outKBps)
        {
            _mockBandwidthIn = inKBps;
            _mockBandwidthOut = outKBps;
        }

        /// <summary>Gets applied simulation parameters for test verification.</summary>
        public (int Latency, float PacketLoss, int Jitter) GetSimulationParameters()
            => (_simulatedLatencyMs, _simulatedPacketLoss, _simulatedJitterMs);

        #endregion

        #region ICullingBackend

        private readonly Dictionary<uint, HashSet<ulong>> _objectVisibility = new();
        private readonly HashSet<uint> _globallyVisible = new();

        /// <summary>Shows a network object to a specific client.</summary>
        public void NetworkShow(uint networkId, ulong clientId)
        {
            if (!_objectVisibility.TryGetValue(networkId, out var clients))
            {
                clients = new HashSet<ulong>();
                _objectVisibility[networkId] = clients;
            }
            clients.Add(clientId);
            Debug.Log($"[MockNetworkBackend] NetworkShow: {networkId} -> client {clientId}");
        }

        /// <summary>Hides a network object from a specific client.</summary>
        public void NetworkHide(uint networkId, ulong clientId)
        {
            if (_objectVisibility.TryGetValue(networkId, out var clients))
            {
                clients.Remove(clientId);
            }
            Debug.Log($"[MockNetworkBackend] NetworkHide: {networkId} <- client {clientId}");
        }

        /// <summary>Shows a network object to all clients.</summary>
        public void NetworkShowToAll(uint networkId)
        {
            _globallyVisible.Add(networkId);
            Debug.Log($"[MockNetworkBackend] NetworkShowToAll: {networkId}");
        }

        /// <summary>Hides a network object from all clients.</summary>
        public void NetworkHideFromAll(uint networkId)
        {
            _globallyVisible.Remove(networkId);
            _objectVisibility.Remove(networkId);
            Debug.Log($"[MockNetworkBackend] NetworkHideFromAll: {networkId}");
        }

        /// <summary>Checks if an object is visible to a client.</summary>
        public bool IsVisibleTo(uint networkId, ulong clientId)
        {
            if (_globallyVisible.Contains(networkId)) return true;
            if (_objectVisibility.TryGetValue(networkId, out var clients))
            {
                return clients.Contains(clientId);
            }
            return false;
        }

        /// <summary>Gets all clients that can see an object (for testing).</summary>
        public HashSet<ulong> GetVisibleClients(uint networkId)
        {
            if (_objectVisibility.TryGetValue(networkId, out var clients))
                return new HashSet<ulong>(clients);
            return new HashSet<ulong>();
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
            _objectVisibility.Clear();
            _globallyVisible.Clear();
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

        public GameObject SpawnPlayer(ulong clientId, Vector3? position = null, Quaternion? rotation = null)
        {
            Debug.Log($"[MockNetworkBackend] Manually spawned player for client {clientId} at {position ?? Vector3.zero}");
            return null;
        }

        public ulong GetOwner(GameObject go)
        {
            if (go == null) return 0;
            return 0; // Default to server/zero for mock
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

        /// <summary>
        /// Clears sent messages for test isolation.
        /// </summary>
        public void ClearSentMessages()
        {
            _sentMessages.Clear();
        }

        #region INetworkLifecycle

        public bool StartServer(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
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

        public bool StartHost(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP)
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
