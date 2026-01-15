using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Spawn;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class NetworkSpawnManagerTests
    {
        private NetworkManager _networkManager;
        private NetworkIdManager _idManager;
        private MockNetworkBackend _mockBackend;
        private NetworkSpawnManager _spawnManager;

        [SetUp]
        public void SetUp()
        {
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _networkManager = new NetworkManager();
            _idManager = new NetworkIdManager();
            _spawnManager = new NetworkSpawnManager();
            
            App.Register<NetworkManager>(_networkManager);
            App.Register<NetworkIdManager>(_idManager);
            App.Register<NetworkSpawnManager>(_spawnManager);
            
            _networkManager.SetBackend(_mockBackend);
            _idManager.Initialize();
            _spawnManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _spawnManager?.Shutdown();
            _idManager?.Shutdown();
            _networkManager?.Stop();
            App.Shutdown();
        }

        [Test]
        public void RegisterSpawnPoint_AddsToList()
        {
            // Arrange
            var go = new GameObject("SpawnPoint");
            var spawnPoint = go.AddComponent<NetworkSpawnPoint>();
            
            // Act
            _spawnManager.RegisterSpawnPoint(spawnPoint);
            
            // Assert
            Assert.IsTrue(_spawnManager.SpawnPoints.Contains(spawnPoint));
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void UnregisterSpawnPoint_RemovesFromList()
        {
            // Arrange
            var go = new GameObject("SpawnPoint");
            var spawnPoint = go.AddComponent<NetworkSpawnPoint>();
            _spawnManager.RegisterSpawnPoint(spawnPoint);
            
            // Act
            _spawnManager.UnregisterSpawnPoint(spawnPoint);
            
            // Assert
            Assert.IsFalse(_spawnManager.SpawnPoints.Contains(spawnPoint));
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetClientPayload_StoresPayload()
        {
            // Arrange
            var payload = new SpawnPayload
            {
                PrefabKey = "Warrior",
                TeamId = 1,
                SpawnTag = "TeamA"
            };
            
            // Act
            _spawnManager.SetClientPayload(42, payload);
            
            // Assert
            Assert.IsTrue(_spawnManager.TryGetClientPayload(42, out var retrieved));
            Assert.AreEqual("Warrior", retrieved.PrefabKey);
            Assert.AreEqual(1, retrieved.TeamId);
        }

        [Test]
        public void Strategy_CanBeChanged()
        {
            // Arrange
            var newStrategy = new RoundRobinSpawnStrategy();
            
            // Act
            _spawnManager.Strategy = newStrategy;
            
            // Assert
            Assert.AreEqual(newStrategy, _spawnManager.Strategy);
        }

        [Test]
        public void AutoSpawnEnabled_DefaultsToTrue()
        {
            Assert.IsTrue(_spawnManager.AutoSpawnEnabled);
        }

        [Test]
        public void DefaultPrefabKey_CanBeSet()
        {
            // Act
            _spawnManager.DefaultPrefabKey = "CustomPlayer";
            
            // Assert
            Assert.AreEqual("CustomPlayer", _spawnManager.DefaultPrefabKey);
        }

        [Test]
        public void OnBeforeSpawn_CanCancelSpawn()
        {
            // Arrange
            bool hookCalled = false;
            _spawnManager.OnBeforeSpawn += (clientId, payload) =>
            {
                hookCalled = true;
                return false; // Cancel spawn
            };
            
            var go = new GameObject("SpawnPoint");
            var spawnPoint = go.AddComponent<NetworkSpawnPoint>();
            _spawnManager.RegisterSpawnPoint(spawnPoint);
            
            // Act
            var result = _spawnManager.SpawnPlayerForClient(1);
            
            // Assert
            Assert.IsTrue(hookCalled);
            Assert.IsNull(result);
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SpawnPlayerForClient_WithNoSpawnPoints_ReturnsNull()
        {
            // Act
            var result = _spawnManager.SpawnPlayerForClient(1);
            
            // Assert
            Assert.IsNull(result);
        }
    }
}
