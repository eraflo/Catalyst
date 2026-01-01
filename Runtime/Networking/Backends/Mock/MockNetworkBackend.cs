using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Backends
{
    /// <summary>
    /// Mock network backend for testing without actual network.
    /// Logs all operations and can simulate local message delivery.
    /// </summary>
    public class MockNetworkBackend : INetworkBackend
    {
        private readonly Dictionary<ushort, Action<byte[], ulong>> _handlers = new Dictionary<ushort, Action<byte[], ulong>>();
        private bool _isServer;
        private bool _isClient;
        private bool _isConnected;

        public bool IsServer => _isServer;
        public bool IsClient => _isClient;
        public bool IsConnected => _isConnected;

        /// <summary>
        /// If true, messages sent are immediately delivered locally (loopback).
        /// </summary>
        public bool EnableLoopback { get; set; } = true;

        /// <summary>
        /// Simulated local client ID.
        /// </summary>
        public ulong LocalClientId { get; set; } = 0;

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

        public void Send(ushort msgType, byte[] data, NetworkTarget target)
        {
            Debug.Log($"[MockNetworkBackend] Send msgType={msgType}, {data.Length} bytes, target={target}");

            // Loopback for testing
            if (EnableLoopback && _handlers.TryGetValue(msgType, out var handler))
            {
                handler.Invoke(data, LocalClientId);
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

        public void SendToClient(ushort msgType, byte[] data, ulong clientId)
        {
            Debug.Log($"[MockNetworkBackend] SendToClient msgType={msgType}, {data.Length} bytes, clientId={clientId}");

            // Loopback if targeting self
            if (EnableLoopback && clientId == LocalClientId && _handlers.TryGetValue(msgType, out var handler))
            {
                handler.Invoke(data, LocalClientId);
            }
        }

        public void SendToClients(ushort msgType, byte[] data, ulong[] clientIds)
        {
            Debug.Log($"[MockNetworkBackend] SendToClients msgType={msgType}, {data.Length} bytes, clients={clientIds.Length}");

            foreach (var clientId in clientIds)
            {
                if (EnableLoopback && clientId == LocalClientId && _handlers.TryGetValue(msgType, out var handler))
                {
                    handler.Invoke(data, LocalClientId);
                }
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
    }
}
