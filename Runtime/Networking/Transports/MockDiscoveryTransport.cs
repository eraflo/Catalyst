/*
 * ============================================================================
 * MOCK DISCOVERY TRANSPORT
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * This transport is for unit testing and development. It simulates network
 * discovery without any actual network communication.
 * 
 * FEATURES:
 * ---------
 * - Simulate server discovery with configurable data
 * - Instant or delayed responses
 * - Controllable from tests
 * - No network dependencies
 * 
 * USE CASES:
 * ----------
 * - Unit tests for discovery logic
 * - Development without network
 * - Simulating edge cases (slow responses, failures)
 * 
 * ============================================================================
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Mock discovery transport for unit testing.
    /// 
    /// <para><b>Usage in tests:</b></para>
    /// <code>
    /// var mock = new MockDiscoveryTransport();
    /// mock.SimulateServerFound(serverData, "192.168.1.1");
    /// </code>
    /// </summary>
    public class MockDiscoveryTransport : IDiscoveryTransport
    {
        public string Name => "Mock";
        public bool IsBroadcasting => _isBroadcasting;
        public bool IsListening => _isListening;

        private volatile bool _isBroadcasting;
        private volatile bool _isListening;
        private byte[] _broadcastData;
        private int _broadcastIntervalMs;
        
        private readonly List<(byte[] Data, string Sender)> _pendingMessages = new();

        public event Action<byte[], string> OnDataReceived;

        /// <summary>
        /// Simulates receiving data from a server.
        /// Use this in tests to trigger OnDataReceived.
        /// </summary>
        public void SimulateServerFound(byte[] data, string senderAddress)
        {
            if (_isListening)
            {
                OnDataReceived?.Invoke(data, senderAddress);
            }
            else
            {
                _pendingMessages.Add((data, senderAddress));
            }
        }

        /// <summary>
        /// Simulates multiple servers being found.
        /// </summary>
        public void SimulateMultipleServers(IEnumerable<(byte[] Data, string Address)> servers)
        {
            foreach (var server in servers)
            {
                SimulateServerFound(server.Data, server.Address);
            }
        }

        /// <summary>
        /// Gets the data being broadcast (for test verification).
        /// </summary>
        public byte[] GetBroadcastData() => _broadcastData;

        /// <summary>
        /// Gets the broadcast interval (for test verification).
        /// </summary>
        public int GetBroadcastInterval() => _broadcastIntervalMs;

        public void StartBroadcasting(byte[] data, int intervalMs)
        {
            _broadcastData = data;
            _broadcastIntervalMs = intervalMs;
            _isBroadcasting = true;
            Debug.Log("[MockDiscoveryTransport] Started broadcasting");
        }

        public void StopBroadcasting()
        {
            _isBroadcasting = false;
            _broadcastData = null;
        }

        public void StartListening()
        {
            _isListening = true;
            
            // Deliver any pending messages
            foreach (var msg in _pendingMessages)
            {
                OnDataReceived?.Invoke(msg.Data, msg.Sender);
            }
            _pendingMessages.Clear();
            
            Debug.Log("[MockDiscoveryTransport] Started listening");
        }

        public void StopListening()
        {
            _isListening = false;
        }

        public void Shutdown()
        {
            StopBroadcasting();
            StopListening();
            _pendingMessages.Clear();
        }

        /// <summary>
        /// Clears all state (for test teardown).
        /// </summary>
        public void Reset()
        {
            Shutdown();
            _broadcastData = null;
            _broadcastIntervalMs = 0;
        }
    }
}
