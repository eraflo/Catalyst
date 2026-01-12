using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Extension methods for networked timers.
    /// </summary>
    public static class TimerNetworkExtensions
    {
        /// <summary>
        /// Makes a timer networked. Auto-registers with the handler.
        /// </summary>
        public static uint MakeNetworked(this TimerHandle handle, AuthorityMode authority = AuthorityMode.ServerAuthoritative)
        {
            var network = App.Get<NetworkManager>();
            var handler = network?.Handlers.Get<TimerNetworkHandler>();
            if (handler == null) return 0;
            return handler.MakeNetworked(handle, authority);
        }

        /// <summary>
        /// Removes networking from a timer.
        /// </summary>
        public static void RemoveNetworking(this TimerHandle handle)
        {
            var network = App.Get<NetworkManager>();
            var handler = network?.Handlers.Get<TimerNetworkHandler>();
            handler?.Remove(handle);
        }

        /// <summary>
        /// Gets the network ID for a timer.
        /// </summary>
        public static uint GetNetworkId(this TimerHandle handle)
        {
            return Eraflo.Catalyst.Networking.NetworkExtensions.GetNetworkId(handle);
        }

        /// <summary>
        /// Broadcasts sync for all networked timers.
        /// </summary>
        public static void BroadcastTimerSync()
        {
            var network = App.Get<NetworkManager>();
            var handler = network?.Handlers.Get<TimerNetworkHandler>();
            handler?.BroadcastSync();
        }
    }
}
