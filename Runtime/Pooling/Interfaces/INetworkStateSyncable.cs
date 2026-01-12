namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Interface for pooled objects that can receive granular network state updates.
    /// </summary>
    public interface INetworkStateSyncable
    {
        /// <summary>
        /// Called when a networked property has been updated from the server.
        /// </summary>
        /// <param name="propertyName">Identifier for the property.</param>
        /// <param name="data">Serialized property value.</param>
        void OnNetworkStateUpdate(string propertyName, byte[] data);
    }
}
