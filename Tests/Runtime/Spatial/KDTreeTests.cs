using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.Spatial;

namespace Eraflo.Catalyst.Tests.Spatial
{
    public class KDTreeTests
    {
        private KDTree<TestItem> _tree;
        
        private class TestItem
        {
            public string Name;
            public TestItem(string name) => Name = name;
        }
        
        [SetUp]
        public void SetUp()
        {
            _tree = new KDTree<TestItem>();
        }
        
        [TearDown]
        public void TearDown()
        {
            _tree.Clear();
        }
        
        #region Insert/Remove
        
        [Test]
        public void Insert_IncreasesCount()
        {
            _tree.Insert(new TestItem("A"), Vector3.zero);
            Assert.AreEqual(1, _tree.Count);
        }
        
        [Test]
        public void Remove_DecreasesCount()
        {
            var item = new TestItem("A");
            _tree.Insert(item, Vector3.zero);
            _tree.Remove(item);
            
            Assert.AreEqual(0, _tree.Count);
        }
        
        [Test]
        public void Clear_RemovesAllItems()
        {
            for (int i = 0; i < 10; i++)
                _tree.Insert(new TestItem($"Item{i}"), Vector3.one * i);
            
            _tree.Clear();
            Assert.AreEqual(0, _tree.Count);
        }
        
        #endregion
        
        #region QueryNearest
        
        [Test]
        public void QueryNearest_ReturnsClosestItem()
        {
            var a = new TestItem("A");
            var b = new TestItem("B");
            var c = new TestItem("C");
            
            _tree.Insert(a, new Vector3(10, 0, 0));
            _tree.Insert(b, new Vector3(2, 0, 0));
            _tree.Insert(c, new Vector3(20, 0, 0));
            
            var nearest = _tree.QueryNearest(Vector3.zero);
            Assert.AreEqual(b, nearest);
        }
        
        [Test]
        public void QueryNearest_EmptyTree_ReturnsNull()
        {
            var nearest = _tree.QueryNearest(Vector3.zero);
            Assert.IsNull(nearest);
        }
        
        [Test]
        public void QueryNearest_SingleItem_ReturnsThatItem()
        {
            var item = new TestItem("Only");
            _tree.Insert(item, new Vector3(100, 100, 100));
            
            var nearest = _tree.QueryNearest(Vector3.zero);
            Assert.AreEqual(item, nearest);
        }
        
        #endregion
        
        #region QueryNearestN
        
        [Test]
        public void QueryNearestN_ReturnsNClosestItems()
        {
            for (int i = 0; i < 10; i++)
                _tree.Insert(new TestItem($"Item{i}"), new Vector3(i * 10, 0, 0));
            
            var nearest = new List<TestItem>(_tree.QueryNearestN(Vector3.zero, 3));
            
            Assert.AreEqual(3, nearest.Count);
        }
        
        [Test]
        public void QueryNearestN_LessThanN_ReturnsAll()
        {
            _tree.Insert(new TestItem("A"), Vector3.zero);
            _tree.Insert(new TestItem("B"), Vector3.one);
            
            var nearest = new List<TestItem>(_tree.QueryNearestN(Vector3.zero, 10));
            
            Assert.AreEqual(2, nearest.Count);
        }
        
        #endregion
        
        #region QueryRadius
        
        [Test]
        public void QueryRadius_ReturnsItemsInRange()
        {
            _tree.Insert(new TestItem("Near"), new Vector3(5, 0, 0));
            _tree.Insert(new TestItem("Far"), new Vector3(100, 0, 0));
            
            var results = new List<TestItem>();
            _tree.QueryRadius(Vector3.zero, 10f, results);
            
            Assert.AreEqual(1, results.Count);
        }
        
        #endregion
        
        #region QueryBox
        
        [Test]
        public void QueryBox_ReturnsItemsInBounds()
        {
            _tree.Insert(new TestItem("In"), new Vector3(5, 5, 5));
            _tree.Insert(new TestItem("Out"), new Vector3(100, 100, 100));
            
            var bounds = new Bounds(Vector3.zero, Vector3.one * 20);
            var results = new List<TestItem>();
            _tree.QueryBox(bounds, results);
            
            Assert.AreEqual(1, results.Count);
        }
        
        #endregion
        
        #region BuildBalanced
        
        [Test]
        public void BuildBalanced_CreatesBalancedTree()
        {
            var items = new List<(TestItem, Vector3)>();
            for (int i = 0; i < 100; i++)
            {
                items.Add((new TestItem($"Item{i}"), new Vector3(
                    Random.Range(-100f, 100f),
                    Random.Range(-100f, 100f),
                    Random.Range(-100f, 100f)
                )));
            }
            
            _tree.BuildBalanced(items);
            
            Assert.AreEqual(100, _tree.Count);
            
            // Balanced tree should have depth <= log2(n) + 1
            int depth = _tree.GetDepth();
            int maxExpectedDepth = Mathf.CeilToInt(Mathf.Log(100, 2)) + 2;
            Assert.LessOrEqual(depth, maxExpectedDepth);
        }
        
        #endregion
        
        #region Update
        
        [Test]
        public void Update_ChangesPosition()
        {
            var item = new TestItem("A");
            _tree.Insert(item, Vector3.zero);
            _tree.Update(item, Vector3.one * 50);
            
            _tree.TryGetPosition(item, out var pos);
            Assert.AreEqual(Vector3.one * 50, pos);
        }
        
        #endregion
    }
}
