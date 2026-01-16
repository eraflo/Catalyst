namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for discovery transport mechanisms.
    /// 
    /// <para><b>Purpose:</b></para>
    /// Abstracts the low-level network transport used for server discovery.
    /// This allows different transport implementations (UDP broadcast, WebSocket, 
    /// platform-specific APIs) without changing the discovery logic.
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="UdpBroadcastTransport"/> - LAN discovery via UDP broadcast</item>
    ///   <item>WebSocketTransport - Cloud/Relay discovery (future)</item>
    ///   <item>MockDiscoveryTransport - Unit testing</item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// var transport = new UdpBroadcastTransport(47777);
    /// transport.OnDataReceived += (data, sender) => ProcessDiscovery(data);
    /// transport.StartListening();
    /// </code>
    /// </summary>
    public interface IDiscoveryTransport
    {
        /// <summary>
        /// Transport name for logging/debugging (e.g., "UDP Broadcast", "WebSocket").
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Whether the transport is currently broadcasting data.
        /// </summary>
        bool IsBroadcasting { get; }
        
        /// <summary>
        /// Whether the transport is currently listening for incoming data.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Starts broadcasting data at regular intervals.
        /// </summary>
        /// <param name="data">Raw bytes to broadcast.</param>
        /// <param name="intervalMs">Interval between broadcasts in milliseconds.</param>
        void StartBroadcasting(byte[] data, int intervalMs);
        
        /// <summary>
        /// Stops broadcasting.
        /// </summary>
        void StopBroadcasting();
        
        /// <summary>
        /// Starts listening for incoming discovery data.
        /// </summary>
        void StartListening();
        
        /// <summary>
        /// Stops listening.
        /// </summary>
        void StopListening();
        
        /// <summary>
        /// Releases all resources.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Fired when data is received from a remote sender.
        /// </summary>
        /// <param name="data">Raw received bytes.</param>
        /// <param name="senderAddress">IP address or identifier of the sender.</param>
        event System.Action<byte[], string> OnDataReceived;
    }
}
