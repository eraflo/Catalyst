using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkOwnershipManagerTests
    {
        private NetworkManager _network;
        private NetworkOwnershipManager _ownership;

        [SetUp]
        public void SetUp()
        {
            _network = new NetworkManager();
            App.Register(_network);
            
            _ownership = new NetworkOwnershipManager();
            ((IGameService)_ownership).Initialize();
            App.Register(_ownership);
            
            _network.Backends.Register(new MockBackendFactory());
            _network.SetBackendById("mock");
            _network.Handlers.Register(_ownership);
        }

        [TearDown]
        public void TearDown()
        {
            _network.Stop();
            ((IGameService)_ownership).Shutdown();
            App.Shutdown();
        }

        [Test]
        public void OnNetworkDisconnected_ClearsOwnershipMap()
        {
            // Set some ownership
            _ownership.SetOwner(1, 100);
            Assert.AreEqual(100u, _ownership.GetOwner(1));
            
            // Disconnect
            _network.NotifyDisconnected();
            
            // Verify cleared
            Assert.AreEqual(0u, _ownership.GetOwner(1));
        }

        [Test]
        public void NetworkIdManager_WorksWithStructHandles()
        {
            var idManager = new NetworkIdManager();
            var handle = new Eraflo.Catalyst.Timers.TimerHandle(42, 1, 0);
            
            idManager.Register(10, handle);
            
            var retrieved = idManager.GetObject<Eraflo.Catalyst.Timers.TimerHandle>(10);
            Assert.AreEqual(handle, retrieved);
        }
    }
}
