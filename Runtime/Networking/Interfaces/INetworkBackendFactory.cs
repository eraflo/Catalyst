namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Factory interface for creating network backends.
    /// </summary>
    public interface INetworkBackendFactory
    {
        /// <summary>Unique ID (e.g., "netcode", "mirror").</summary>
        string Id { get; }
        
        /// <summary>Display name.</summary>
        string DisplayName { get; }
        
        /// <summary>Whether backend is available.</summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Called during bootstrapping. Return true if initialization succeeded immediately.
        /// Return false if initialization is deferred (e.g., waiting for dependencies).
        /// </summary>
        bool OnInitialize();
        
        /// <summary>Creates the backend instance.</summary>
        INetworkBackend Create();
    }
}
