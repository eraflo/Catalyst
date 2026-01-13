using System.Collections.Generic;
using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Collections;

namespace Eraflo.Catalyst.Tests
{
    public class NetworkDictionaryTests
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
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict.Add("Health", 100);
            Assert.AreEqual(1, dict.Count);
        }

        [Test]
        public void Indexer_Set_UpdatesValue()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict["Score"] = 50;
            Assert.AreEqual(50, dict["Score"]);
        }

        [Test]
        public void Remove_DecreasesCount()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict.Add("Key", 1);
            dict.Remove("Key");
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void Clear_EmptiesDictionary()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict.Add("A", 1);
            dict.Add("B", 2);
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void ContainsKey_ReturnsTrue_WhenKeyExists()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict.Add("Exists", 1);
            Assert.IsTrue(dict.ContainsKey("Exists"));
        }

        [Test]
        public void TryGetValue_ReturnsValue_WhenKeyExists()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            dict.Add("Key", 42);
            Assert.IsTrue(dict.TryGetValue("Key", out int value));
            Assert.AreEqual(42, value);
        }

        #endregion

        #region Events

        [Test]
        public void OnItemAdded_Fires_WhenItemAdded()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            string addedKey = null;
            dict.OnItemAdded += (k, v) => addedKey = k;

            dict.Add("NewKey", 1);

            Assert.AreEqual("NewKey", addedKey);
        }

        [Test]
        public void OnItemRemoved_Fires_WhenItemRemoved()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            string removedKey = null;
            dict.OnItemRemoved += (k, v) => removedKey = k;

            dict.Add("Key", 1);
            dict.Remove("Key");

            Assert.AreEqual("Key", removedKey);
        }

        [Test]
        public void OnItemSet_Fires_WhenValueUpdated()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            int oldValue = 0, newValue = 0;
            dict.OnItemSet += (k, o, n) => { oldValue = o; newValue = n; };

            dict.Add("Key", 10);
            dict["Key"] = 20;

            Assert.AreEqual(10, oldValue);
            Assert.AreEqual(20, newValue);
        }

        [Test]
        public void OnCleared_Fires_WhenCleared()
        {
            var dict = new NetworkDictionary<string, int>("TestDict", 1);
            bool cleared = false;
            dict.OnCleared += () => cleared = true;

            dict.Add("A", 1);
            dict.Clear();

            Assert.IsTrue(cleared);
        }

        #endregion

        #region Authority

        [Test]
        public void Add_IsIgnored_WhenNoAuthority()
        {
            _mockBackend.SetServerState(false);
            _ownership.SetOwner(1, 999); // Someone else owns it

            var dict = new NetworkDictionary<string, int>("TestDict", 1, AuthorityMode.ServerAuthoritative);
            dict.Add("Key", 1);

            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void IsReadOnly_ReturnsTrue_WhenNoAuthority()
        {
            _mockBackend.SetServerState(false);
            _ownership.SetOwner(1, 999);

            var dict = new NetworkDictionary<string, int>("TestDict", 1, AuthorityMode.ServerAuthoritative);

            Assert.IsTrue(dict.IsReadOnly);
        }

        #endregion
    }
}
