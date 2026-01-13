using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst;

namespace Eraflo.Catalyst.Networking.Backends.Mock
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
            App.Get<NetworkManager>().SetBackendById("mock");
            return true;
        }
        
        public INetworkBackend Create()
        {
            return new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
        }
    }
}
