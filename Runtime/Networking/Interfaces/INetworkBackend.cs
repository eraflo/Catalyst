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
        void Send(ushort msgType, byte[] data, NetworkTarget target);
        
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
        /// Sends a message to a specific client by ID.
        /// </summary>
        /// <param name="msgType">Message type identifier.</param>
        /// <param name="data">Serialized message data.</param>
        /// <param name="clientId">Target client ID.</param>
        void SendToClient(ushort msgType, byte[] data, ulong clientId);

        /// <summary>
        /// Sends a message to multiple specific clients by ID.
        /// </summary>
        /// <param name="msgType">Message type identifier.</param>
        /// <param name="data">Serialized message data.</param>
        /// <param name="clientIds">Target client IDs.</param>
        void SendToClients(ushort msgType, byte[] data, ulong[] clientIds);

        /// <summary>
        /// Gets the local client ID.
        /// </summary>
        ulong LocalClientId { get; }
        
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
