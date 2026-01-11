using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Core.Chronos.Features;
using Eraflo.Catalyst.EasingSystem;
using Eraflo.Catalyst.Networking.Backends;

namespace Eraflo.Catalyst.Tests.Chronos
{
    public class ChronosNetworkTests
    {
        private ChronosManager _chronos;
        private NetworkManager _network;
        private ChronosNetworkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _chronos = new ChronosManager();
            App.Register(_chronos);
            ((IGameService)_chronos).Initialize();

            _network = new NetworkManager();
            App.Register(_network);
            _network.Backends.Register(new MockBackendFactory());
            _network.SetBackendById("mock");

            _handler = new ChronosNetworkHandler();
            _network.Handlers.Register(_handler);
        }

        [TearDown]
        public void TearDown()
        {
            App.Shutdown();
        }

        [Test]
        public void Server_Broadcasts_Transition()
        {
            // Simulate server
            var mock = (MockNetworkBackend)_network.Backend;
            mock.SetServerState(true);
            mock.SetConnectedState(true);

            _chronos.SetTimeScale("World", 0.5f, 1.0f, EasingType.Linear);

            // Verify message was sent
            var msgId = _network.Router.GetId<ChronosSyncMessage>();
            Assert.IsTrue(mock.SentMessages.Any(m => m.Type == msgId), "ChronosSyncMessage should be sent by server");
            
            var sentData = mock.SentMessages.First(m => m.Type == msgId).Data;
            var msg = NetworkSerializer.Deserialize<ChronosSyncMessage>(sentData);
            Assert.AreEqual("World", msg.ChannelId);
            Assert.AreEqual(0.5f, msg.TargetScale);
            Assert.AreEqual(1.0f, msg.Duration);
        }

        [UnityTest]
        public IEnumerator Client_Applies_Received_Transition()
        {
            // Simulate client
            var mock = (MockNetworkBackend)_network.Backend;
            mock.SetServerState(false);
            mock.SetClientState(true);
            mock.SetConnectedState(true);

            // Simulate receiving message
            var msg = new ChronosSyncMessage
            {
                ChannelId = "World",
                TargetScale = 0.2f,
                Duration = 0.1f,
                EaseType = EasingType.Linear
            };
            
            // Route message manually to handler
            var msgId = _network.Router.GetId<ChronosSyncMessage>();
            var data = NetworkSerializer.Serialize(msg);
            mock.TriggerReceive(msgId, data, 0);

            // Wait for transition to start and progress
            yield return new WaitForSeconds(0.05f);
            
            float scale = _chronos.GetChannelScale("World");
            Assert.Less(scale, 1.0f, "Scale should be decreasing");
            Assert.Greater(scale, 0.2f, "Scale should not have reached target yet");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(0.2f, _chronos.GetChannelScale("World"), 0.01f, "Scale should reach target");
        }
    }
}
