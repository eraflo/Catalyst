using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.UnityImportPackage.Pooling;

namespace Eraflo.UnityImportPackage.Tests
{
    public class PoolTests
    {
        [SetUp]
        public void SetUp()
        {
            Pool.ClearAll();
            Pool.Metrics.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Pool.ClearAll();
        }

        #region Generic Pool Tests

        [Test]
        public void Get_ReturnsValidHandle()
        {
            var handle = Pool.Get<TestPoolable>();

            Assert.IsTrue(handle.IsValid);
            Assert.IsNotNull(handle.Instance);
        }

        [Test]
        public void Get_CallsOnSpawn()
        {
            var handle = Pool.Get<TestPoolable>();

            Assert.IsTrue(handle.Instance.WasSpawned);
        }

        [Test]
        public void Release_CallsOnDespawn()
        {
            var handle = Pool.Get<TestPoolable>();
            var instance = handle.Instance;

            Pool.Release(handle);

            Assert.IsTrue(instance.WasDespawned);
        }

        [Test]
        public void Get_ReusesPreviouslyReleasedObject()
        {
            var handle1 = Pool.Get<TestPoolable>();
            var instance1 = handle1.Instance;
            Pool.Release(handle1);

            var handle2 = Pool.Get<TestPoolable>();
            var instance2 = handle2.Instance;

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Warmup_PreAllocatesObjects()
        {
            Pool.Warmup<TestPoolable>(10);

            // Get should reuse warmed up objects
            var handles = new PoolHandle<TestPoolable>[10];
            for (int i = 0; i < 10; i++)
            {
                handles[i] = Pool.Get<TestPoolable>();
            }

            // All should be valid
            foreach (var h in handles)
            {
                Assert.IsTrue(h.IsValid);
            }
        }

        [Test]
        public void Clear_RemovesAllObjects()
        {
            Pool.Warmup<TestPoolable>(5);
            
            Pool.Clear<TestPoolable>();

            // No exception means success
            Assert.Pass();
        }

        #endregion

        #region Handle Tests

        [Test]
        public void PoolHandle_None_IsNotValid()
        {
            var handle = PoolHandle<TestPoolable>.None;

            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        public void PoolHandle_Equality_Works()
        {
            var handle1 = Pool.Get<TestPoolable>();
            var handle2 = handle1;

            Assert.AreEqual(handle1, handle2);
            Assert.IsTrue(handle1 == handle2);
        }

        [Test]
        public void PoolHandle_DifferentHandles_NotEqual()
        {
            var handle1 = Pool.Get<TestPoolable>();
            var handle2 = Pool.Get<TestPoolable>();

            Assert.AreNotEqual(handle1, handle2);
            Assert.IsTrue(handle1 != handle2);
        }

        #endregion

        #region Metrics Tests

        [Test]
        public void Metrics_TotalSpawned_IncrementsOnGet()
        {
            Pool.Get<TestPoolable>();
            Pool.Get<TestPoolable>();
            Pool.Get<TestPoolable>();

            Assert.AreEqual(3, Pool.Metrics.TotalSpawned);
        }

        [Test]
        public void Metrics_TotalDespawned_IncrementsOnRelease()
        {
            var h1 = Pool.Get<TestPoolable>();
            var h2 = Pool.Get<TestPoolable>();
            Pool.Release(h1);

            Assert.AreEqual(1, Pool.Metrics.TotalDespawned);
        }

        [Test]
        public void Metrics_ActiveCount_TracksCorrectly()
        {
            var h1 = Pool.Get<TestPoolable>();
            var h2 = Pool.Get<TestPoolable>();
            
            Assert.AreEqual(2, Pool.Metrics.ActiveCount);
            
            Pool.Release(h1);
            
            Assert.AreEqual(1, Pool.Metrics.ActiveCount);
        }

        [Test]
        public void Metrics_PeakActiveCount_TracksMax()
        {
            var h1 = Pool.Get<TestPoolable>();
            var h2 = Pool.Get<TestPoolable>();
            var h3 = Pool.Get<TestPoolable>();
            Pool.Release(h1);
            Pool.Release(h2);

            Assert.AreEqual(3, Pool.Metrics.PeakActiveCount);
        }

        #endregion

        #region Prefab Pool Tests

        [UnityTest]
        public IEnumerator Spawn_WithPrefab_CreatesGameObject()
        {
            var prefab = new GameObject("TestPrefab");
            
            var handle = Pool.Spawn(prefab, Vector3.zero);
            
            Assert.IsTrue(handle.IsValid);
            Assert.IsNotNull(handle.Instance);
            Assert.IsTrue(handle.Instance.activeSelf);
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_AtPosition_SetsCorrectPosition()
        {
            var prefab = new GameObject("TestPrefab");
            var pos = new Vector3(10, 20, 30);
            
            var handle = Pool.Spawn(prefab, pos);
            
            Assert.AreEqual(pos, handle.Instance.transform.position);
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Despawn_DeactivatesGameObject()
        {
            var prefab = new GameObject("TestPrefab");
            var handle = Pool.Spawn(prefab, Vector3.zero);
            var go = handle.Instance;
            
            Pool.Despawn(handle);
            
            Assert.IsFalse(go.activeSelf);
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_ReusesDesspawnedObject()
        {
            var prefab = new GameObject("TestPrefab");
            
            var handle1 = Pool.Spawn(prefab, Vector3.zero);
            var go1 = handle1.Instance;
            Pool.Despawn(handle1);
            
            var handle2 = Pool.Spawn(prefab, Vector3.zero);
            var go2 = handle2.Instance;
            
            Assert.AreSame(go1, go2);
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_AddsPooledObjectComponent()
        {
            var prefab = new GameObject("TestPrefab");
            
            var handle = Pool.Spawn(prefab, Vector3.zero);
            
            Assert.IsNotNull(handle.Instance.GetComponent<PooledObject>());
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator PooledObject_IsSpawned_ReflectsState()
        {
            var prefab = new GameObject("TestPrefab");
            
            var handle = Pool.Spawn(prefab, Vector3.zero);
            var pooledObj = handle.Instance.GetComponent<PooledObject>();
            
            Assert.IsTrue(pooledObj.IsSpawned);
            
            Pool.Despawn(handle);
            
            Assert.IsFalse(pooledObj.IsSpawned);
            
            // Cleanup
            Pool.ClearAll();
            Object.DestroyImmediate(prefab);
            
            yield return null;
        }

        #endregion

        #region Test Helpers

        private class TestPoolable : IPoolable
        {
            public bool WasSpawned { get; private set; }
            public bool WasDespawned { get; private set; }

            public void OnSpawn()
            {
                WasSpawned = true;
                WasDespawned = false;
            }

            public void OnDespawn()
            {
                WasDespawned = true;
            }
        }

        #endregion
    }
}
