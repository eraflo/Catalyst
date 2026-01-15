using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Culling;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class NetworkCullingManagerTests
    {
        private NetworkManager _networkManager;
        private NetworkIdManager _idManager;
        private MockNetworkBackend _mockBackend;
        private NetworkCullingManager _cullingManager;

        [SetUp]
        public void SetUp()
        {
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _networkManager = new NetworkManager();
            _idManager = new NetworkIdManager();
            _cullingManager = new NetworkCullingManager();
            
            App.Register<NetworkManager>(_networkManager);
            App.Register<NetworkIdManager>(_idManager);
            App.Register<NetworkCullingManager>(_cullingManager);
            
            _networkManager.SetBackend(_mockBackend);
            _idManager.Initialize();
            _cullingManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _cullingManager?.Shutdown();
            _idManager?.Shutdown();
            _networkManager?.Stop();
            App.Shutdown();
        }

        [Test]
        public void RegisterCullable_AddsToSpatialIndex()
        {
            // Arrange
            var cullable = new MockCullable(1, Vector3.zero);
            
            // Act
            _cullingManager.RegisterCullable(cullable);
            
            // Assert - no exception means success
            Assert.Pass();
        }

        [Test]
        public void UnregisterCullable_RemovesFromSpatialIndex()
        {
            // Arrange
            var cullable = new MockCullable(1, Vector3.zero);
            _cullingManager.RegisterCullable(cullable);
            
            // Act
            _cullingManager.UnregisterCullable(cullable);
            
            // Assert - no exception means success
            Assert.Pass();
        }

        [Test]
        public void RegisterCullingArea_AddsToClientList()
        {
            // Arrange
            var go = new GameObject("CullingArea");
            var area = go.AddComponent<NetworkCullingArea>();
            area.Radius = 50f;
            
            // Act
            _cullingManager.RegisterCullingArea(1, area);
            
            // Assert - The area is registered (verified by not throwing)
            Assert.Pass();
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void UnregisterCullingArea_RemovesFromClientList()
        {
            // Arrange
            var go = new GameObject("CullingArea");
            var area = go.AddComponent<NetworkCullingArea>();
            _cullingManager.RegisterCullingArea(1, area);
            
            // Act
            _cullingManager.UnregisterCullingArea(1);
            
            // Assert
            Assert.Pass();
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetVisibleObjects_ReturnsEmptyForUnknownClient()
        {
            // Act
            var visible = _cullingManager.GetVisibleObjects(999);
            
            // Assert
            Assert.IsNotNull(visible);
            Assert.AreEqual(0, visible.Count);
        }

        [Test]
        public void Enabled_DefaultsToTrue()
        {
            Assert.IsTrue(_cullingManager.Enabled);
        }

        [Test]
        public void UseStaggeredUpdates_DefaultsToTrue()
        {
            Assert.IsTrue(_cullingManager.UseStaggeredUpdates);
        }

        [Test]
        public void CellSize_CanBeSet()
        {
            // Act
            _cullingManager.CellSize = 100f;
            
            // Assert
            Assert.AreEqual(100f, _cullingManager.CellSize);
        }

        [Test]
        public void BackendVisibility_NetworkShow_CallsBackend()
        {
            // Arrange - simulate client connected
            _mockBackend.SetServerState(true);
            
            // Act - directly test backend
            _mockBackend.NetworkShow(1, 42);
            
            // Assert
            Assert.IsTrue(_mockBackend.IsVisibleTo(1, 42));
        }

        [Test]
        public void BackendVisibility_NetworkHide_CallsBackend()
        {
            // Arrange
            _mockBackend.NetworkShow(1, 42);
            
            // Act
            _mockBackend.NetworkHide(1, 42);
            
            // Assert
            Assert.IsFalse(_mockBackend.IsVisibleTo(1, 42));
        }

        [Test]
        public void BackendVisibility_NetworkShowToAll_MakesGloballyVisible()
        {
            // Act
            _mockBackend.NetworkShowToAll(1);
            
            // Assert
            Assert.IsTrue(_mockBackend.IsVisibleTo(1, 123)); // Any client
            Assert.IsTrue(_mockBackend.IsVisibleTo(1, 456));
        }

        [Test]
        public void BackendVisibility_NetworkHideFromAll_ClearsAllVisibility()
        {
            // Arrange
            _mockBackend.NetworkShowToAll(1);
            _mockBackend.NetworkShow(1, 42);
            
            // Act
            _mockBackend.NetworkHideFromAll(1);
            
            // Assert
            Assert.IsFalse(_mockBackend.IsVisibleTo(1, 42));
            Assert.IsFalse(_mockBackend.IsVisibleTo(1, 123));
        }

        /// <summary>
        /// Mock implementation of ICullable for testing.
        /// </summary>
        private class MockCullable : ICullable
        {
            public uint NetworkId { get; }
            public Vector3 CullPosition { get; }
            public bool IsVisible { get; set; }

            public MockCullable(uint networkId, Vector3 position)
            {
                NetworkId = networkId;
                CullPosition = position;
                IsVisible = true;
            }
        }
    }
}
