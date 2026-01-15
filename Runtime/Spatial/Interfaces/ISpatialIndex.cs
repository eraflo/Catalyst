using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Spatial
{
    /// <summary>
    /// Generic interface for spatial indexing data structures.
    /// Provides common operations for insertion, removal, and spatial queries.
    /// </summary>
    /// <typeparam name="T">Type of items stored in the spatial index.</typeparam>
    public interface ISpatialIndex<T> where T : class
    {
        /// <summary>
        /// Number of items currently in the index.
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// Inserts an item at the specified position.
        /// </summary>
        /// <param name="item">Item to insert.</param>
        /// <param name="position">World position of the item.</param>
        void Insert(T item, Vector3 position);
        
        /// <summary>
        /// Removes an item from the index.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was found and removed.</returns>
        bool Remove(T item);
        
        /// <summary>
        /// Updates the position of an existing item.
        /// </summary>
        /// <param name="item">Item to update.</param>
        /// <param name="newPosition">New world position.</param>
        void Update(T item, Vector3 newPosition);
        
        /// <summary>
        /// Removes all items from the index.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Queries all items within a spherical radius.
        /// </summary>
        /// <param name="center">Center of the query sphere.</param>
        /// <param name="radius">Radius of the query sphere.</param>
        /// <returns>Enumerable of items within the radius.</returns>
        IEnumerable<T> QueryRadius(Vector3 center, float radius);
        
        /// <summary>
        /// Queries all items within a spherical radius (non-allocating).
        /// </summary>
        /// <param name="center">Center of the query sphere.</param>
        /// <param name="radius">Radius of the query sphere.</param>
        /// <param name="results">List to populate with results.</param>
        void QueryRadius(Vector3 center, float radius, List<T> results);
        
        /// <summary>
        /// Finds the nearest item to the specified position.
        /// </summary>
        /// <param name="position">Query position.</param>
        /// <returns>Nearest item, or null if empty.</returns>
        T QueryNearest(Vector3 position);
        
        /// <summary>
        /// Finds the N nearest items to the specified position.
        /// </summary>
        /// <param name="position">Query position.</param>
        /// <param name="count">Number of nearest items to find.</param>
        /// <returns>Enumerable of nearest items, ordered by distance.</returns>
        IEnumerable<T> QueryNearestN(Vector3 position, int count);
        
        /// <summary>
        /// Queries all items within an axis-aligned bounding box.
        /// </summary>
        /// <param name="bounds">The bounding box to query.</param>
        /// <returns>Enumerable of items within the bounds.</returns>
        IEnumerable<T> QueryBox(Bounds bounds);
        
        /// <summary>
        /// Queries all items within an axis-aligned bounding box (non-allocating).
        /// </summary>
        /// <param name="bounds">The bounding box to query.</param>
        /// <param name="results">List to populate with results.</param>
        void QueryBox(Bounds bounds, List<T> results);
    }
}
