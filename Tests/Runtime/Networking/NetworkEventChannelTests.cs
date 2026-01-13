using NUnit.Framework;
using Eraflo.Catalyst.Events;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;
using Eraflo.Catalyst.Networking.Backends.Mock;
using UnityEngine;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkEventChannelTests
    {
        [SetUp]
        public void SetUp()
        {
            App.Get<NetworkManager>().Reset();
            var mock = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            mock.EnableLoopback = true;
            App.Get<NetworkManager>().SetBackend(mock);
            App.Get<NetworkManager>().Handlers.Register(new EventNetworkHandler());
        }

        [TearDown]
        public void TearDown()
        {
            App.Get<NetworkManager>().Reset();
        }

        [Test]
        public void NetworkEventChannel_EnableNetwork_DefaultsFalse()
        {
            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            Assert.IsFalse(channel.EnableNetwork);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void NetworkEventChannel_RaiseLocal_DoesNotUseNetwork()
        {
            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            bool called = false;

            channel.Subscribe(() => called = true);
            channel.RaiseLocal();

            Assert.IsTrue(called);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void NetworkEventChannel_ChannelId_DefaultsToName()
        {
            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            channel.name = "TestChannel";

            Assert.AreEqual("TestChannel", channel.ChannelId);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void NetworkEventChannel_FallsBackToLocal_WhenNoNetwork()
        {
            // Clear backend to simulate no network
            App.Get<NetworkManager>().SetBackend(null);

            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            channel.EnableNetwork = true;
            bool called = false;

            channel.Subscribe(() => called = true);
            channel.Raise(); // Should fallback to local

            Assert.IsTrue(called);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void NetworkEventChannel_Raise_WithTarget()
        {
            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            channel.EnableNetwork = true;
            bool called = false;

            channel.Subscribe(() => called = true);
            channel.Raise(NetworkTarget.All);

            Assert.IsTrue(called);
            Object.DestroyImmediate(channel);
        }
    }
}
