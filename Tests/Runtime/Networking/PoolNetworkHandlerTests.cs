using System.Collections;
using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Pooling;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;

namespace Eraflo.Catalyst.Tests
{
    public class PoolNetworkHandlerTests
    {
        private NetworkManager _network;
        private Pool _pool;
        private PoolNetworkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _network = new NetworkManager();
            _pool = new Pool();
            App.Register(_network);
            App.Register(_pool);
            
            _network.Backends.Register(new MockBackendFactory());
            _network.SetBackendById("mock");
            
            _handler = new PoolNetworkHandler();
            _network.Handlers.Register(_handler);
            
            ((IGameService)_pool).Initialize();
            _pool.ClearAllPools();
            _handler.Clear();
            
            // Start as host for tests
            _network.StartHost(7777);
        }

        [TearDown]
        public void TearDown()
        {
            _network.Stop();
            _pool.ClearAllPools();
        }

        [Test]
        public void SpawnNetworked_Class_RegistersAndBroadcats()
        {
            var (handle, id) = _pool.GetFromPoolNetworked<TestNetworkPoolable>();
            
            // Check registration
            Assert.IsTrue(handle.IsValid);
            Assert.IsNotNull(handle.Instance);
            Assert.Greater(id, 0u);
            
            // Check if backend got a message
            var mock = (MockNetworkBackend)_network.Backend;
            Assert.IsTrue(mock.SentMessages.Any(m => m.Target == NetworkTarget.Clients), "Spawn message should be broadcast");
        }

        [UnityTest]
        public IEnumerator DespawnNetworked_Class_Unregisters()
        {
            var handle = _pool.GetFromPoolNetworked<TestNetworkPoolable>();
            var instance = handle.Instance;
            
            handle.DespawnNetworked();
            
            Assert.IsFalse(instance.WasSpawned); // Should have called OnNetworkDespawn
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnNetworked_GameObject_CallsBackendSync()
        {
            var prefab = new GameObject("NetPrefab");
            var handle = _pool.SpawnNetworked(prefab, Vector3.one);
            
            Assert.IsTrue(handle.IsValid);
            // Verify position
            Assert.AreEqual(Vector3.one, handle.Instance.transform.position);
            
            Object.DestroyImmediate(prefab);
            yield return null;
        }

        private class TestNetworkPoolable : IPoolable, INetworkPoolable
        {
            public bool WasSpawned { get; private set; }
            public void OnSpawn() { }
            public void OnDespawn() { }
            public void OnNetworkSpawn(byte[] data) => WasSpawned = true;
            public void OnNetworkDespawn() => WasSpawned = false;
        }
    }
}
