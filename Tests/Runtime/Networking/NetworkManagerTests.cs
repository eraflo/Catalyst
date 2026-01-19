using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Timers;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkManagerTests
    {
        private MockNetworkBackend _mockBackend;

        [SetUp]
        public void SetUp()
        {
            App.Get<NetworkManager>().Reset();
            App.Get<NetworkManager>().Backends.Register(new MockBackendFactory());
            
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            App.Get<NetworkManager>().SetBackend(_mockBackend);
            
            // Register test handlers
            App.Get<NetworkManager>().Handlers.Register(new TimerNetworkHandler());
            App.Get<NetworkManager>().Handlers.Register(new PoolNetworkHandler());
        }

        [TearDown]
        public void TearDown()
        {
            App.Get<NetworkManager>().Reset();
        }

        #region State

        [Test]
        public void HasBackend_ReturnsTrue_WhenSet()
        {
            Assert.IsTrue(App.Get<NetworkManager>().HasBackend);
        }

        [Test]
        public void HasBackend_ReturnsFalse_WhenCleared()
        {
            App.Get<NetworkManager>().SetBackend(null);
            Assert.IsFalse(App.Get<NetworkManager>().HasBackend);
        }

        [Test]
        public void IsHost_True_WhenServerAndClient()
        {
            Assert.IsTrue(App.Get<NetworkManager>().IsHost);
        }

        [Test]
        public void ServerClientId_ReturnsDynamicId_FromBackend()
        {
            // MockBackend returns 0 as ServerClientId by default
            Assert.AreEqual(0ul, App.Get<NetworkManager>().ServerClientId);
        }

        #endregion

        #region Lifecycle

        [Test]
        public void StartServer_CallsBackend()
        {
            var success = App.Get<NetworkManager>().StartServer();
            Assert.IsTrue(success);
            Assert.IsTrue(_mockBackend.IsServer);
        }

        [Test]
        public void StartClient_CallsBackend()
        {
            var success = App.Get<NetworkManager>().StartClient();
            Assert.IsTrue(success);
            Assert.IsTrue(_mockBackend.IsClient);
        }

        [Test]
        public void StartHost_CallsBackend()
        {
            var success = App.Get<NetworkManager>().StartHost();
            Assert.IsTrue(success);
            Assert.IsTrue(_mockBackend.IsServer);
            Assert.IsTrue(_mockBackend.IsClient);
        }

        [Test]
        public void Stop_CallsBackend()
        {
            App.Get<NetworkManager>().StartHost();
            App.Get<NetworkManager>().Stop();
            
            // MockBackend Shutdown should likely clear flags or similar, 
            // but the test confirms the proxy call doesn't crash.
            // For MockBackend, we can verify IsConnected or similar.
            Assert.IsFalse(_mockBackend.IsConnected);
        }

        #endregion

        #region Backends Registry

        [Test]
        public void Backends_Register_AddsFactory()
        {
            var factory = App.Get<NetworkManager>().Backends.Get("mock");
            Assert.IsNotNull(factory);
        }

        [Test]
        public void SetBackendById_UsesRegistry()
        {
            App.Get<NetworkManager>().SetBackend(null);
            Assert.IsTrue(App.Get<NetworkManager>().SetBackendById("mock"));
        }

        #endregion

        #region Handler Registry

        [Test]
        public void Handlers_Get_ReturnsRegisteredHandler()
        {
            var handler = App.Get<NetworkManager>().Handlers.Get<TimerNetworkHandler>();
            Assert.IsNotNull(handler);
        }

        [Test]
        public void Handlers_Get_ReturnsNull_WhenNotRegistered()
        {
            App.Get<NetworkManager>().Reset();
            var handler = App.Get<NetworkManager>().Handlers.Get<TimerNetworkHandler>();
            Assert.IsNull(handler);
        }

        #endregion

        #region Messaging

        [Test]
        public void Send_InvokesHandler_WithLoopback()
        {
            bool received = false;
            App.Get<NetworkManager>().On<TestMessage>(m => received = true);
            App.Get<NetworkManager>().Send(new TestMessage { Value = 42 });
            Assert.IsTrue(received);
        }

        [Test]
        public void Off_RemovesHandler()
        {
            int count = 0;
            void Handler(TestMessage m) => count++;

            App.Get<NetworkManager>().On<TestMessage>(Handler);
            App.Get<NetworkManager>().Send(new TestMessage());
            App.Get<NetworkManager>().Off<TestMessage>(Handler);
            App.Get<NetworkManager>().Send(new TestMessage());
            
            Assert.AreEqual(1, count);
        }

        #endregion

        #region Extension Methods

        [Test]
        public void Timer_MakeNetworked_UsesHandler()
        {
            var handle = App.Get<Timer>().CreateTimer<CountdownTimer>(1f);
            var id = handle.MakeNetworked();
            Assert.Greater(id, 0u);
            handle.RemoveNetworking();
        }

        #endregion

        #region Serialization

        [Test]
        public void Serialize_PreservesData()
        {
            var original = new TestMessage { Value = 123, Name = "Test" };
            var bytes = NetworkSerializer.Serialize(original);
            var restored = NetworkSerializer.Deserialize<TestMessage>(bytes);
            
            Assert.AreEqual(original.Value, restored.Value);
            Assert.AreEqual(original.Name, restored.Name);
        }

        #endregion

        #region Client Targeting

        [Test]
        public void LocalClientId_ReturnsBackendClientId()
        {
            Assert.AreEqual(0ul, App.Get<NetworkManager>().LocalClientId);
        }

        [Test]
        public void SendToClient_DeliveresToSpecificClient()
        {
            bool received = false;
            App.Get<NetworkManager>().On<TestMessage>(m => received = true);
            
            // Mock loopback simulates self-delivery
            App.Get<NetworkManager>().SendToClient(new TestMessage { Value = 1 }, _mockBackend.LocalClientId);
            
            Assert.IsTrue(received);
        }

        [Test]
        public void SendToClients_DeliversToMultipleClients()
        {
            int count = 0;
            App.Get<NetworkManager>().On<TestMessage>(m => count++);
            
            // Send to self twice (loopback hits each time targeting self)
            App.Get<NetworkManager>().SendToClients(new TestMessage { Value = 1 }, 
                _mockBackend.LocalClientId, _mockBackend.LocalClientId);
            
            Assert.AreEqual(2, count);
        }

        [Test]
        public void SendToClient_DoesNothing_WhenNotServer()
        {
            _mockBackend.SetServerState(false);
            bool received = false;
            App.Get<NetworkManager>().On<TestMessage>(m => received = true);
            
            App.Get<NetworkManager>().SendToClient(new TestMessage { Value = 1 }, 0);
            
            Assert.IsFalse(received);
        }

        #endregion

        #region Helpers

        private struct TestMessage : INetworkMessage
        {
            public int Value;
            public string Name;

            public void Serialize(System.IO.BinaryWriter w)
            {
                w.Write(Value);
                w.Write(Name ?? "");
            }

            public void Deserialize(System.IO.BinaryReader r)
            {
                Value = r.ReadInt32();
                Name = r.ReadString();
            }
        }

        #endregion
    }
}
