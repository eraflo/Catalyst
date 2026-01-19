/*
 * ============================================================================
 * UDP BROADCAST TRANSPORT
 * ============================================================================
 * 
 * HOW IT WORKS:
 * -------------
 * UDP (User Datagram Protocol) broadcast is a connectionless protocol that 
 * allows sending data to ALL devices on a local network simultaneously.
 * 
 * TECHNICAL DETAILS:
 * - Uses IP address 255.255.255.255 (broadcast address) to reach all devices
 * - Port 47777 is the default discovery port
 * - Messages are NOT guaranteed to arrive (UDP is unreliable by design)
 * - No connection handshake required - just send and receive
 * 
 * SECURITY CONSIDERATIONS:
 * - Only works on local networks (same subnet)
 * - Anyone on the network can see broadcast messages
 * - Rate limiting prevents flood attacks
 * - Message size limit prevents buffer overflow attacks
 * 
 * USE CASES:
 * - LAN game discovery
 * - Local multiplayer party games
 * - Development/testing without internet
 * 
 * LIMITATIONS:
 * - Does NOT work across the internet
 * - Does NOT work across different subnets
 * - Blocked by many firewalls/routers
 * 
 * ============================================================================
 */

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// UDP broadcast transport for LAN server discovery.
    /// 
    /// <para><b>When to use:</b></para>
    /// Use this transport for local network (LAN) game discovery where all
    /// players are on the same physical network (WiFi, Ethernet, hotspot).
    /// 
    /// <para><b>How it works:</b></para>
    /// <list type="number">
    ///   <item>Server broadcasts its info to 255.255.255.255 (all devices)</item>
    ///   <item>Clients listen on the discovery port</item>
    ///   <item>When a broadcast is received, OnDataReceived fires</item>
    /// </list>
    /// </summary>
    public class UdpBroadcastTransport : IDiscoveryTransport
    {
        public string Name => "UDP Broadcast";
        public bool IsBroadcasting => _isBroadcasting;
        public bool IsListening => _isListening;

        private readonly int _port;
        private readonly int _maxMessageSize;
        private readonly int _rateLimitPerSecond;
        
        private UdpClient _broadcaster;
        private UdpClient _listener;
        private volatile bool _isBroadcasting;
        private volatile bool _isListening;
        
        // Rate limiting
        private readonly ConcurrentDictionary<string, int> _rateLimitCounter = new();
        private long _rateLimitResetTicks;

        public event Action<byte[], string> OnDataReceived;

        /// <summary>
        /// Creates a new UDP broadcast transport.
        /// </summary>
        /// <param name="port">Discovery port (default: 47777).</param>
        /// <param name="maxMessageSize">Maximum message size in bytes (security).</param>
        /// <param name="rateLimitPerSecond">Max messages per IP per second (security).</param>
        public UdpBroadcastTransport(
            int port = 47777, 
            int maxMessageSize = 512, 
            int rateLimitPerSecond = 10)
        {
            _port = port;
            _maxMessageSize = maxMessageSize;
            _rateLimitPerSecond = rateLimitPerSecond;
        }

        public void StartBroadcasting(byte[] data, int intervalMs)
        {
            if (_isBroadcasting) return;
            _isBroadcasting = true;

            _broadcaster = new UdpClient();
            _broadcaster.EnableBroadcast = true;

            Task.Run(async () =>
            {
                while (_isBroadcasting)
                {
                    try
                    {
                        if (_broadcaster == null) break;
                        await _broadcaster.SendAsync(data, data.Length, 
                            new IPEndPoint(IPAddress.Broadcast, _port));
                        await Task.Delay(intervalMs);
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception e)
                    {
                        if (_isBroadcasting) 
                            Debug.LogError($"[UdpBroadcastTransport] Broadcast error: {e.Message}");
                        break;
                    }
                }
            });

            Debug.Log($"[UdpBroadcastTransport] Broadcasting on port {_port}");
        }

        public void StopBroadcasting()
        {
            _isBroadcasting = false;
            _broadcaster?.Dispose();
            _broadcaster = null;
        }

        public void StartListening()
        {
            if (_isListening) return;
            _isListening = true;
            _rateLimitResetTicks = DateTime.UtcNow.Ticks;

            Task.Run(async () =>
            {
                try
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socket.Bind(new IPEndPoint(IPAddress.Any, _port));
                    _listener = new UdpClient { Client = socket };

                    while (_isListening)
                    {
                        try
                        {
                            var result = await _listener.ReceiveAsync();
                            
                            // Security: size check
                            if (result.Buffer.Length > _maxMessageSize)
                                continue;
                            
                            // Security: rate limiting
                            string senderIp = result.RemoteEndPoint.Address.ToString();
                            if (!CheckRateLimit(senderIp))
                                continue;

                            OnDataReceived?.Invoke(result.Buffer, senderIp);
                        }
                        catch (ObjectDisposedException) { break; }
                        catch (Exception e)
                        {
                            if (_isListening) 
                                Debug.LogWarning($"[UdpBroadcastTransport] Listen error: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UdpBroadcastTransport] Failed to bind port {_port}: {e.Message}");
                }
                finally
                {
                    StopListening();
                }
            });

            Debug.Log($"[UdpBroadcastTransport] Listening on port {_port}");
        }

        public void StopListening()
        {
            _isListening = false;
            _listener?.Dispose();
            _listener = null;
        }

        public void Shutdown()
        {
            StopBroadcasting();
            StopListening();
            _rateLimitCounter.Clear();
        }

        private bool CheckRateLimit(string senderIp)
        {
            long currentTicks = DateTime.UtcNow.Ticks;
            if (currentTicks - _rateLimitResetTicks >= TimeSpan.TicksPerSecond)
            {
                _rateLimitCounter.Clear();
                _rateLimitResetTicks = currentTicks;
            }
            int count = _rateLimitCounter.AddOrUpdate(senderIp, 1, (_, c) => c + 1);
            return count <= _rateLimitPerSecond;
        }
    }
}
