using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Collections;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkStackTests
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

        #region LIFO Operations

        [Test]
        public void Push_IncreasesCount()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            stack.Push(1);
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void Pop_ReturnsLastItem()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            stack.Push(10);
            stack.Push(20);
            var item = stack.Pop();
            Assert.AreEqual(20, item);
        }

        [Test]
        public void Pop_DecreasesCount()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            stack.Push(1);
            stack.Push(2);
            stack.Pop();
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void Peek_ReturnsTopItem_WithoutRemoving()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            stack.Push(42);
            var peeked = stack.Peek();
            Assert.AreEqual(42, peeked);
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void Clear_EmptiesStack()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            stack.Push(1);
            stack.Push(2);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
        }

        #endregion

        #region Events

        [Test]
        public void OnPushed_Fires_WhenItemPushed()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            int pushedItem = 0;
            stack.OnPushed += item => pushedItem = item;

            stack.Push(99);

            Assert.AreEqual(99, pushedItem);
        }

        [Test]
        public void OnPopped_Fires_WhenItemPopped()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            int poppedItem = 0;
            stack.OnPopped += item => poppedItem = item;

            stack.Push(55);
            stack.Pop();

            Assert.AreEqual(55, poppedItem);
        }

        [Test]
        public void OnCleared_Fires_WhenCleared()
        {
            var stack = new NetworkStack<int>("TestStack", 1);
            bool cleared = false;
            stack.OnCleared += () => cleared = true;

            stack.Push(1);
            stack.Clear();

            Assert.IsTrue(cleared);
        }

        #endregion

        #region Authority

        [Test]
        public void Push_IsIgnored_WhenNoAuthority()
        {
            _mockBackend.SetServerState(false);
            _ownership.SetOwner(1, 999);

            var stack = new NetworkStack<int>("TestStack", 1, AuthorityMode.ServerAuthoritative);
            stack.Push(1);

            Assert.AreEqual(0, stack.Count);
        }

        #endregion
    }
}
