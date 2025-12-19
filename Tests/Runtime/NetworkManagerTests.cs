using NUnit.Framework;
using Eraflo.UnityImportPackage.Networking;
using Eraflo.UnityImportPackage.Networking.Backends;
using Eraflo.UnityImportPackage.Timers;
using Eraflo.UnityImportPackage.Pooling;

namespace Eraflo.UnityImportPackage.Tests
{
    public class NetworkManagerTests
    {
        private MockNetworkBackend _mockBackend;

        [SetUp]
        public void SetUp()
        {
            NetworkManager.Reset();
            NetworkManager.Backends.Register(new MockBackendFactory());
            
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            NetworkManager.SetBackend(_mockBackend);
            
            // Register test handlers
            NetworkManager.Handlers.Register(new TimerNetworkHandler());
            NetworkManager.Handlers.Register(new PoolNetworkHandler());
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManager.Reset();
        }

        #region State

        [Test]
        public void HasBackend_ReturnsTrue_WhenSet()
        {
            Assert.IsTrue(NetworkManager.HasBackend);
        }

        [Test]
        public void HasBackend_ReturnsFalse_WhenCleared()
        {
            NetworkManager.ClearBackend();
            Assert.IsFalse(NetworkManager.HasBackend);
        }

        [Test]
        public void IsHost_True_WhenServerAndClient()
        {
            Assert.IsTrue(NetworkManager.IsHost);
        }

        #endregion

        #region Backends Registry

        [Test]
        public void Backends_Register_AddsFactory()
        {
            var factory = NetworkManager.Backends.Get("mock");
            Assert.IsNotNull(factory);
        }

        [Test]
        public void SetBackendById_UsesRegistry()
        {
            NetworkManager.ClearBackend();
            Assert.IsTrue(NetworkManager.SetBackendById("mock"));
        }

        #endregion

        #region Handler Registry

        [Test]
        public void Handlers_Get_ReturnsRegisteredHandler()
        {
            var handler = NetworkManager.Handlers.Get<TimerNetworkHandler>();
            Assert.IsNotNull(handler);
        }

        [Test]
        public void Handlers_Get_ReturnsNull_WhenNotRegistered()
        {
            NetworkManager.Reset();
            var handler = NetworkManager.Handlers.Get<TimerNetworkHandler>();
            Assert.IsNull(handler);
        }

        #endregion

        #region Messaging

        [Test]
        public void Send_InvokesHandler_WithLoopback()
        {
            bool received = false;
            NetworkManager.On<TestMessage>(m => received = true);
            NetworkManager.Send(new TestMessage { Value = 42 });
            Assert.IsTrue(received);
        }

        [Test]
        public void Off_RemovesHandler()
        {
            int count = 0;
            void Handler(TestMessage m) => count++;

            NetworkManager.On<TestMessage>(Handler);
            NetworkManager.Send(new TestMessage());
            NetworkManager.Off<TestMessage>(Handler);
            NetworkManager.Send(new TestMessage());
            
            Assert.AreEqual(1, count);
        }

        #endregion

        #region Extension Methods

        [Test]
        public void Timer_MakeNetworked_UsesHandler()
        {
            var handle = Timer.Create<CountdownTimer>(1f);
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
            Assert.AreEqual(0ul, NetworkManager.LocalClientId);
        }

        [Test]
        public void SendToClient_DeliveresToSpecificClient()
        {
            bool received = false;
            NetworkManager.On<TestMessage>(m => received = true);
            
            // Mock loopback simulates self-delivery
            NetworkManager.SendToClient(new TestMessage { Value = 1 }, _mockBackend.LocalClientId);
            
            Assert.IsTrue(received);
        }

        [Test]
        public void SendToClients_DeliversToMultipleClients()
        {
            int count = 0;
            NetworkManager.On<TestMessage>(m => count++);
            
            // Send to self twice (loopback hits each time targeting self)
            NetworkManager.SendToClients(new TestMessage { Value = 1 }, 
                _mockBackend.LocalClientId, _mockBackend.LocalClientId);
            
            Assert.AreEqual(2, count);
        }

        [Test]
        public void SendToClient_DoesNothing_WhenNotServer()
        {
            _mockBackend.SetServerState(false);
            bool received = false;
            NetworkManager.On<TestMessage>(m => received = true);
            
            NetworkManager.SendToClient(new TestMessage { Value = 1 }, 0);
            
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
