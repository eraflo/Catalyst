namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Backend extension interface for network simulation support.
    /// Implement this on your backend to enable latency/packet loss simulation.
    /// </summary>
    public interface ISimulationBackend
    {
        /// <summary>
        /// Applies simulation parameters to the transport layer.
        /// </summary>
        /// <param name="latencyMs">Simulated one-way latency in milliseconds.</param>
        /// <param name="packetLossPercent">Packet loss percentage (0-100).</param>
        /// <param name="jitterMs">Latency variation in milliseconds.</param>
        void ApplySimulationParameters(int latencyMs, float packetLossPercent, int jitterMs);
        
        /// <summary>
        /// Gets current RTT (Round Trip Time) in milliseconds.
        /// </summary>
        float GetRTT();
        
        /// <summary>
        /// Gets current measured packet loss percentage.
        /// </summary>
        float GetPacketLoss();
        
        /// <summary>
        /// Gets current inbound bandwidth in KB/s.
        /// </summary>
        float GetBandwidthIn();
        
        /// <summary>
        /// Gets current outbound bandwidth in KB/s.
        /// </summary>
        float GetBandwidthOut();
    }
}
