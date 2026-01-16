using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Discovery
{
    /// <summary>
    /// LAN discovery provider using an <see cref="IDiscoveryTransport"/>.
    /// 
    /// <para><b>Purpose:</b></para>
    /// Discovers game servers on the local network by broadcasting server 
    /// information and listening for responses.
    /// 
    /// <para><b>Protocol:</b></para>
    /// Messages use format: <c>CATALYST|Name|Port|Players|MaxPlayers|HasPassword</c>
    /// 
    /// <para><b>Security:</b></para>
    /// <list type="bullet">
    ///   <item>Rate limiting per IP (configurable via PackageSettings)</item>
    ///   <item>Max message size (prevents buffer overflow)</item>
    ///   <item>Max name length (prevents spam)</item>
    /// </list>
    /// </summary>
    public class LanDiscoveryProvider : IDiscoveryProvider
    {
        public string Name => "LAN";
        public bool IsAdvertising => _transport?.IsBroadcasting ?? false;
        public bool IsScanning => _transport?.IsListening ?? false;

        private const int BroadcastIntervalMs = 2000;
        
        private readonly IDiscoveryTransport _transport;
        private readonly int _maxNameLength;
        private DiscoveryInfo _currentInfo;

        public event Action<DiscoveryInfo> OnServerFound;

        /// <summary>
        /// Creates a LAN discovery provider with the transport from PackageSettings.
        /// </summary>
        public LanDiscoveryProvider() : this(DiscoveryTransportFactory.CreateFromSettings())
        {
        }

        /// <summary>
        /// Creates a LAN discovery provider with a custom transport.
        /// </summary>
        /// <param name="transport">The transport to use for discovery.</param>
        public LanDiscoveryProvider(IDiscoveryTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _maxNameLength = PackageSettings.Instance.DiscoveryMaxNameLength;
            _transport.OnDataReceived += HandleDataReceived;
        }

        public void StartAdvertising(DiscoveryInfo info)
        {
            _currentInfo = info;
            
            // Format: CATALYST|Name|Port|Players|MaxPlayers|HasPassword
            string hasPassword = info.IsPasswordProtected ? "1" : "0";
            string message = $"CATALYST|{info.Name}|{info.Port}|{info.CurrentPlayers}|{info.MaxPlayers}|{hasPassword}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            
            _transport.StartBroadcasting(data, BroadcastIntervalMs);
            Debug.Log($"[LanDiscoveryProvider] Advertising '{info.Name}' via {_transport.Name}");
        }

        public void StopAdvertising()
        {
            _transport.StopBroadcasting();
        }

        public void StartScanning()
        {
            _transport.StartListening();
            Debug.Log($"[LanDiscoveryProvider] Scanning via {_transport.Name}");
        }

        public void StopScanning()
        {
            _transport.StopListening();
        }

        public void Shutdown()
        {
            _transport.OnDataReceived -= HandleDataReceived;
            _transport.Shutdown();
        }

        private void HandleDataReceived(byte[] data, string senderAddress)
        {
            try
            {
                string message = Encoding.UTF8.GetString(data);
                
                if (!message.StartsWith("CATALYST|"))
                    return;

                var info = TryParseMessage(message, senderAddress);
                if (info.HasValue)
                {
                    OnServerFound?.Invoke(info.Value);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscoveryProvider] Parse error: {e.Message}");
            }
        }

        private DiscoveryInfo? TryParseMessage(string message, string senderIp)
        {
            var parts = message.Split('|');
            if (parts.Length < 3)
                return null;

            string name = parts[1];
            if (string.IsNullOrEmpty(name) || name.Length > _maxNameLength)
                return null;

            if (!ushort.TryParse(parts[2], out ushort port))
                return null;

            int currentPlayers = 0;
            int maxPlayers = 0;
            bool hasPassword = false;

            if (parts.Length > 3) int.TryParse(parts[3], out currentPlayers);
            if (parts.Length > 4) int.TryParse(parts[4], out maxPlayers);
            if (parts.Length > 5) hasPassword = parts[5] == "1";

            return new DiscoveryInfo
            {
                Id = $"{senderIp}:{port}",
                Address = senderIp,
                Name = name,
                Port = port,
                CurrentPlayers = currentPlayers,
                MaxPlayers = maxPlayers,
                IsPasswordProtected = hasPassword
            };
        }
    }
}
