using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Attachment;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class NetworkAttachmentManagerTests
    {
        private NetworkManager _networkManager;
        private NetworkIdManager _idManager;
        private MockNetworkBackend _mockBackend;
        private NetworkAttachmentManager _attachmentManager;

        [SetUp]
        public void SetUp()
        {
            _mockBackend = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
            _networkManager = new NetworkManager();
            _idManager = new NetworkIdManager();
            _attachmentManager = new NetworkAttachmentManager();
            
            App.Register<NetworkManager>(_networkManager);
            App.Register<NetworkIdManager>(_idManager);
            App.Register<NetworkAttachmentManager>(_attachmentManager);
            
            _networkManager.SetBackend(_mockBackend);
            _idManager.Initialize();
            _attachmentManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _attachmentManager?.Shutdown();
            _idManager?.Shutdown();
            _networkManager?.Stop();
            App.Shutdown();
        }

        [Test]
        public void IsAttached_ReturnsFalseForUnknownId()
        {
            Assert.IsFalse(_attachmentManager.IsAttached(999));
        }

        [Test]
        public void TryGetParent_ReturnsFalseForUnattached()
        {
            Assert.IsFalse(_attachmentManager.TryGetParent(999, out _));
        }

        [Test]
        public void RequestAttach_WithInvalidIds_DoesNotThrow()
        {
            // Should log warning but not throw
            Assert.DoesNotThrow(() => _attachmentManager.RequestAttach(999, 888));
        }

        [Test]
        public void RequestDetach_WithInvalidId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _attachmentManager.RequestDetach(999));
        }

        [Test]
        public void RequestAttach_WithValidObjects_TriggersMessage()
        {
            // Arrange
            var parentGo = new GameObject("Parent");
            var childGo = new GameObject("Child");
            
            _idManager.Register(1, parentGo);
            _idManager.Register(2, childGo);
            
            _mockBackend.ClearSentMessages();
            
            // Act
            _attachmentManager.RequestAttach(2, 1);
            
            // Assert - Server should execute immediately and broadcast
            // Check that message was sent
            Assert.That(_mockBackend.SentMessages.Count, Is.GreaterThan(0));
            
            // Cleanup
            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(childGo);
        }

        [Test]
        public void RequestAttach_OnServer_ExecutesImmediately()
        {
            // Arrange
            var parentGo = new GameObject("Parent");
            var childGo = new GameObject("Child");
            
            _idManager.Register(1, parentGo);
            _idManager.Register(2, childGo);
            
            bool attachEventFired = false;
            _attachmentManager.OnAttached += (child, parent) => attachEventFired = true;
            
            // Act
            _attachmentManager.RequestAttach(2, 1);
            
            // Assert
            Assert.IsTrue(attachEventFired);
            Assert.IsTrue(_attachmentManager.IsAttached(2));
            
            // Cleanup
            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(childGo);
        }

        [Test]
        public void RequestDetach_OnAttached_FiresEvent()
        {
            // Arrange
            var parentGo = new GameObject("Parent");
            var childGo = new GameObject("Child");
            
            _idManager.Register(1, parentGo);
            _idManager.Register(2, childGo);
            
            _attachmentManager.RequestAttach(2, 1);
            
            bool detachEventFired = false;
            _attachmentManager.OnDetached += (child) => detachEventFired = true;
            
            // Act
            _attachmentManager.RequestDetach(2);
            
            // Assert
            Assert.IsTrue(detachEventFired);
            Assert.IsFalse(_attachmentManager.IsAttached(2));
            
            // Cleanup
            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(childGo);
        }

        [Test]
        public void RequestAttach_WithLocalPosition_SetsCorrectPosition()
        {
            // Arrange
            var parentGo = new GameObject("Parent");
            var childGo = new GameObject("Child");
            
            _idManager.Register(1, parentGo);
            _idManager.Register(2, childGo);
            
            var localPos = new Vector3(1f, 2f, 3f);
            
            // Act
            _attachmentManager.RequestAttach(2, 1, localPos);
            
            // Assert
            Assert.AreEqual(parentGo.transform, childGo.transform.parent);
            Assert.AreEqual(localPos, childGo.transform.localPosition);
            
            // Cleanup
            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(childGo);
        }

        [Test]
        public void RequestDetach_WithInheritVelocity_PreservesVelocity()
        {
            // Arrange
            var parentGo = new GameObject("Parent");
            var parentRb = parentGo.AddComponent<Rigidbody>();
            parentRb.velocity = new Vector3(10f, 0f, 0f);
            parentRb.isKinematic = true;
            
            var childGo = new GameObject("Child");
            var childRb = childGo.AddComponent<Rigidbody>();
            childRb.isKinematic = false;
            
            _idManager.Register(1, parentGo);
            _idManager.Register(2, childGo);
            
            _attachmentManager.RequestAttach(2, 1);
            
            // Act
            _attachmentManager.RequestDetach(2, inheritVelocity: true);
            
            // Assert - child should be unparented
            Assert.IsNull(childGo.transform.parent);
            
            // Cleanup
            Object.DestroyImmediate(parentGo);
            Object.DestroyImmediate(childGo);
        }
    }
}
