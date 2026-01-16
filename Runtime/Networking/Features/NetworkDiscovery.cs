using System;
using Eraflo.Catalyst.Networking.Features.Discovery;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Service for server discovery. Delegates to an IDiscoveryProvider.
    /// </summary>
    [Service(Priority = 12)]
    public class NetworkDiscovery : IGameService
    {
        private IDiscoveryProvider _provider;

        /// <summary>Current provider name.</summary>
        public string ProviderName => _provider?.Name ?? "None";
        
        /// <summary>True if currently advertising.</summary>
        public bool IsAdvertising => _provider?.IsAdvertising ?? false;
        
        /// <summary>True if currently scanning.</summary>
        public bool IsScanning => _provider?.IsScanning ?? false;

        /// <summary>Fired when a server is discovered.</summary>
        public event Action<DiscoveryInfo> OnServerFound;

        public void Initialize()
        {
            // Set default LAN provider
            SetProvider(new LanDiscoveryProvider());
        }

        public void Shutdown()
        {
            _provider?.Shutdown();
            _provider = null;
        }

        /// <summary>Sets the discovery provider.</summary>
        public void SetProvider(IDiscoveryProvider provider)
        {
            if (_provider != null)
            {
                _provider.OnServerFound -= HandleServerFound;
                _provider.Shutdown();
            }

            _provider = provider;

            if (_provider != null)
            {
                _provider.OnServerFound += HandleServerFound;
            }

            Debug.Log($"[NetworkDiscovery] Provider set to: {provider?.Name ?? "none"}");
        }

        /// <summary>Start advertising a server.</summary>
        public void StartAdvertising(DiscoveryInfo info)
        {
            if (_provider == null)
            {
                Debug.LogWarning("[NetworkDiscovery] No provider set.");
                return;
            }
            _provider.StartAdvertising(info);
        }

        /// <summary>Start advertising with simple parameters (legacy API).</summary>
        public void StartAdvertising(string serverName, ushort gamePort, int currentPlayers = 0, int maxPlayers = 0)
        {
            StartAdvertising(new DiscoveryInfo
            {
                Name = serverName,
                Port = gamePort,
                CurrentPlayers = currentPlayers,
                MaxPlayers = maxPlayers
            });
        }

        /// <summary>Stop advertising.</summary>
        public void StopAdvertising() => _provider?.StopAdvertising();

        /// <summary>Start scanning for servers.</summary>
        public void StartScanning()
        {
            if (_provider == null)
            {
                Debug.LogWarning("[NetworkDiscovery] No provider set.");
                return;
            }
            _provider.StartScanning();
        }

        /// <summary>Stop scanning.</summary>
        public void StopScanning() => _provider?.StopScanning();

        private void HandleServerFound(DiscoveryInfo info)
        {
            OnServerFound?.Invoke(info);
        }
    }
}
