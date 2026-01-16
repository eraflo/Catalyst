using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Service for discovering servers on the local network using UDP broadcast.
    /// </summary>
    [Service(Priority = 12)]
    public class NetworkDiscovery : IGameService
    {
        private const int DiscoveryPort = 47777;

        // Security limits (loaded from PackageSettings)
        private int MaxMessageSize => PackageSettings.Instance.DiscoveryMaxMessageSize;
        private int MaxNameLength => PackageSettings.Instance.DiscoveryMaxNameLength;
        private int RateLimitPerSecond => PackageSettings.Instance.DiscoveryRateLimitPerSecond;

        private UdpClient _advertiser;
        private UdpClient _scanner;
        private bool _isAdvertising;
        private bool _isScanning;
        private string _serverName = "Catalyst Game";

        // Rate limiting: thread-safe counter per IP
        private readonly ConcurrentDictionary<string, int> _rateLimitCounter = new();
        private long _rateLimitResetTicks;

        public event Action<DiscoveryInfo> OnServerFound;

        public struct DiscoveryInfo
        {
            public string Address;
            public string Name;
            public ushort Port;
            public int CurrentPlayers;
            public int MaxPlayers;
        }

        public void Initialize() { }
        public void Shutdown()
        {
            StopAdvertising();
            StopScanning();
            _rateLimitCounter.Clear();
        }

        public void StartAdvertising(string serverName, ushort gamePort, int currentPlayers = 0, int maxPlayers = 0)
        {
            if (_isAdvertising) return;
            _serverName = serverName;
            _isAdvertising = true;

            _advertiser = new UdpClient();
            _advertiser.EnableBroadcast = true;

            string message = $"CATALYST|{_serverName}|{gamePort}|{currentPlayers}|{maxPlayers}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            Task.Run(async () =>
            {
                while (_isAdvertising)
                {
                    try
                    {
                        if (_advertiser == null) break;
                        await _advertiser.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                        await Task.Delay(2000); // Pulse every 2 seconds
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception e)
                    {
                        if (_isAdvertising) Debug.LogError($"[NetworkDiscovery] Advertising error: {e.Message}");
                        break;
                    }
                }
            });

            Debug.Log($"[NetworkDiscovery] Advertising '{_serverName}' (Game Port: {gamePort}) via UDP Discovery Port {DiscoveryPort}");
        }

        public void StartScanning()
        {
            if (_isScanning) return;
            _isScanning = true;
            _rateLimitResetTicks = DateTime.UtcNow.Ticks;

            Task.Run(async () =>
            {
                try
                {
                    // Setup socket with ReuseAddress to allow multiple instances on same machine
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    if (PackageSettings.Instance.AllowDiscoveryPortSharing)
                    {
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    }

                    socket.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

                    _scanner = new UdpClient { Client = socket };

                    while (_isScanning)
                    {
                        try
                        {
                            var result = await _scanner.ReceiveAsync();

                            // Security: Check message size
                            if (result.Buffer.Length > MaxMessageSize)
                                continue;

                            // Security: Rate limiting per IP
                            string senderIp = result.RemoteEndPoint.Address.ToString();
                            if (!CheckRateLimit(senderIp))
                                continue;

                            string message = Encoding.UTF8.GetString(result.Buffer);

                            if (message.StartsWith("CATALYST|"))
                            {
                                var info = TryParseDiscoveryMessage(message, senderIp);
                                if (info.HasValue)
                                {
                                    OnServerFound?.Invoke(info.Value);
                                }
                            }
                        }
                        catch (ObjectDisposedException) { break; }
                        catch (Exception e)
                        {
                            if (_isScanning) Debug.LogWarning($"[NetworkDiscovery] Receive error: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkDiscovery] Failed to bind discovery port: {e.Message}");
                }
                finally
                {
                    StopScanning();
                }
            });

            Debug.Log("[NetworkDiscovery] Scanning for servers...");
        }

        public void StopAdvertising()
        {
            _isAdvertising = false;
            _advertiser?.Dispose();
            _advertiser = null;
        }

        public void StopScanning()
        {
            _isScanning = false;
            _scanner?.Dispose();
            _scanner = null;
        }

        /// <summary>
        /// Checks if a sender IP is within rate limits. Resets counters every second.
        /// </summary>
        private bool CheckRateLimit(string senderIp)
        {
            long currentTicks = DateTime.UtcNow.Ticks;
            long ticksPerSecond = TimeSpan.TicksPerSecond;

            // Reset counters every second (thread-safe)
            if (currentTicks - _rateLimitResetTicks >= ticksPerSecond)
            {
                _rateLimitCounter.Clear();
                _rateLimitResetTicks = currentTicks;
            }

            int count = _rateLimitCounter.AddOrUpdate(senderIp, 1, (_, c) => c + 1);
            return count <= RateLimitPerSecond;
        }

        /// <summary>
        /// Safely parses a discovery message using TryParse for all numeric fields.
        /// Returns null if parsing fails or data is invalid.
        /// </summary>
        private DiscoveryInfo? TryParseDiscoveryMessage(string message, string senderIp)
        {
            var parts = message.Split('|');
            if (parts.Length < 3)
                return null;

            string name = parts[1];

            // Validate name length
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
                return null;

            // Use TryParse for all numeric values
            if (!ushort.TryParse(parts[2], out ushort port))
                return null;

            int currentPlayers = 0;
            int maxPlayers = 0;

            if (parts.Length > 3 && !int.TryParse(parts[3], out currentPlayers))
                currentPlayers = 0;

            if (parts.Length > 4 && !int.TryParse(parts[4], out maxPlayers))
                maxPlayers = 0;

            return new DiscoveryInfo
            {
                Address = senderIp,
                Name = name,
                Port = port,
                CurrentPlayers = currentPlayers,
                MaxPlayers = maxPlayers
            };
        }
    }
}
