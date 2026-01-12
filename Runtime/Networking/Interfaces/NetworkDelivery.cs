namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Delivery guarantees for network messages.
    /// </summary>
    public enum NetworkDelivery
    {
        /// <summary>Unreliable, no guarantee of delivery or order (best for high-frequency data like positions).</summary>
        Unreliable = 0,
        
        /// <summary>Reliable, guaranteed delivery and order (best for one-time events like spawning).</summary>
        Reliable = 1,
        
        /// <summary>Unreliable but sequenced (newer packets discard older ones).</summary>
        UnreliableSequenced = 2,
        
        /// <summary>Reliable and sequenced (standard reliable stream).</summary>
        ReliableSequenced = 3,
        
        /// <summary>Reliable but message order is not guaranteed.</summary>
        ReliableFragmented = 4
    }
}
