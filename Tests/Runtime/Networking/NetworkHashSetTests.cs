using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Collections;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkHashSetTests
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

        #region CRUD Operations

        [Test]
        public void Add_IncreasesCount()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            set.Add("Item1");
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void Add_ReturnsFalse_ForDuplicate()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            set.Add("Item1");
            bool added = set.Add("Item1");
            Assert.IsFalse(added);
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void Remove_DecreasesCount()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            set.Add("Item1");
            set.Remove("Item1");
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Clear_EmptiesSet()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            set.Add("A");
            set.Add("B");
            set.Clear();
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Contains_ReturnsTrue_WhenItemExists()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            set.Add("Exists");
            Assert.IsTrue(set.Contains("Exists"));
        }

        #endregion

        #region Events

        [Test]
        public void OnItemAdded_Fires_WhenItemAdded()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            string addedItem = null;
            set.OnItemAdded += item => addedItem = item;

            set.Add("NewItem");

            Assert.AreEqual("NewItem", addedItem);
        }

        [Test]
        public void OnItemRemoved_Fires_WhenItemRemoved()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            string removedItem = null;
            set.OnItemRemoved += item => removedItem = item;

            set.Add("Item");
            set.Remove("Item");

            Assert.AreEqual("Item", removedItem);
        }

        [Test]
        public void OnCleared_Fires_WhenCleared()
        {
            var set = new NetworkHashSet<string>("TestSet", 1);
            bool cleared = false;
            set.OnCleared += () => cleared = true;

            set.Add("A");
            set.Clear();

            Assert.IsTrue(cleared);
        }

        #endregion

        #region Authority

        [Test]
        public void Add_IsIgnored_WhenNoAuthority()
        {
            _mockBackend.SetServerState(false);
            _ownership.SetOwner(1, 999);

            var set = new NetworkHashSet<string>("TestSet", 1, AuthorityMode.ServerAuthoritative);
            set.Add("Item");

            Assert.AreEqual(0, set.Count);
        }

        #endregion
    }
}
