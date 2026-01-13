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
            return HasAuthority(_network.LocalClientId, networkId, mode);
        }

        /// <summary>
        /// Checks if a specific client has authority over the object.
        /// </summary>
        public bool HasAuthority(ulong clientId, uint networkId, AuthorityMode mode)
        {
            if (!_network.IsConnected) return true;

            if (mode == AuthorityMode.ServerAuthoritative)
            {
                if (clientId == _network.LocalClientId)
                {
                    return _network.IsServer;
                }

                return clientId == _network.ServerClientId; 
            }
            
            return IsOwner(clientId, networkId);
        }

        /// <summary>
        /// Checks if the local client is the owner of the object.
        /// </summary>
        public bool IsOwner(uint networkId) => IsOwner(_network.LocalClientId, networkId);

        /// <summary>
        /// Checks if a specific client is the owner of the object.
        /// </summary>
        public bool IsOwner(ulong clientId, uint networkId)
        {
            if (!_network.IsConnected) return true;
            
            if (_ownershipMap.TryGetValue(networkId, out ulong ownerId))
            {
                return ownerId == clientId;
            }
            
            // Default to server if no owner registered
            return clientId == _network.ServerClientId;
        }

        /// <summary>
        /// Gets the owner client ID of an object.
        /// </summary>
        public ulong GetOwner(uint networkId)
        {
            return _ownershipMap.TryGetValue(networkId, out ulong ownerId) ? ownerId : _network.ServerClientId;
        }

        #region INetworkMessageHandler

        public void OnRegistered() { }
        public void OnUnregistered() { }
        public void OnNetworkConnected() { }
        
        public void OnNetworkDisconnected()
        {
            _ownershipMap.Clear();
        }

        #endregion
    }
}
