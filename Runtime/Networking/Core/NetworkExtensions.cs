using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Universal network extension methods for retrieving Network IDs.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        /// Gets the network ID for a given object instance.
        /// </summary>
        public static uint GetNetworkId(this object instance)
        {
            var manager = App.Get<NetworkIdManager>();
            return manager?.GetId(instance) ?? 0;
        }

        /// <summary>
        /// Gets the network ID for a GameObject.
        /// </summary>
        public static uint GetNetworkId(this GameObject gameObject)
        {
            return GetNetworkId((object)gameObject);
        }

        /// <summary>
        /// Gets the network ID for a Component.
        /// </summary>
        public static uint GetNetworkId(this Component component)
        {
            return GetNetworkId((object)component);
        }
    }
}
