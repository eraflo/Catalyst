using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.Spatial;

namespace Eraflo.Catalyst.Networking.Features.Culling
{
    /// <summary>
    /// Marker interface for objects that can be network-culled.
    /// </summary>
    public interface ICullable
    {
        /// <summary>Network ID of this object.</summary>
        uint NetworkId { get; }
        
        /// <summary>World position for culling calculations.</summary>
        Vector3 CullPosition { get; }
        
        /// <summary>Whether this object is currently visible.</summary>
        bool IsVisible { get; set; }
    }
    
    /// <summary>
    /// Service for automatic network visibility management.
    /// Uses spatial hashing for efficient range queries.
    /// </summary>
    [Service(Priority = 11)]
    public class NetworkCullingManager : IGameService
    {
        private NetworkManager _networkManager;
        private NetworkIdManager _idManager;
        
        // Spatial index for cullable objects
        private SpatialHash<ICullable> _spatialHash;
        
        // Track visibility per client
        private readonly Dictionary<ulong, HashSet<uint>> _clientVisibility = new();
        
        // Registered culling areas (players)
        private readonly Dictionary<ulong, NetworkCullingArea> _cullingAreas = new();
        
        // Staggered update tracking
        private readonly List<ulong> _clientList = new();
        private int _currentClientIndex;
        private int _clientsPerFrame = 4;
        
        // Pool for query results
        private readonly List<ICullable> _queryResults = new();
        
        #region Configuration
        
        /// <summary>Cell size for spatial hash. Larger = fewer cells, less precision.</summary>
        public float CellSize { get; set; } = 50f;
        
        /// <summary>Whether culling is enabled.</summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>Whether to use staggered updates (spread across frames).</summary>
        public bool UseStaggeredUpdates { get; set; } = true;
        
        #endregion
        
        #region IGameService
        
        /// <summary>
        /// Initializes the culling manager, loading settings from PackageSettings.
        /// </summary>
        public void Initialize()
        {
            _networkManager = App.Get<NetworkManager>();
            _idManager = App.Get<NetworkIdManager>();
            
            // Load settings from PackageSettings
            var settings = PackageSettings.Instance;
            CellSize = settings.CullingCellSize;
            _clientsPerFrame = settings.CullingClientsPerFrame;
            
            _spatialHash = new SpatialHash<ICullable>(CellSize, ignoreY: true);
            
            if (_networkManager != null)
            {
                _networkManager.OnClientConnected += HandleClientConnected;
                _networkManager.OnClientDisconnected += HandleClientDisconnected;
            }
        }
        
        /// <summary>
        /// Shuts down the culling manager and cleans up resources.
        /// </summary>
        public void Shutdown()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnected -= HandleClientConnected;
                _networkManager.OnClientDisconnected -= HandleClientDisconnected;
            }
            
            _spatialHash?.Clear();
            _clientVisibility.Clear();
            _cullingAreas.Clear();
            _clientList.Clear();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Registers a cullable object for visibility management.
        /// </summary>
        public void RegisterCullable(ICullable cullable)
        {
            if (cullable == null) return;
            
            _spatialHash.Insert(cullable, cullable.CullPosition);
        }
        
        /// <summary>
        /// Unregisters a cullable object.
        /// </summary>
        public void UnregisterCullable(ICullable cullable)
        {
            if (cullable == null) return;
            
            _spatialHash.Remove(cullable);
            
            // Remove from all client visibility sets
            foreach (var visibility in _clientVisibility.Values)
            {
                visibility.Remove(cullable.NetworkId);
            }
        }
        
        /// <summary>
        /// Updates the position of a cullable object.
        /// </summary>
        public void UpdateCullablePosition(ICullable cullable)
        {
            if (cullable == null) return;
            
            _spatialHash.Update(cullable, cullable.CullPosition);
        }
        
        /// <summary>
        /// Registers a culling area for a client.
        /// </summary>
        public void RegisterCullingArea(ulong clientId, NetworkCullingArea area)
        {
            if (area == null) return;
            
            _cullingAreas[clientId] = area;
            
            if (!_clientList.Contains(clientId))
                _clientList.Add(clientId);
        }
        
