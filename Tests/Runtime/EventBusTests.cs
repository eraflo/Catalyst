using NUnit.Framework;
using Eraflo.Catalyst.Events;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eraflo.Catalyst.Tests
{
    public class EventBusTests
    {
        private IntEventChannel _testChannel;

        [SetUp]
        public void Setup()
        {
            _testChannel = ScriptableObject.CreateInstance<IntEventChannel>();
            EventBus.Clear(_testChannel);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear(_testChannel);
            if (_testChannel != null)
            {
                UnityEngine.Object.DestroyImmediate(_testChannel);
            }
        }

        [Test]
        public void Subscribe_AddsCallback()
        {
            int receivedValue = 0;
            Action<int> callback = (v) => receivedValue = v;

            _testChannel.Subscribe(callback);
            _testChannel.Raise(42);

            Assert.AreEqual(42, receivedValue);
        }

        [Test]
        public void Unsubscribe_RemovesCallback()
        {
            int callCount = 0;
            Action<int> callback = (v) => callCount++;

            _testChannel.Subscribe(callback);
            _testChannel.Unsubscribe(callback);
            _testChannel.Raise(42);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void MultipleSubscribers_AllReceiveEvent()
        {
            int count1 = 0, count2 = 0, count3 = 0;

            _testChannel.Subscribe((v) => count1++);
            _testChannel.Subscribe((v) => count2++);
            _testChannel.Subscribe((v) => count3++);
            
            _testChannel.Raise(1);

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
            Assert.AreEqual(1, count3);
        }

        [Test]
        public void SubscriberCount_TracksCorrectly()
        {
            Action<int> callback1 = (v) => { };
            Action<int> callback2 = (v) => { };

            Assert.AreEqual(0, _testChannel.SubscriberCount);

            _testChannel.Subscribe(callback1);
            Assert.AreEqual(1, _testChannel.SubscriberCount);

            _testChannel.Subscribe(callback2);
            Assert.AreEqual(2, _testChannel.SubscriberCount);

            _testChannel.Unsubscribe(callback1);
            Assert.AreEqual(1, _testChannel.SubscriberCount);
        }

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            int callCount = 0;
            _testChannel.Subscribe((v) => callCount++);
            _testChannel.Subscribe((v) => callCount++);

            EventBus.Clear(_testChannel);
            _testChannel.Raise(1);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void VoidCallback_Works()
        {
            int callCount = 0;
            Action callback = () => callCount++;

            _testChannel.Subscribe(callback);
            _testChannel.Raise(42);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void ExceptionInCallback_DoesNotStopOthers()
        {
            int count = 0;

            _testChannel.Subscribe((v) => throw new Exception("Test exception"));
            _testChannel.Subscribe((v) => count++);

            // Expect the exception to be logged
            LogAssert.ignoreFailingMessages = true;
            
            _testChannel.Raise(1);
            
            LogAssert.ignoreFailingMessages = false;

            // Second subscriber should still have been called
            Assert.AreEqual(1, count);
        }
    }
}

