/*
 * ============================================================================
 * WEBSOCKET DISCOVERY TRANSPORT
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * This transport uses WebSocket connections for server discovery via a 
 * central relay/matchmaking server. Unlike UDP broadcast, this works across
 * the internet.
 * 
 * HOW IT WORKS:
 * -------------
 * 1. Client connects to a WebSocket relay server
 * 2. Server publishes its info to the relay
 * 3. Clients receive server lists from the relay
 * 4. Relay handles NAT traversal and routing
 * 
 * REQUIREMENTS:
 * -------------
 * - A WebSocket relay server running (Unity Relay, custom, etc.)
 * - Internet connection
 * - Relay server URL configured in PackageSettings
 * 
 * USE CASES:
 * ----------
 * - Internet matchmaking
 * - Cross-network discovery
 * - When UDP broadcast is blocked
 * 
 * PROTOCOL:
 * ---------
 * Messages are JSON-formatted:
 * - ADVERTISE: { "type": "advertise", "data": "<base64>" }
 * - DISCOVER: { "type": "discover" }
 * - SERVER_LIST: { "type": "server", "data": "<base64>", "sender": "ip" }
 * 
 * SECURITY:
 * ---------
 * - TLS encryption via wss:// URLs
 * - JSON validation with Newtonsoft
 * - Input sanitization
 * 
 * ============================================================================
 */

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// WebSocket-based discovery transport for internet matchmaking.
    /// 
    /// <para><b>Configuration:</b></para>
    /// Set the relay URL in PackageSettings.DiscoveryRelayUrl
    /// 
    /// <para><b>Note:</b></para>
    /// Requires a WebSocket relay server. See documentation for setup.
    /// </summary>
    public class WebSocketDiscoveryTransport : IDiscoveryTransport
    {
        public string Name => "WebSocket";
        public bool IsBroadcasting => _isBroadcasting;
        public bool IsListening => _isListening;

        private readonly string _relayUrl;
        private readonly int _reconnectDelayMs;
        private readonly int _maxReconnectAttempts;
        
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private volatile bool _isBroadcasting;
        private volatile bool _isListening;
        private byte[] _broadcastData;
        private int _broadcastIntervalMs;

        public event Action<byte[], string> OnDataReceived;

        /// <summary>
        /// Creates a WebSocket discovery transport.
        /// </summary>
        /// <param name="relayUrl">WebSocket relay server URL (e.g., "wss://relay.example.com").</param>
        /// <param name="reconnectDelayMs">Delay between reconnection attempts.</param>
        /// <param name="maxReconnectAttempts">Maximum reconnection attempts (0 = infinite).</param>
        public WebSocketDiscoveryTransport(
            string relayUrl,
            int reconnectDelayMs = 3000,
            int maxReconnectAttempts = 5)
        {
            if (string.IsNullOrEmpty(relayUrl))
                throw new ArgumentNullException(nameof(relayUrl), "Relay URL is required for WebSocket transport");
            
            // Security: Validate URL format
            if (!relayUrl.StartsWith("ws://") && !relayUrl.StartsWith("wss://"))
                throw new ArgumentException("Relay URL must start with ws:// or wss://", nameof(relayUrl));
            
            _relayUrl = relayUrl;
            _reconnectDelayMs = reconnectDelayMs;
            _maxReconnectAttempts = maxReconnectAttempts;
        }

        public void StartBroadcasting(byte[] data, int intervalMs)
        {
            if (_isBroadcasting) return;
            
            _broadcastData = data;
            _broadcastIntervalMs = intervalMs;
            _isBroadcasting = true;
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                int attempts = 0;
                while (_isBroadcasting && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        _socket = new ClientWebSocket();
                        await _socket.ConnectAsync(new Uri(_relayUrl), _cts.Token);
                        Debug.Log($"[WebSocketDiscoveryTransport] Connected to relay: {_relayUrl}");
                        attempts = 0; // Reset on successful connect

                        while (_isBroadcasting && 
                               _socket.State == WebSocketState.Open && 
                               !_cts.Token.IsCancellationRequested)
                        {
                            // Create message using Newtonsoft
                            var message = new JObject
                            {
                                ["type"] = "advertise",
                                ["data"] = Convert.ToBase64String(_broadcastData)
                            };
                            
                            var json = message.ToString(Formatting.None);
                            var bytes = Encoding.UTF8.GetBytes(json);
                            await _socket.SendAsync(new ArraySegment<byte>(bytes), 
                                WebSocketMessageType.Text, true, _cts.Token);
                            
                            await Task.Delay(_broadcastIntervalMs, _cts.Token);
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception e)
                    {
                        if (_isBroadcasting)
                        {
                            Debug.LogWarning($"[WebSocketDiscoveryTransport] Connection error: {e.Message}");
                            attempts++;
                            
                            if (_maxReconnectAttempts > 0 && attempts >= _maxReconnectAttempts)
                            {
                                Debug.LogError("[WebSocketDiscoveryTransport] Max reconnect attempts reached.");
                                break;
                            }
                            
                            await Task.Delay(_reconnectDelayMs, _cts.Token);
                        }
                    }
                    finally
                    {
                        _socket?.Dispose();
                        _socket = null;
                    }
                }
            });

            Debug.Log($"[WebSocketDiscoveryTransport] Broadcasting to relay");
        }

        public void StopBroadcasting()
        {
            _isBroadcasting = false;
            _cts?.Cancel();
        }

        public void StartListening()
        {
            if (_isListening) return;
            
            _isListening = true;
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                int attempts = 0;
                while (_isListening && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        _socket = new ClientWebSocket();
                        await _socket.ConnectAsync(new Uri(_relayUrl), _cts.Token);
                        Debug.Log($"[WebSocketDiscoveryTransport] Connected for discovery");
                        attempts = 0;

                        // Send discover request
                        var discoverMsg = new JObject { ["type"] = "discover" };
                        var msgBytes = Encoding.UTF8.GetBytes(discoverMsg.ToString(Formatting.None));
                        await _socket.SendAsync(new ArraySegment<byte>(msgBytes), 
                            WebSocketMessageType.Text, true, _cts.Token);

                        // Receive loop
                        var buffer = new byte[4096];
                        while (_isListening && 
                               _socket.State == WebSocketState.Open && 
                               !_cts.Token.IsCancellationRequested)
                        {
                            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                            
                            if (result.MessageType == WebSocketMessageType.Close)
                                break;

                            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            ProcessMessage(json);
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception e)
                    {
                        if (_isListening)
                        {
                            Debug.LogWarning($"[WebSocketDiscoveryTransport] Listen error: {e.Message}");
                            attempts++;
                            
                            if (_maxReconnectAttempts > 0 && attempts >= _maxReconnectAttempts)
                            {
                                Debug.LogError("[WebSocketDiscoveryTransport] Max reconnect attempts reached.");
                                break;
                            }
                            
                            await Task.Delay(_reconnectDelayMs, _cts.Token);
                        }
                    }
                    finally
                    {
                        _socket?.Dispose();
                        _socket = null;
                    }
                }
            });

            Debug.Log("[WebSocketDiscoveryTransport] Listening for servers");
        }

        public void StopListening()
        {
            _isListening = false;
            _cts?.Cancel();
        }

        public void Shutdown()
        {
            StopBroadcasting();
            StopListening();
            
            try
            {
                if (_socket?.State == WebSocketState.Open)
                    _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutdown", CancellationToken.None);
            }
            catch { }
            
            _socket?.Dispose();
            _socket = null;
            _cts?.Dispose();
            _cts = null;
        }

        private void ProcessMessage(string json)
        {
            try
            {
                // Parse with Newtonsoft for security and validation
                var msg = JObject.Parse(json);
                
                string type = msg["type"]?.Value<string>();
                if (type != "server") return;
                
                string base64Data = msg["data"]?.Value<string>();
                string sender = msg["sender"]?.Value<string>();
                
                if (string.IsNullOrEmpty(base64Data) || string.IsNullOrEmpty(sender))
                    return;

                // Validate Base64 before decoding
                byte[] data;
                try
                {
                    data = Convert.FromBase64String(base64Data);
                }
                catch (FormatException)
                {
                    Debug.LogWarning("[WebSocketDiscoveryTransport] Invalid Base64 data received");
                    return;
                }
                
                OnDataReceived?.Invoke(data, sender);
            }
            catch (JsonException e)
            {
                Debug.LogWarning($"[WebSocketDiscoveryTransport] Invalid JSON: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebSocketDiscoveryTransport] Parse error: {e.Message}");
            }
        }
    }
}
