using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Collections;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkQueueTests
    {
        private NetworkManager _network;
        private NetworkOwnershipManager _ownership;
        private MockNetworkBackend _mockBackend;

        [SetUp]
        public void SetUp()
        {
            _network = new NetworkManager();
            App.Register(_network);

            _ownership = new NetworkOwnershipManager();
            App.Register(_ownership);

            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _network.SetBackend(_mockBackend);
            _network.Handlers.Register(_ownership);
        }

        [TearDown]
        public void TearDown()
        {
            _network.Stop();
            ((IGameService)_ownership).Shutdown();
            App.Shutdown();
        }

        #region FIFO Operations

        [Test]
        public void Enqueue_IncreasesCount()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            queue.Enqueue(1);
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Dequeue_ReturnsFirstItem()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            queue.Enqueue(10);
            queue.Enqueue(20);
            var item = queue.Dequeue();
            Assert.AreEqual(10, item);
        }

        [Test]
        public void Dequeue_DecreasesCount()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Dequeue();
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Peek_ReturnsFirstItem_WithoutRemoving()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            queue.Enqueue(42);
            var peeked = queue.Peek();
            Assert.AreEqual(42, peeked);
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Clear_EmptiesQueue()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Clear();
            Assert.AreEqual(0, queue.Count);
        }

        #endregion

        #region Events

        [Test]
        public void OnEnqueued_Fires_WhenItemEnqueued()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            int enqueuedItem = 0;
            queue.OnEnqueued += item => enqueuedItem = item;

            queue.Enqueue(99);

            Assert.AreEqual(99, enqueuedItem);
        }

        [Test]
        public void OnDequeued_Fires_WhenItemDequeued()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            int dequeuedItem = 0;
            queue.OnDequeued += item => dequeuedItem = item;

            queue.Enqueue(55);
            queue.Dequeue();

            Assert.AreEqual(55, dequeuedItem);
        }

        [Test]
        public void OnCleared_Fires_WhenCleared()
        {
            var queue = new NetworkQueue<int>("TestQueue", 1);
            bool cleared = false;
            queue.OnCleared += () => cleared = true;

            queue.Enqueue(1);
            queue.Clear();

            Assert.IsTrue(cleared);
        }

        #endregion

        #region Authority

        [Test]
        public void Enqueue_IsIgnored_WhenNoAuthority()
        {
            _mockBackend.SetServerState(false);
            _ownership.SetOwner(1, 999);

            var queue = new NetworkQueue<int>("TestQueue", 1, AuthorityMode.ServerAuthoritative);
            queue.Enqueue(1);

            Assert.AreEqual(0, queue.Count);
        }

        #endregion
    }
}
