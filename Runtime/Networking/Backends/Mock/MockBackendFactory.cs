namespace Eraflo.UnityImportPackage.Networking.Backends
{
    /// <summary>
    /// Factory for Mock backend.
    /// </summary>
    public class MockBackendFactory : INetworkBackendFactory
    {
        public string Id => "mock";
        public string DisplayName => "Mock Backend";
        public bool IsAvailable => true;
        
        public bool OnInitialize()
        {
            // Mock backend initializes immediately
            return NetworkManager.SetBackendById("mock");
        }
        
        public INetworkBackend Create()
        {
            return new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
        }
    }
}
