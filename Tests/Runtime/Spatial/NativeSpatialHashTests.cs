using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Eraflo.Catalyst.Spatial.Native;
using UnityEngine;

namespace Eraflo.Catalyst.Tests.Spatial
{
    public class NativeSpatialHashTests
    {
        private NativeSpatialHash _hash;
        
        [SetUp]
        public void SetUp()
        {
            _hash = new NativeSpatialHash(capacity: 100, cellSize: 10f, Allocator.TempJob);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_hash.IsCreated)
                _hash.Dispose();
        }
        
        [Test]
        public void Add_IncreasesCount()
        {
            _hash.Add(0, new float3(0, 0, 0));
            Assert.AreEqual(1, _hash.Count);
        }
        
        [Test]
        public void Remove_DecreasesCount()
        {
            _hash.Add(0, new float3(0, 0, 0));
            _hash.Remove(0);
            Assert.AreEqual(0, _hash.Count);
        }
        
        [Test]
        public void Update_ChangesPosition()
        {
            _hash.Add(0, new float3(0, 0, 0));
            _hash.Update(0, new float3(50, 50, 50));
            
            Assert.AreEqual(new float3(50, 50, 50), _hash.GetPosition(0));
        }
        
        [Test]
        public void QueryRadius_ReturnsItemsInRange()
        {
            _hash.Add(0, new float3(0, 0, 0));
            _hash.Add(1, new float3(5, 0, 0));
            _hash.Add(2, new float3(100, 0, 0));
            
            var results = new NativeList<int>(Allocator.Temp);
            _hash.QueryRadius(new float3(0, 0, 0), 10f, results);
            
            var array = results.AsArray().ToArray();
            Assert.Contains(0, array);
            Assert.Contains(1, array);
            
            results.Dispose();
        }
        
        [Test]
        public void Clear_RemovesAllItems()
        {
            _hash.Add(0, new float3(0, 0, 0));
            _hash.Add(1, new float3(5, 0, 0));
            
            _hash.Clear();
            
            Assert.AreEqual(0, _hash.Count);
            Assert.IsFalse(_hash.IsActive(0));
            Assert.IsFalse(_hash.IsActive(1));
        }
    }
}
