using NUnit.Framework;
using Eraflo.UnityImportPackage.Events;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Tests
{
    public class NetworkEventChannelTests
    {
        [Test]
        public void NetworkEventChannel_DefaultModeIsLocalOnly()
        {
            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            Assert.AreEqual(NetworkEventMode.LocalOnly, channel.NetworkMode);
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
        public void NetworkIntEventChannel_RaiseLocal_PassesValue()
        {
            var channel = ScriptableObject.CreateInstance<NetworkIntEventChannel>();
            int received = 0;

            channel.Subscribe((v) => received = v);
            channel.RaiseLocal(42);

            Assert.AreEqual(42, received);
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
        public void NetworkEventChannel_FallsBackToLocal_WhenNoHandler()
        {
            // Ensure no handler is registered
            NetworkEventManager.UnregisterHandler();

            var channel = ScriptableObject.CreateInstance<NetworkEventChannel>();
            channel.NetworkMode = NetworkEventMode.Broadcast;
            bool called = false;

            channel.Subscribe(() => called = true);
            channel.Raise(); // Should fallback to local since no network

            Assert.IsTrue(called);
            Object.DestroyImmediate(channel);
        }
    }
}
