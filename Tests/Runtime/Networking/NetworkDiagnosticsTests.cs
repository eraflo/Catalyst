using NUnit.Framework;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Diagnostics;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class NetworkDiagnosticsTests
    {
        private NetworkManager _networkManager;
        private MockNetworkBackend _mockBackend;
        private NetworkDiagnostics _diagnostics;

        [SetUp]
        public void SetUp()
        {
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _networkManager = new NetworkManager();
            _diagnostics = new NetworkDiagnostics();
            
            App.Register<NetworkManager>(_networkManager);
            App.Register<NetworkDiagnostics>(_diagnostics);
            
            _networkManager.SetBackend(_mockBackend);
            _diagnostics.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _diagnostics?.Shutdown();
            _networkManager?.Stop();
            App.Shutdown();
        }

        [Test]
        public void SetSimulation_AppliesParametersToBackend()
        {
            // Act
            _diagnostics.SetSimulation(100, 5f, 20);
            
            // Assert
            var (latency, loss, jitter) = _mockBackend.GetSimulationParameters();
            Assert.AreEqual(100, latency);
            Assert.AreEqual(5f, loss);
            Assert.AreEqual(20, jitter);
        }

        [Test]
        public void DisableSimulation_ClearsParameters()
        {
            // Arrange
            _diagnostics.SetSimulation(100, 5f, 20);
            
            // Act
            _diagnostics.DisableSimulation();
            
            // Assert
            var (latency, loss, jitter) = _mockBackend.GetSimulationParameters();
            Assert.AreEqual(0, latency);
            Assert.AreEqual(0f, loss);
            Assert.AreEqual(0, jitter);
        }

        [Test]
        public void GetMetricsString_ReturnsFormattedString()
        {
            // Arrange
            _mockBackend.SetMockRTT(45.5f);
            _mockBackend.SetMockPacketLoss(0.1f);
            _mockBackend.SetMockBandwidth(12.5f, 8.3f);
            
            // Act
            string metrics = _diagnostics.GetMetricsString();
            
            // Assert
            Assert.That(metrics, Does.Contain("RTT"));
            Assert.That(metrics, Does.Contain("45"));
        }

        [Test]
        public void IsSimulationActive_ReturnsCorrectState()
        {
            // Initially false
            Assert.IsFalse(_diagnostics.IsSimulationActive);
            
            // After enabling
            _diagnostics.SetSimulation(50, 1f, 10);
            Assert.IsTrue(_diagnostics.IsSimulationActive);
            
            // After disabling
            _diagnostics.DisableSimulation();
            Assert.IsFalse(_diagnostics.IsSimulationActive);
        }

        [Test]
        public void RTT_ReturnsBackendValue()
        {
            // Arrange
            _mockBackend.SetMockRTT(100f);
            
            // Act & Assert
            Assert.AreEqual(100f, _diagnostics.RTT);
        }

        [Test]
        public void PacketLoss_ReturnsBackendValue()
        {
            // Arrange
            _mockBackend.SetMockPacketLoss(2.5f);
            
            // Act & Assert
            Assert.AreEqual(2.5f, _diagnostics.PacketLoss);
        }
    }
}
