using NUnit.Framework;
using Eraflo.Catalyst.Events;
using UnityEngine;

namespace Eraflo.Catalyst.Tests
{
    public class EventChannelTests
    {
        [Test]
        public void EventChannel_CanBeCreated()
        {
            var channel = ScriptableObject.CreateInstance<EventChannel>();
            Assert.IsNotNull(channel);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void IntEventChannel_RaisesWithValue()
        {
            var channel = ScriptableObject.CreateInstance<IntEventChannel>();
            int received = 0;

            channel.Subscribe((v) => received = v);
            channel.Raise(100);

            Assert.AreEqual(100, received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void FloatEventChannel_RaisesWithValue()
        {
            var channel = ScriptableObject.CreateInstance<FloatEventChannel>();
            float received = 0f;

            channel.Subscribe((v) => received = v);
            channel.Raise(3.14f);

            Assert.AreEqual(3.14f, received, 0.001f);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void StringEventChannel_RaisesWithValue()
        {
            var channel = ScriptableObject.CreateInstance<StringEventChannel>();
            string received = null;

            channel.Subscribe((v) => received = v);
            channel.Raise("Hello World");

            Assert.AreEqual("Hello World", received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void BoolEventChannel_RaisesWithValue()
        {
            var channel = ScriptableObject.CreateInstance<BoolEventChannel>();
            bool received = false;

            channel.Subscribe((v) => received = v);
            channel.Raise(true);

            Assert.IsTrue(received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Vector3EventChannel_RaisesWithValue()
        {
            var channel = ScriptableObject.CreateInstance<Vector3EventChannel>();
            Vector3 received = Vector3.zero;

            channel.Subscribe((v) => received = v);
            channel.Raise(new Vector3(1, 2, 3));

            Assert.AreEqual(new Vector3(1, 2, 3), received);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void VoidEventChannel_Raises()
        {
            var channel = ScriptableObject.CreateInstance<EventChannel>();
            bool called = false;

            channel.Subscribe(() => called = true);
            channel.Raise();

            Assert.IsTrue(called);
            Object.DestroyImmediate(channel);
        }
    }
}
