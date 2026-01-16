namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Supported network transport protocols.
    /// </summary>
    public enum NetworkTransportType
    {
        UDP,
        TCP,
        WebSocket
    }

    /// <summary>
    /// Interface for network backends that support manual lifecycle management.
    /// </summary>
    public interface INetworkLifecycle
    {
        /// <summary>Starts listening as a server on the specified port.</summary>
        bool StartServer(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP);

        /// <summary>Connects to a server at the specified address and port.</summary>
        bool StartClient(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP);

        /// <summary>Starts as a host (server + local client).</summary>
        bool StartHost(string address, ushort port, NetworkTransportType transport = NetworkTransportType.UDP);

        /// <summary>Stops all network activity.</summary>
        void Stop();
    }
}
