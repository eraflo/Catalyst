using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Spatial
{
    /// <summary>
    /// O(1) grid-based spatial partitioning using a hash map of cells.
    /// Optimized for uniform object distribution and radius queries.
    /// </summary>
    /// <typeparam name="T">Type of items stored.</typeparam>
    public class SpatialHash<T> : ISpatialIndex<T> where T : class
    {
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly bool _ignoreY;
        
        // Cell storage: hash -> items in cell
        private readonly Dictionary<int, HashSet<T>> _cells = new();
        
        // Reverse lookup: item -> (position, cellKey)
        private readonly Dictionary<T, (Vector3 pos, int cellKey)> _items = new();
        
        // Pool of reusable HashSets to avoid GC
        private readonly Stack<HashSet<T>> _hashSetPool = new();
        
        // Reusable list for queries
        private readonly List<T> _queryBuffer = new();
        
        public int Count => _items.Count;
        public float CellSize => _cellSize;
        
        /// <summary>
        /// Creates a new spatial hash with the specified cell size.
        /// </summary>
        /// <param name="cellSize">Size of each cell in world units.</param>
        /// <param name="ignoreY">If true, uses 2D hashing (XZ plane).</param>
        public SpatialHash(float cellSize = 50f, bool ignoreY = false)
        {
            _cellSize = Mathf.Max(cellSize, 0.1f);
            _inverseCellSize = 1f / _cellSize;
            _ignoreY = ignoreY;
        }
        
        #region Core Operations
        
        public void Insert(T item, Vector3 position)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_items.ContainsKey(item))
            {
                Update(item, position);
                return;
            }
            
            int cellKey = GetCellKey(position);
            
            if (!_cells.TryGetValue(cellKey, out var cell))
            {
                cell = RentHashSet();
                _cells[cellKey] = cell;
            }
            
            cell.Add(item);
            _items[item] = (position, cellKey);
        }
        
        public bool Remove(T item)
        {
            if (item == null || !_items.TryGetValue(item, out var data))
                return false;
            
            if (_cells.TryGetValue(data.cellKey, out var cell))
            {
                cell.Remove(item);
                
                // Return empty cells to pool
                if (cell.Count == 0)
                {
                    _cells.Remove(data.cellKey);
                    ReturnHashSet(cell);
                }
            }
            
            _items.Remove(item);
            return true;
        }
        
        public void Update(T item, Vector3 newPosition)
        {
            if (item == null || !_items.TryGetValue(item, out var data))
                return;
            
            int newCellKey = GetCellKey(newPosition);
            
            // Optimisation: skip if cell hasn't changed
            if (newCellKey == data.cellKey)
            {
                _items[item] = (newPosition, data.cellKey);
                return;
            }
            
            // Remove from old cell
            if (_cells.TryGetValue(data.cellKey, out var oldCell))
            {
                oldCell.Remove(item);
                if (oldCell.Count == 0)
                {
                    _cells.Remove(data.cellKey);
                    ReturnHashSet(oldCell);
                }
            }
            
            // Add to new cell
            if (!_cells.TryGetValue(newCellKey, out var newCell))
            {
                newCell = RentHashSet();
                _cells[newCellKey] = newCell;
            }
            
            newCell.Add(item);
            _items[item] = (newPosition, newCellKey);
        }
        
        public void Clear()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Clear();
                ReturnHashSet(cell);
            }
            _cells.Clear();
            _items.Clear();
        }
        
        #endregion
        
        #region Queries
        
        public IEnumerable<T> QueryRadius(Vector3 center, float radius)
        {
            _queryBuffer.Clear();
            QueryRadius(center, radius, _queryBuffer);
            return _queryBuffer;
        }
        
        public void QueryRadius(Vector3 center, float radius, List<T> results)
        {
            float radiusSq = radius * radius;
            
            // Calculate cell range to check
            int minX = Mathf.FloorToInt((center.x - radius) * _inverseCellSize);
            int maxX = Mathf.FloorToInt((center.x + radius) * _inverseCellSize);
            int minZ = Mathf.FloorToInt((center.z - radius) * _inverseCellSize);
            int maxZ = Mathf.FloorToInt((center.z + radius) * _inverseCellSize);
            
            int minY = 0, maxY = 0;
            if (!_ignoreY)
            {
                minY = Mathf.FloorToInt((center.y - radius) * _inverseCellSize);
                maxY = Mathf.FloorToInt((center.y + radius) * _inverseCellSize);
            }
            
            // Iterate over relevant cells
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (_ignoreY)
                    {
                        int cellKey = HashCoords(x, 0, z);
                        CheckCellForRadius(cellKey, center, radiusSq, results);
                    }
                    else
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            int cellKey = HashCoords(x, y, z);
                            CheckCellForRadius(cellKey, center, radiusSq, results);
                        }
                    }
                }
            }
        }
        
        private void CheckCellForRadius(int cellKey, Vector3 center, float radiusSq, List<T> results)
        {
            if (!_cells.TryGetValue(cellKey, out var cell))
                return;
            
            foreach (var item in cell)
            {
                if (_items.TryGetValue(item, out var data))
                {
                    float distSq = (data.pos - center).sqrMagnitude;
                    if (distSq <= radiusSq)
                    {
                        results.Add(item);
                    }
                }
            }
        }
        
        public T QueryNearest(Vector3 position)
        {
            T nearest = null;
            float nearestDistSq = float.MaxValue;
            
            // Start with local cell, expand outward
            int centerX = Mathf.FloorToInt(position.x * _inverseCellSize);
            int centerY = _ignoreY ? 0 : Mathf.FloorToInt(position.y * _inverseCellSize);
            int centerZ = Mathf.FloorToInt(position.z * _inverseCellSize);
            
            // Search expanding rings until we find something
            for (int ring = 0; ring <= 10; ring++) // Limit search radius
            {
                bool foundInRing = false;
                
                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dz = -ring; dz <= ring; dz++)
                    {
                        if (_ignoreY)
                        {
                            int cellKey = HashCoords(centerX + dx, 0, centerZ + dz);
                            if (CheckCellForNearest(cellKey, position, ref nearest, ref nearestDistSq))
                                foundInRing = true;
                        }
                        else
                        {
                            for (int dy = -ring; dy <= ring; dy++)
                            {
                                int cellKey = HashCoords(centerX + dx, centerY + dy, centerZ + dz);
                                if (CheckCellForNearest(cellKey, position, ref nearest, ref nearestDistSq))
                                    foundInRing = true;
                            }
                        }
                    }
                }
                
                // If we found something and searched the adjacent ring, we're done
                if (nearest != null && ring > 0)
                    break;
            }
            
            return nearest;
        }
        
        private bool CheckCellForNearest(int cellKey, Vector3 position, ref T nearest, ref float nearestDistSq)
        {
            if (!_cells.TryGetValue(cellKey, out var cell))
                return false;
            
            bool found = false;
            foreach (var item in cell)
            {
                if (_items.TryGetValue(item, out var data))
                {
                    float distSq = (data.pos - position).sqrMagnitude;
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = item;
                        found = true;
                    }
                }
            }
            return found;
        }
        
        public IEnumerable<T> QueryNearestN(Vector3 position, int count)
        {
            if (count <= 0) yield break;
            
            // Use a simple sorted approach for small N
            var candidates = new List<(T item, float distSq)>();
            
            foreach (var kvp in _items)
            {
                float distSq = (kvp.Value.pos - position).sqrMagnitude;
                candidates.Add((kvp.Key, distSq));
            }
            
            candidates.Sort((a, b) => a.distSq.CompareTo(b.distSq));
            
            int returned = 0;
            foreach (var (item, _) in candidates)
            {
                if (returned >= count) yield break;
                yield return item;
                returned++;
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
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            
            int minX = Mathf.FloorToInt(min.x * _inverseCellSize);
            int maxX = Mathf.FloorToInt(max.x * _inverseCellSize);
            int minZ = Mathf.FloorToInt(min.z * _inverseCellSize);
            int maxZ = Mathf.FloorToInt(max.z * _inverseCellSize);
            
            int minY = 0, maxY = 0;
            if (!_ignoreY)
            {
                minY = Mathf.FloorToInt(min.y * _inverseCellSize);
                maxY = Mathf.FloorToInt(max.y * _inverseCellSize);
            }
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (_ignoreY)
                    {
                        int cellKey = HashCoords(x, 0, z);
                        CheckCellForBox(cellKey, bounds, results);
                    }
                    else
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            int cellKey = HashCoords(x, y, z);
                            CheckCellForBox(cellKey, bounds, results);
                        }
                    }
                }
            }
        }
        
        private void CheckCellForBox(int cellKey, Bounds bounds, List<T> results)
        {
            if (!_cells.TryGetValue(cellKey, out var cell))
                return;
            
            foreach (var item in cell)
            {
                if (_items.TryGetValue(item, out var data))
                {
                    if (bounds.Contains(data.pos))
                    {
                        results.Add(item);
                    }
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private int GetCellKey(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x * _inverseCellSize);
            int y = _ignoreY ? 0 : Mathf.FloorToInt(position.y * _inverseCellSize);
            int z = Mathf.FloorToInt(position.z * _inverseCellSize);
            return HashCoords(x, y, z);
        }
        
        private static int HashCoords(int x, int y, int z)
        {
            // Standard hash combining
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                return hash;
            }
        }
        
        private HashSet<T> RentHashSet()
        {
            if (_hashSetPool.Count > 0)
                return _hashSetPool.Pop();
            return new HashSet<T>();
        }
        
        private void ReturnHashSet(HashSet<T> set)
        {
            set.Clear();
            if (_hashSetPool.Count < 64) // Cap pool size
                _hashSetPool.Push(set);
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Gets the position of an item.
        /// </summary>
        public bool TryGetPosition(T item, out Vector3 position)
        {
            if (_items.TryGetValue(item, out var data))
            {
                position = data.pos;
                return true;
            }
            position = default;
            return false;
        }
        
        /// <summary>
        /// Gets the number of cells currently allocated.
        /// </summary>
        public int CellCount => _cells.Count;
        
        #endregion
    }
}
