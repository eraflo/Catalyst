using System;
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
    [Service(Priority = 7)]
    public class NetworkDiscovery : IGameService
    {
        private const int DiscoveryPort = 47777;
        private UdpClient _udpClient;
        private bool _isAdvertising;
        private bool _isScanning;
        private string _serverName = "Catalyst Game";
        
        public event Action<DiscoveryInfo> OnServerFound;

        public struct DiscoveryInfo
        {
            public string Address;
            public string Name;
            public ushort Port;
        }

        public void Initialize() { }
        public void Shutdown() => StopAll();

        public void StartAdvertising(string serverName, ushort gamePort)
        {
            if (_isAdvertising) return;
            _serverName = serverName;
            _isAdvertising = true;
            
            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;
            
            string message = $"CATALYST|{_serverName}|{gamePort}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            
            Task.Run(async () =>
            {
                while (_isAdvertising)
                {
                    try
                    {
                        await _udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                        await Task.Delay(2000); // Pulse every 2 seconds
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[NetworkDiscovery] Advertising error: {e.Message}");
                        break;
                    }
                }
            });
            
            Debug.Log($"[NetworkDiscovery] Advertising as '{_serverName}' on port {DiscoveryPort}");
        }

        public void StartScanning()
        {
            if (_isScanning) return;
            _isScanning = true;
            
            var listener = new UdpClient(DiscoveryPort);
            
            Task.Run(async () =>
            {
                while (_isScanning)
                {
                    try
                    {
                        var result = await listener.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);
                        
                        if (message.StartsWith("CATALYST|"))
                        {
                            var parts = message.Split('|');
                            if (parts.Length >= 3)
                            {
                                var info = new DiscoveryInfo
                                {
                                    Address = result.RemoteEndPoint.Address.ToString(),
                                    Name = parts[1],
                                    Port = ushort.Parse(parts[2])
                                };
                                OnServerFound?.Invoke(info);
                            }
                        }
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception e)
                    {
                        Debug.LogError($"[NetworkDiscovery] Scanning error: {e.Message}");
                    }
                }
                listener.Dispose();
            });
            
            Debug.Log("[NetworkDiscovery] Scanning for servers...");
        }

        public void StopAll()
        {
            _isAdvertising = false;
            _isScanning = false;
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }
}
