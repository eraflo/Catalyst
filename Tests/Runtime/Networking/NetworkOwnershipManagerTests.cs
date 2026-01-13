using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;

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

        [Test]
        public void HasAuthority_UsesServerClientId_NotHardcodedZero()
        {
            // Arrange: Set up mock backend with custom server ID
            var customBackend = new MockNetworkBackend(isServer: true, isClient: false, isConnected: true);
            _network.SetBackend(customBackend);

            // Server should have authority over its own objects
            _ownership.SetOwner(100, customBackend.ServerClientId);
            
            // Assert: Server has authority (using dynamic ServerClientId)
            Assert.IsTrue(_ownership.HasAuthority(100, AuthorityMode.ServerAuthoritative));
        }

        [Test]
        public void HasAuthority_ValidatesSenderId_ForClientAuthoritative()
        {
            // Arrange: Client 42 owns object 100
            _ownership.SetOwner(100, 42);
            
            // Assert: Client 42 has authority in ClientAuthoritative mode
            Assert.IsTrue(_ownership.HasAuthority(42, 100, AuthorityMode.ClientAuthoritative));
            
            // Assert: Client 99 does NOT have authority
            Assert.IsFalse(_ownership.HasAuthority(99, 100, AuthorityMode.ClientAuthoritative));
        }

        [Test]
        public void HasAuthority_RejectsUnauthorizedClient()
        {
            // Arrange: Client 10 owns object 50
            _ownership.SetOwner(50, 10);
            
            // Assert: Client 20 cannot modify in ClientAuthoritative
            Assert.IsFalse(_ownership.HasAuthority(20, 50, AuthorityMode.ClientAuthoritative));
        }
    }
}