        /// <summary>
        /// Unregisters a culling area.
        /// </summary>
        public void UnregisterCullingArea(ulong clientId)
        {
            _cullingAreas.Remove(clientId);
            _clientList.Remove(clientId);
        }
        
        /// <summary>
        /// Performs culling update. Call from a MonoBehaviour's Update.
        /// </summary>
        public void UpdateCulling()
        {
            if (!Enabled || !_networkManager.IsServer || _cullingAreas.Count == 0)
                return;
            
            var backend = _networkManager.Backend as ICullingBackend;
            if (backend == null)
                return;
            
            if (UseStaggeredUpdates)
            {
                UpdateCullingStaggered(backend);
            }
            else
            {
                UpdateCullingAll(backend);
            }
        }
        
        /// <summary>
        /// Gets visibility set for a client.
        /// </summary>
        public IReadOnlyCollection<uint> GetVisibleObjects(ulong clientId)
        {
            if (_clientVisibility.TryGetValue(clientId, out var set))
                return set;
            return System.Array.Empty<uint>();
        }
        
        #endregion
        
        #region Update Methods
        
        private void UpdateCullingStaggered(ICullingBackend backend)
        {
            if (_clientList.Count == 0) return;
            
            int processed = 0;
            while (processed < _clientsPerFrame && processed < _clientList.Count)
            {
                _currentClientIndex = (_currentClientIndex + 1) % _clientList.Count;
                ulong clientId = _clientList[_currentClientIndex];
                
                if (_cullingAreas.TryGetValue(clientId, out var area))
                {
                    UpdateClientVisibility(clientId, area, backend);
                }
                
                processed++;
            }
        }
        
        private void UpdateCullingAll(ICullingBackend backend)
        {
            foreach (var kvp in _cullingAreas)
            {
                UpdateClientVisibility(kvp.Key, kvp.Value, backend);
            }
        }
        
        private void UpdateClientVisibility(ulong clientId, NetworkCullingArea area, ICullingBackend backend)
        {
            if (!_clientVisibility.TryGetValue(clientId, out var currentVisibility))
            {
                currentVisibility = new HashSet<uint>();
                _clientVisibility[clientId] = currentVisibility;
            }
            
            // Query objects in range
            _queryResults.Clear();
            _spatialHash.QueryRadius(area.Position, area.OuterRadius, _queryResults);
            
            var newVisibility = new HashSet<uint>();
            float innerRadiusSq = area.Radius * area.Radius;
            float outerRadiusSq = area.OuterRadius * area.OuterRadius;
            
            foreach (var cullable in _queryResults)
            {
                float distSq = area.GetSqrDistance(cullable.CullPosition);
                
                // Inside inner radius -> always visible
                // Between inner and outer -> keep current state (hysteresis)
                // Outside outer -> hidden (but this shouldn't be in query results)
                
                bool shouldBeVisible;
                if (distSq <= innerRadiusSq)
                {
                    shouldBeVisible = true;
                }
                else if (distSq <= outerRadiusSq && currentVisibility.Contains(cullable.NetworkId))
                {
                    // In hysteresis zone and currently visible -> stay visible
                    shouldBeVisible = true;
                }
                else
                {
                    shouldBeVisible = false;
                }
                
                if (shouldBeVisible)
                {
                    newVisibility.Add(cullable.NetworkId);
                }
            }
            
            // Calculate changes
            foreach (uint id in newVisibility)
            {
                if (!currentVisibility.Contains(id))
                {
                    // New visible object
                    backend.NetworkShow(id, clientId);
                }
            }
            
            foreach (uint id in currentVisibility)
            {
                if (!newVisibility.Contains(id))
                {
                    // Object is no longer visible
                    backend.NetworkHide(id, clientId);
                }
            }
            
            // Update stored visibility
            _clientVisibility[clientId] = newVisibility;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleClientConnected(ulong clientId)
        {
            _clientVisibility[clientId] = new HashSet<uint>();
        }
        
        private void HandleClientDisconnected(ulong clientId)
        {
            _clientVisibility.Remove(clientId);
            _cullingAreas.Remove(clientId);
            _clientList.Remove(clientId);
        }
        
        #endregion
    }
}
