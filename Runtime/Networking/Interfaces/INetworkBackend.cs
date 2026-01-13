using System;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for network backend implementations.
    /// Implement this to integrate with your networking solution (Netcode, Mirror, Photon, etc.).
    /// </summary>
    public interface INetworkBackend
    {
        /// <summary>Whether the local instance is the server/host.</summary>
        bool IsServer { get; }
        
        /// <summary>Whether the local instance is a client.</summary>
        bool IsClient { get; }
        
        /// <summary>Whether the network is currently connected.</summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Sends a message over the network.
        /// </summary>
        /// <param name="msgType">Message type identifier.</param>
        /// <param name="data">Serialized message data.</param>
        /// <param name="target">Target recipients.</param>
        /// <param name="delivery">Delivery guarantee.</param>
        void Send(ushort msgType, byte[] data, NetworkTarget target, NetworkDelivery delivery = NetworkDelivery.Reliable);
        
        /// <summary>
        /// Registers a handler for incoming messages.
        /// </summary>
        /// <param name="msgType">Message type to handle.</param>
        /// <param name="handler">Callback receiving (data, senderId).</param>
        void RegisterHandler(ushort msgType, Action<byte[], ulong> handler);
        
        /// <summary>
        /// Unregisters a message handler.
        /// </summary>
        void UnregisterHandler(ushort msgType);

        /// <summary>
        /// Sends a message to a specific client (Server only).
        /// </summary>
        void SendToClient(ushort msgType, byte[] data, ulong clientId, NetworkDelivery delivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Sends a message to multiple specific clients (Server only).
        /// </summary>
        void SendToClients(ushort msgType, byte[] data, ulong[] clientIds, NetworkDelivery delivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Gets the local client ID.
        /// </summary>
        ulong LocalClientId { get; }

        /// <summary>
        /// Gets the server's client ID.
        /// </summary>
        ulong ServerClientId { get; }

        /// <summary>True if the backend handles its own GameObject replication (e.g. NGO).</summary>
        bool SupportsNativeGameObjectReplication { get; }

        /// <summary>
        /// Called when the backend is set as active.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Called when the backend is being replaced or shutdown.
        /// </summary>
        void Shutdown();
    }
}
