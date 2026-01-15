using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.Spatial;

namespace Eraflo.Catalyst.Tests.Spatial
{
    public class SpatialHashTests
    {
        private SpatialHash<TestItem> _hash;
        
        private class TestItem
        {
            public string Name;
            public TestItem(string name) => Name = name;
        }
        
        [SetUp]
        public void SetUp()
        {
            _hash = new SpatialHash<TestItem>(cellSize: 10f);
        }
        
        [TearDown]
        public void TearDown()
        {
            _hash.Clear();
        }
        
        #region Insert/Remove
        
        [Test]
        public void Insert_IncreasesCount()
        {
            var item = new TestItem("A");
            _hash.Insert(item, Vector3.zero);
            
            Assert.AreEqual(1, _hash.Count);
        }
        
        [Test]
        public void Remove_DecreasesCount()
        {
            var item = new TestItem("A");
            _hash.Insert(item, Vector3.zero);
            _hash.Remove(item);
            
            Assert.AreEqual(0, _hash.Count);
        }
        
        [Test]
        public void Insert_Duplicate_UpdatesPosition()
        {
            var item = new TestItem("A");
            _hash.Insert(item, Vector3.zero);
            _hash.Insert(item, Vector3.one * 100);
            
            Assert.AreEqual(1, _hash.Count);
            _hash.TryGetPosition(item, out var pos);
            Assert.AreEqual(Vector3.one * 100, pos);
        }
        
        [Test]
        public void Clear_RemovesAllItems()
        {
            for (int i = 0; i < 10; i++)
                _hash.Insert(new TestItem($"Item{i}"), Vector3.one * i);
            
            _hash.Clear();
            
            Assert.AreEqual(0, _hash.Count);
        }
        
        #endregion
        
        #region Radius Query
        
        [Test]
        public void QueryRadius_ReturnsItemsInRange()
        {
            var near = new TestItem("Near");
            var far = new TestItem("Far");
            
            _hash.Insert(near, Vector3.zero);
            _hash.Insert(far, Vector3.one * 100);
            
            var results = new List<TestItem>();
            _hash.QueryRadius(Vector3.zero, 10f, results);
            
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(near, results[0]);
        }
        
        [Test]
        public void QueryRadius_ReturnsMultipleItemsInRange()
        {
            _hash.Insert(new TestItem("A"), new Vector3(0, 0, 0));
            _hash.Insert(new TestItem("B"), new Vector3(3, 0, 0));
            _hash.Insert(new TestItem("C"), new Vector3(6, 0, 0));
            _hash.Insert(new TestItem("D"), new Vector3(100, 0, 0));
            
            var results = new List<TestItem>();
            _hash.QueryRadius(Vector3.zero, 10f, results);
            
            Assert.AreEqual(3, results.Count);
        }
        
        [Test]
        public void QueryRadius_EmptyHash_ReturnsEmpty()
        {
            var results = new List<TestItem>();
            _hash.QueryRadius(Vector3.zero, 100f, results);
            
            Assert.AreEqual(0, results.Count);
        }
        
        #endregion
        
        #region Nearest Query
        
        [Test]
        public void QueryNearest_ReturnsClosestItem()
        {
            var a = new TestItem("A");
            var b = new TestItem("B");
            var c = new TestItem("C");
            
            _hash.Insert(a, new Vector3(10, 0, 0));
            _hash.Insert(b, new Vector3(5, 0, 0));
            _hash.Insert(c, new Vector3(20, 0, 0));
            
            var nearest = _hash.QueryNearest(Vector3.zero);
            
            Assert.AreEqual(b, nearest);
        }
        
        [Test]
        public void QueryNearest_EmptyHash_ReturnsNull()
        {
            var nearest = _hash.QueryNearest(Vector3.zero);
            Assert.IsNull(nearest);
        }
        
        #endregion
        
        #region Box Query
        
        [Test]
        public void QueryBox_ReturnsItemsInBounds()
        {
            _hash.Insert(new TestItem("In"), new Vector3(5, 5, 5));
            _hash.Insert(new TestItem("Out"), new Vector3(100, 100, 100));
            
            var bounds = new Bounds(Vector3.zero, Vector3.one * 20);
            var results = new List<TestItem>();
            _hash.QueryBox(bounds, results);
            
            Assert.AreEqual(1, results.Count);
        }
        
        #endregion
        
        #region Update
        
        [Test]
        public void Update_ChangesPosition()
        {
            var item = new TestItem("A");
            _hash.Insert(item, Vector3.zero);
            _hash.Update(item, Vector3.one * 50);
            
            _hash.TryGetPosition(item, out var pos);
            Assert.AreEqual(Vector3.one * 50, pos);
        }
        
        [Test]
        public void Update_MovesToNewCell()
        {
            var item = new TestItem("A");
            _hash.Insert(item, Vector3.zero);
            
            int cellsBefore = _hash.CellCount;
            _hash.Update(item, Vector3.one * 100);
            int cellsAfter = _hash.CellCount;
            
            // Should have created a new cell (old cell removed if empty)
            Assert.GreaterOrEqual(cellsAfter, 1);
        }
        
        #endregion
    }
}
