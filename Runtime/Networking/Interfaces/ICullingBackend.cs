namespace Eraflo.Catalyst.Networking.Features.Culling
{
    /// <summary>
    /// Backend extension interface for network visibility control.
    /// Implement on your backend to enable interest management.
    /// </summary>
    public interface ICullingBackend
    {
        /// <summary>
        /// Shows a network object to a specific client.
        /// </summary>
        /// <param name="networkId">Catalyst network ID of the object.</param>
        /// <param name="clientId">Client to show the object to.</param>
        void NetworkShow(uint networkId, ulong clientId);
        
        /// <summary>
        /// Hides a network object from a specific client.
        /// </summary>
        /// <param name="networkId">Catalyst network ID of the object.</param>
        /// <param name="clientId">Client to hide the object from.</param>
        void NetworkHide(uint networkId, ulong clientId);
        
        /// <summary>
        /// Shows a network object to all clients.
        /// </summary>
        /// <param name="networkId">Catalyst network ID of the object.</param>
        void NetworkShowToAll(uint networkId);
        
        /// <summary>
        /// Hides a network object from all clients.
        /// </summary>
        /// <param name="networkId">Catalyst network ID of the object.</param>
        void NetworkHideFromAll(uint networkId);
        
        /// <summary>
        /// Checks if an object is visible to a client.
        /// </summary>
        bool IsVisibleTo(uint networkId, ulong clientId);
    }
}
