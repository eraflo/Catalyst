using System;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Factory for creating discovery transports based on type.
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// // Create from PackageSettings
    /// var transport = DiscoveryTransportFactory.CreateFromSettings();
    /// 
    /// // Create specific type
    /// var transport = DiscoveryTransportFactory.Create(DiscoveryTransportType.WebSocket);
    /// </code>
    /// </summary>
    public static class DiscoveryTransportFactory
    {
        /// <summary>
        /// Creates a transport based on PackageSettings configuration.
        /// </summary>
        public static IDiscoveryTransport CreateFromSettings()
        {
            return Create(PackageSettings.Instance.DiscoveryTransportType);
        }

        /// <summary>
        /// Creates a transport of the specified type.
        /// </summary>
        /// <param name="type">Transport type to create.</param>
        /// <returns>New transport instance.</returns>
        public static IDiscoveryTransport Create(DiscoveryTransportType type)
        {
            var settings = PackageSettings.Instance;
            
            return type switch
            {
                DiscoveryTransportType.UdpBroadcast => new UdpBroadcastTransport(
                    port: settings.DiscoveryPort,
                    maxMessageSize: settings.DiscoveryMaxMessageSize,
                    rateLimitPerSecond: settings.DiscoveryRateLimitPerSecond),
                    
                DiscoveryTransportType.WebSocket => new WebSocketDiscoveryTransport(
                    relayUrl: settings.DiscoveryRelayUrl),
                    
                DiscoveryTransportType.Mock => new MockDiscoveryTransport(),
                
                _ => throw new ArgumentException($"Unknown transport type: {type}")
            };
        }

        /// <summary>
        /// Creates the default transport (UDP broadcast).
        /// </summary>
        public static IDiscoveryTransport CreateDefault()
        {
            return Create(DiscoveryTransportType.UdpBroadcast);
        }
    }
}
