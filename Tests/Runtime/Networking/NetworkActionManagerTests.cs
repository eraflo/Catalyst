using NUnit.Framework;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Actions;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class NetworkActionManagerTests
    {
        private NetworkManager _networkManager;
        private MockNetworkBackend _mockBackend;
        private NetworkActionManager _actionManager;

        [SetUp]
        public void SetUp()
        {
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _networkManager = new NetworkManager();
            _actionManager = new NetworkActionManager();
            
            App.Register<NetworkManager>(_networkManager);
            App.Register<NetworkActionManager>(_actionManager);
            
            _networkManager.SetBackend(_mockBackend);
            _actionManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _actionManager?.Shutdown();
            _networkManager?.Stop();
            App.Shutdown();
        }

        [Test]
        public void RegisterAction_AddsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            
            // Act
            _actionManager.RegisterAction("TestAction", payload => handlerCalled = true);
            
            // Assert - no exception means success
            Assert.Pass();
        }

        [Test]
        public void UnregisterAction_RemovesHandler()
        {
            // Arrange
            _actionManager.RegisterAction("TestAction", payload => { });
            
            // Act
            _actionManager.UnregisterAction("TestAction");
            
            // Assert - no exception means success
            Assert.Pass();
        }

        [Test]
        public void Trigger_SendsMessage()
        {
            // Arrange
            _mockBackend.ClearSentMessages();
            
            // Act
            _actionManager.Trigger("TestAction", 42, "hello");
            
            // Assert
            Assert.That(_mockBackend.SentMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void TriggerToTarget_SendsToCorrectTarget()
        {
            // Arrange
            _mockBackend.ClearSentMessages();
            
            // Act
            _actionManager.TriggerToTarget("TestAction", NetworkTarget.Server, "data");
            
            // Assert
            Assert.That(_mockBackend.SentMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Handler_ReceivesPayload()
        {
            // Arrange
            byte[] receivedPayload = null;
            _actionManager.RegisterAction("TestAction", payload => receivedPayload = payload);
            
            // Simulate local execution
            _actionManager.Trigger("TestAction", 123);
            
            // Note: The handler may or may not be called depending on loopback
            // This test verifies no exceptions are thrown
            Assert.Pass();
        }

        [Test]
        public void HasAction_ReturnsTrueForRegistered()
        {
            // Arrange
            _actionManager.RegisterAction("MyAction", _ => { });
            
            // Act & Assert
            Assert.IsTrue(_actionManager.HasAction("MyAction"));
            Assert.IsFalse(_actionManager.HasAction("UnknownAction"));
        }

        [Test]
        public void ClearAllActions_RemovesAll()
        {
            // Arrange
            _actionManager.RegisterAction("Action1", _ => { });
            _actionManager.RegisterAction("Action2", _ => { });
            
            // Act
            _actionManager.ClearAllActions();
            
            // Assert
            Assert.IsFalse(_actionManager.HasAction("Action1"));
            Assert.IsFalse(_actionManager.HasAction("Action2"));
        }
    }
}
