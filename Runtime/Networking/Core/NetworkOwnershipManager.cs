using System.Collections.Generic;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Synchronizes object ownership and authority.
    /// </summary>
    [Service(Priority = 3)]
    public class NetworkOwnershipManager : IGameService, INetworkMessageHandler
    {
        private readonly Dictionary<uint, ulong> _ownershipMap = new Dictionary<uint, ulong>();
        private NetworkManager _network;

        public void Initialize()
        {
            _network = App.Get<NetworkManager>();
        }

        public void Shutdown()
        {
            _ownershipMap.Clear();
        }

        /// <summary>
        /// Registers a client as the owner of a networked object.
        /// </summary>
        public void SetOwner(uint networkId, ulong clientId)
        {
            _ownershipMap[networkId] = clientId;
        }

        /// <summary>
        /// Removes ownership tracking for an object.
        /// </summary>
        public void RemoveOwner(uint networkId)
        {
            _ownershipMap.Remove(networkId);
        }

        /// <summary>
        /// Checks if the local instance has authority over the object.
        /// </summary>
        public bool HasAuthority(uint networkId, AuthorityMode mode)
        {
            if (mode == AuthorityMode.ServerAuthoritative)
            {
                return _network.IsServer;
            }
            
            return IsOwner(networkId);
        }

        /// <summary>
        /// Checks if the local client is the owner of the object.
        /// </summary>
        public bool IsOwner(uint networkId)
        {
            if (!_network.IsConnected) return true; // Local fallback
            
            if (_ownershipMap.TryGetValue(networkId, out ulong ownerId))
            {
                return ownerId == _network.LocalClientId;
            }
            
            // Default to server if no owner registered
            return _network.IsServer;
        }

        /// <summary>
        /// Gets the owner client ID of an object.
        /// </summary>
        public ulong GetOwner(uint networkId)
        {
            return _ownershipMap.TryGetValue(networkId, out ulong ownerId) ? ownerId : 0;
        }
    }
}
