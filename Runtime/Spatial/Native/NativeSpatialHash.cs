using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Eraflo.Catalyst.Spatial.Native
{
    /// <summary>
    /// Burst-compatible spatial hash using NativeContainers.
    /// Designed for high-performance queries in jobs.
    /// </summary>
    [BurstCompile]
    public struct NativeSpatialHash : IDisposable
    {
        private NativeParallelMultiHashMap<int, int> _cells;
        private NativeArray<float3> _positions;
        private NativeArray<bool> _active;
        
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private int _count;
        private readonly int _capacity;
        
        public int Count => _count;
        public int Capacity => _capacity;
        public bool IsCreated => _cells.IsCreated;
        
        /// <summary>
        /// Creates a new native spatial hash.
        /// </summary>
        /// <param name="capacity">Maximum number of items.</param>
        /// <param name="cellSize">Size of each cell in world units.</param>
        /// <param name="allocator">Memory allocator to use.</param>
        public NativeSpatialHash(int capacity, float cellSize, Allocator allocator)
        {
            _capacity = capacity;
            _cellSize = math.max(cellSize, 0.1f);
            _inverseCellSize = 1f / _cellSize;
            _count = 0;
            
            _cells = new NativeParallelMultiHashMap<int, int>(capacity * 2, allocator);
            _positions = new NativeArray<float3>(capacity, allocator);
            _active = new NativeArray<bool>(capacity, allocator);
        }
        
        /// <summary>
        /// Adds an item at the specified index and position.
        /// </summary>
        public void Add(int index, float3 position)
        {
            if (index < 0 || index >= _capacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            int cellKey = GetCellKey(position);
            _positions[index] = position;
            _active[index] = true;
            _cells.Add(cellKey, index);
            _count++;
        }
        
        /// <summary>
        /// Removes an item at the specified index.
        /// </summary>
        public void Remove(int index)
        {
            if (index < 0 || index >= _capacity || !_active[index])
                return;
            
            float3 pos = _positions[index];
            int cellKey = GetCellKey(pos);
            
            // Remove from hash map
            if (_cells.TryGetFirstValue(cellKey, out int foundIndex, out var iterator))
            {
                do
                {
                    if (foundIndex == index)
                    {
                        _cells.Remove(iterator);
                        break;
                    }
                } while (_cells.TryGetNextValue(out foundIndex, ref iterator));
            }
            
            _active[index] = false;
            _count--;
        }
        
        /// <summary>
        /// Updates the position of an item.
        /// </summary>
        public void Update(int index, float3 newPosition)
        {
            if (index < 0 || index >= _capacity || !_active[index])
                return;
            
            float3 oldPos = _positions[index];
            int oldCellKey = GetCellKey(oldPos);
            int newCellKey = GetCellKey(newPosition);
            
            _positions[index] = newPosition;
            
            if (oldCellKey != newCellKey)
            {
                // Remove from old cell
                if (_cells.TryGetFirstValue(oldCellKey, out int foundIndex, out var iterator))
                {
                    do
                    {
                        if (foundIndex == index)
                        {
                            _cells.Remove(iterator);
                            break;
                        }
                    } while (_cells.TryGetNextValue(out foundIndex, ref iterator));
                }
                
                // Add to new cell
                _cells.Add(newCellKey, index);
            }
        }
        
        /// <summary>
        /// Clears all items.
        /// </summary>
        public void Clear()
        {
            _cells.Clear();
            for (int i = 0; i < _capacity; i++)
            {
                _active[i] = false;
            }
            _count = 0;
        }
        
        /// <summary>
        /// Queries all items within a radius (main thread).
        /// </summary>
        public void QueryRadius(float3 center, float radius, NativeList<int> results)
        {
            float radiusSq = radius * radius;
            
            int minX = (int)math.floor((center.x - radius) * _inverseCellSize);
            int maxX = (int)math.floor((center.x + radius) * _inverseCellSize);
            int minY = (int)math.floor((center.y - radius) * _inverseCellSize);
            int maxY = (int)math.floor((center.y + radius) * _inverseCellSize);
            int minZ = (int)math.floor((center.z - radius) * _inverseCellSize);
            int maxZ = (int)math.floor((center.z + radius) * _inverseCellSize);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int cellKey = HashCoords(x, y, z);
                        
                        if (_cells.TryGetFirstValue(cellKey, out int index, out var iterator))
                        {
                            do
                            {
                                if (_active[index])
                                {
                                    float3 pos = _positions[index];
                                    float distSq = math.distancesq(pos, center);
                                    if (distSq <= radiusSq)
                                    {
                                        results.Add(index);
                                    }
                                }
                            } while (_cells.TryGetNextValue(out index, ref iterator));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the position of an item.
        /// </summary>
        public float3 GetPosition(int index)
        {
            return _positions[index];
        }
        
        /// <summary>
        /// Checks if an item is active.
        /// </summary>
        public bool IsActive(int index)
        {
            return index >= 0 && index < _capacity && _active[index];
        }
        
        private int GetCellKey(float3 position)
        {
            int x = (int)math.floor(position.x * _inverseCellSize);
            int y = (int)math.floor(position.y * _inverseCellSize);
            int z = (int)math.floor(position.z * _inverseCellSize);
            return HashCoords(x, y, z);
        }
        
        private static int HashCoords(int x, int y, int z)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                return hash;
            }
        }
        
        public void Dispose()
        {
            if (_cells.IsCreated) _cells.Dispose();
            if (_positions.IsCreated) _positions.Dispose();
            if (_active.IsCreated) _active.Dispose();
        }
    }
    
    /// <summary>
    /// Burst-compiled job for radius queries.
    /// </summary>
    [BurstCompile]
    public struct SpatialHashRadiusQueryJob : IJob
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, int> Cells;
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<bool> Active;
        
        public float3 Center;
        public float RadiusSq;
        public float InverseCellSize;
        public float Radius;
        
        public NativeList<int> Results;
        
        public void Execute()
        {
            int minX = (int)math.floor((Center.x - Radius) * InverseCellSize);
            int maxX = (int)math.floor((Center.x + Radius) * InverseCellSize);
            int minY = (int)math.floor((Center.y - Radius) * InverseCellSize);
            int maxY = (int)math.floor((Center.y + Radius) * InverseCellSize);
            int minZ = (int)math.floor((Center.z - Radius) * InverseCellSize);
            int maxZ = (int)math.floor((Center.z + Radius) * InverseCellSize);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int cellKey = HashCoords(x, y, z);
                        
                        if (Cells.TryGetFirstValue(cellKey, out int index, out var iterator))
                        {
                            do
                            {
                                if (Active[index])
                                {
                                    float3 pos = Positions[index];
                                    float distSq = math.distancesq(pos, Center);
                                    if (distSq <= RadiusSq)
                                    {
                                        Results.Add(index);
                                    }
                                }
                            } while (Cells.TryGetNextValue(out index, ref iterator));
                        }
                    }
                }
            }
        }
        
        private static int HashCoords(int x, int y, int z)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                return hash;
            }
        }
    }
}
