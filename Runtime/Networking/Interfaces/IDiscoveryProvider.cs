using System;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for server discovery providers.
    /// Allows different discovery mechanisms (LAN UDP, Relay, Steam, etc.)
    /// </summary>
    public interface IDiscoveryProvider
    {
        /// <summary>Provider name (e.g., "LAN", "Relay").</summary>
        string Name { get; }
        
        /// <summary>True if currently advertising a server.</summary>
        bool IsAdvertising { get; }
        
        /// <summary>True if currently scanning for servers.</summary>
        bool IsScanning { get; }
        
        /// <summary>Start advertising a server with the given info.</summary>
        void StartAdvertising(DiscoveryInfo info);
        
        /// <summary>Stop advertising.</summary>
        void StopAdvertising();
        
        /// <summary>Start scanning for servers.</summary>
        void StartScanning();
        
        /// <summary>Stop scanning.</summary>
        void StopScanning();
        
        /// <summary>Cleanup resources.</summary>
        void Shutdown();
        
        /// <summary>Fired when a server is discovered during scanning.</summary>
        event Action<DiscoveryInfo> OnServerFound;
    }
    
    /// <summary>
    /// Information about a discoverable server.
    /// </summary>
    public struct DiscoveryInfo
    {
        public string Id;
        public string Address;
        public string Name;
        public ushort Port;
        public int CurrentPlayers;
        public int MaxPlayers;
        public bool IsPasswordProtected;
        public Dictionary<string, string> Metadata;
    }
}
