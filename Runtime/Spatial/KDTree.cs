using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Spatial
{
    /// <summary>
    /// K-dimensional tree for efficient nearest neighbor and range queries.
    /// Optimized for 3D space with O(log n) nearest neighbor queries.
    /// </summary>
    /// <typeparam name="T">Type of items stored.</typeparam>
    public class KDTree<T> : ISpatialIndex<T> where T : class
    {
        private KDNode _root;
        private int _count;
        
        // Item to node mapping for O(1) removal
        private readonly Dictionary<T, KDNode> _nodeMap = new();
        
        // Node pool for reduced allocations
        private readonly Stack<KDNode> _nodePool = new();
        
        // Reusable list for queries
        private readonly List<T> _queryBuffer = new();
        
        // Priority queue for N-nearest queries
        private readonly List<(T item, float distSq)> _nearestBuffer = new();
        
        public int Count => _count;
        
        private class KDNode
        {
            public T Item;
            public Vector3 Position;
            public KDNode Left;
            public KDNode Right;
            public KDNode Parent;
            public int SplitAxis; // 0=X, 1=Y, 2=Z
            public bool IsDeleted; // Lazy deletion flag
            
            public void Reset()
            {
                Item = null;
                Left = Right = Parent = null;
                IsDeleted = false;
            }
        }
        
        #region Core Operations
        
        public void Insert(T item, Vector3 position)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            
            if (_nodeMap.ContainsKey(item))
            {
                Update(item, position);
                return;
            }
            
            var node = RentNode();
            node.Item = item;
            node.Position = position;
            node.IsDeleted = false;
            
            if (_root == null)
            {
                node.SplitAxis = 0;
                _root = node;
            }
            else
            {
                InsertNode(_root, node, 0);
            }
            
            _nodeMap[item] = node;
            _count++;
        }
        
        private void InsertNode(KDNode parent, KDNode newNode, int depth)
        {
            int axis = depth % 3;
            float parentValue = GetAxisValue(parent.Position, axis);
            float newValue = GetAxisValue(newNode.Position, axis);
            
            if (newValue < parentValue)
            {
                if (parent.Left == null)
                {
                    parent.Left = newNode;
                    newNode.Parent = parent;
                    newNode.SplitAxis = (depth + 1) % 3;
                }
                else
                {
                    InsertNode(parent.Left, newNode, depth + 1);
                }
            }
            else
            {
                if (parent.Right == null)
                {
                    parent.Right = newNode;
                    newNode.Parent = parent;
                    newNode.SplitAxis = (depth + 1) % 3;
                }
                else
                {
                    InsertNode(parent.Right, newNode, depth + 1);
                }
            }
        }
        
        public bool Remove(T item)
        {
            if (item == null || !_nodeMap.TryGetValue(item, out var node))
                return false;
            
            // Use lazy deletion for simplicity
            node.IsDeleted = true;
            _nodeMap.Remove(item);
            _count--;
            
            return true;
        }
        
        public void Update(T item, Vector3 newPosition)
        {
            if (item == null || !_nodeMap.TryGetValue(item, out var node))
                return;
            
            // Simple approach: remove and reinsert
            // For better performance, check if the position change crosses the split plane
            node.IsDeleted = true;
            _nodeMap.Remove(item);
            
            var newNode = RentNode();
            newNode.Item = item;
            newNode.Position = newPosition;
            newNode.IsDeleted = false;
            
            if (_root == null || (_root.IsDeleted && _root.Left == null && _root.Right == null))
            {
                newNode.SplitAxis = 0;
                _root = newNode;
            }
            else
            {
                InsertNode(_root, newNode, 0);
            }
            
            _nodeMap[item] = newNode;
        }
        
        public void Clear()
        {
            ClearNode(_root);
            _root = null;
            _nodeMap.Clear();
            _count = 0;
        }
        
        private void ClearNode(KDNode node)
        {
            if (node == null) return;
            ClearNode(node.Left);
            ClearNode(node.Right);
            ReturnNode(node);
        }
        
        /// <summary>
        /// Builds a balanced tree from a list of items. More efficient than sequential inserts.
        /// </summary>
        public void BuildBalanced(IList<(T item, Vector3 pos)> items)
        {
            Clear();
            
            if (items == null || items.Count == 0) return;
            
            var sortedItems = new List<(T item, Vector3 pos)>(items);
            _root = BuildBalancedRecursive(sortedItems, 0, sortedItems.Count - 1, 0);
        }
        
        private KDNode BuildBalancedRecursive(List<(T item, Vector3 pos)> items, int start, int end, int depth)
        {
            if (start > end) return null;
            
            int axis = depth % 3;
            
            // Sort by current axis
            items.Sort(start, end - start + 1, new AxisComparer(axis));
            
            int mid = start + (end - start) / 2;
            
            var node = RentNode();
            node.Item = items[mid].item;
            node.Position = items[mid].pos;
            node.SplitAxis = axis;
            node.IsDeleted = false;
            
            _nodeMap[items[mid].item] = node;
            _count++;
            
            node.Left = BuildBalancedRecursive(items, start, mid - 1, depth + 1);
            if (node.Left != null) node.Left.Parent = node;
            
            node.Right = BuildBalancedRecursive(items, mid + 1, end, depth + 1);
            if (node.Right != null) node.Right.Parent = node;
            
            return node;
        }
        
        private class AxisComparer : IComparer<(T item, Vector3 pos)>
        {
            private readonly int _axis;
            public AxisComparer(int axis) => _axis = axis;
            
            public int Compare((T item, Vector3 pos) a, (T item, Vector3 pos) b)
            {
                return GetAxisValue(a.pos, _axis).CompareTo(GetAxisValue(b.pos, _axis));
            }
        }
        
        #endregion
        
        #region Queries
        
        public T QueryNearest(Vector3 position)
        {
            if (_root == null) return null;
            
            T best = null;
            float bestDistSq = float.MaxValue;
            
            QueryNearestRecursive(_root, position, ref best, ref bestDistSq);
            
            return best;
        }
        
        private void QueryNearestRecursive(KDNode node, Vector3 target, ref T best, ref float bestDistSq)
        {
            if (node == null) return;
            
            if (!node.IsDeleted)
            {
                float distSq = (node.Position - target).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = node.Item;
                }
            }
            
            int axis = node.SplitAxis;
            float targetValue = GetAxisValue(target, axis);
            float nodeValue = GetAxisValue(node.Position, axis);
            float diff = targetValue - nodeValue;
            
            // Search the near side first
            KDNode nearSide = diff < 0 ? node.Left : node.Right;
            KDNode farSide = diff < 0 ? node.Right : node.Left;
            
            QueryNearestRecursive(nearSide, target, ref best, ref bestDistSq);
            
            // Only search far side if it could contain a closer point
            if (diff * diff < bestDistSq)
            {
                QueryNearestRecursive(farSide, target, ref best, ref bestDistSq);
            }
        }
        
        public IEnumerable<T> QueryNearestN(Vector3 position, int count)
        {
            _nearestBuffer.Clear();
            
            if (_root == null || count <= 0)
                return _nearestBuffer.ConvertAll(x => x.item);
            
            QueryNearestNRecursive(_root, position, count);
            
            // Sort by distance
            _nearestBuffer.Sort((a, b) => a.distSq.CompareTo(b.distSq));
            
            var result = new List<T>(count);
            for (int i = 0; i < Mathf.Min(count, _nearestBuffer.Count); i++)
            {
                result.Add(_nearestBuffer[i].item);
            }
            
            return result;
        }
        
        private void QueryNearestNRecursive(KDNode node, Vector3 target, int count)
        {
            if (node == null) return;
            
            if (!node.IsDeleted)
            {
                float distSq = (node.Position - target).sqrMagnitude;
                
                if (_nearestBuffer.Count < count)
                {
                    _nearestBuffer.Add((node.Item, distSq));
                }
                else
                {
                    // Find max distance in buffer
                    float maxDistSq = 0;
                    int maxIndex = 0;
                    for (int i = 0; i < _nearestBuffer.Count; i++)
                    {
                        if (_nearestBuffer[i].distSq > maxDistSq)
                        {
                            maxDistSq = _nearestBuffer[i].distSq;
                            maxIndex = i;
                        }
                    }
                    
                    if (distSq < maxDistSq)
                    {
                        _nearestBuffer[maxIndex] = (node.Item, distSq);
                    }
                }
            }
            
            int axis = node.SplitAxis;
            float targetValue = GetAxisValue(target, axis);
            float nodeValue = GetAxisValue(node.Position, axis);
            float diff = targetValue - nodeValue;
            
            KDNode nearSide = diff < 0 ? node.Left : node.Right;
            KDNode farSide = diff < 0 ? node.Right : node.Left;
            
            QueryNearestNRecursive(nearSide, target, count);
            
            // Get current max distance in buffer
            float currentMaxDistSq = 0;
            foreach (var (_, d) in _nearestBuffer)
            {
                if (d > currentMaxDistSq) currentMaxDistSq = d;
            }
            
            // Only search far side if necessary
            if (_nearestBuffer.Count < count || diff * diff < currentMaxDistSq)
            {
                QueryNearestNRecursive(farSide, target, count);
            }
        }
        
        public IEnumerable<T> QueryRadius(Vector3 center, float radius)
        {
            _queryBuffer.Clear();
            QueryRadius(center, radius, _queryBuffer);
            return _queryBuffer;
        }
        
        public void QueryRadius(Vector3 center, float radius, List<T> results)
        {
            if (_root == null) return;
            
            float radiusSq = radius * radius;
            QueryRadiusRecursive(_root, center, radiusSq, results);
        }
        
        private void QueryRadiusRecursive(KDNode node, Vector3 center, float radiusSq, List<T> results)
        {
            if (node == null) return;
            
            if (!node.IsDeleted)
            {
                float distSq = (node.Position - center).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    results.Add(node.Item);
                }
            }
            
            int axis = node.SplitAxis;
            float centerValue = GetAxisValue(center, axis);
            float nodeValue = GetAxisValue(node.Position, axis);
            float diff = centerValue - nodeValue;
            float radius = Mathf.Sqrt(radiusSq);
            
            // Always search near side
            KDNode nearSide = diff < 0 ? node.Left : node.Right;
            KDNode farSide = diff < 0 ? node.Right : node.Left;
            
            QueryRadiusRecursive(nearSide, center, radiusSq, results);
            
            // Search far side if sphere intersects split plane
            if (Mathf.Abs(diff) <= radius)
            {
                QueryRadiusRecursive(farSide, center, radiusSq, results);
            }
        }
        
        public IEnumerable<T> QueryBox(Bounds bounds)
        {
            _queryBuffer.Clear();
            QueryBox(bounds, _queryBuffer);
            return _queryBuffer;
        }
        
        public void QueryBox(Bounds bounds, List<T> results)
        {
            if (_root == null) return;
            QueryBoxRecursive(_root, bounds, results);
        }
        
        private void QueryBoxRecursive(KDNode node, Bounds bounds, List<T> results)
        {
            if (node == null) return;
            
            if (!node.IsDeleted && bounds.Contains(node.Position))
            {
                results.Add(node.Item);
            }
            
            int axis = node.SplitAxis;
            float nodeValue = GetAxisValue(node.Position, axis);
            float minValue = GetAxisValue(bounds.min, axis);
            float maxValue = GetAxisValue(bounds.max, axis);
            
            if (minValue <= nodeValue)
            {
                QueryBoxRecursive(node.Left, bounds, results);
            }
            
            if (maxValue >= nodeValue)
            {
                QueryBoxRecursive(node.Right, bounds, results);
            }
        }
        
        #endregion
        
        #region Helpers
        
        private static float GetAxisValue(Vector3 v, int axis)
        {
            switch (axis)
            {
                case 0: return v.x;
                case 1: return v.y;
                case 2: return v.z;
                default: return v.x;
            }
        }
        
        private KDNode RentNode()
        {
            if (_nodePool.Count > 0)
                return _nodePool.Pop();
            return new KDNode();
        }
        
        private void ReturnNode(KDNode node)
        {
            node.Reset();
            if (_nodePool.Count < 256)
                _nodePool.Push(node);
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Gets the position of an item.
        /// </summary>
        public bool TryGetPosition(T item, out Vector3 position)
        {
            if (_nodeMap.TryGetValue(item, out var node))
            {
                position = node.Position;
                return true;
            }
            position = default;
            return false;
        }
        
        /// <summary>
        /// Gets the depth of the tree.
        /// </summary>
        public int GetDepth()
        {
            return GetDepthRecursive(_root);
        }
        
        private int GetDepthRecursive(KDNode node)
        {
            if (node == null) return 0;
            return 1 + Mathf.Max(GetDepthRecursive(node.Left), GetDepthRecursive(node.Right));
        }
        
        #endregion
    }
}
