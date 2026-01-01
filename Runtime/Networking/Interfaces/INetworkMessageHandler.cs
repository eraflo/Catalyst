namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for message handlers that process network messages for specific systems.
    /// </summary>
    public interface INetworkMessageHandler
    {
        /// <summary>
        /// Called when the handler is registered with the network system.
        /// </summary>
        void OnRegistered();
        
        /// <summary>
        /// Called when the handler is unregistered.
        /// </summary>
        void OnUnregistered();
        
        /// <summary>
        /// Called when the network connects.
        /// </summary>
        void OnNetworkConnected();
        
        /// <summary>
        /// Called when the network disconnects.
        /// </summary>
        void OnNetworkDisconnected();
    }
}
