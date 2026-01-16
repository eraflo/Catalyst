using System;
using System.Collections.Generic;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Pooling;
using Eraflo.Catalyst.Spatial;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Spawn
{
    /// <summary>
    /// Payload data for spawn configuration sent by clients.
    /// </summary>
    [Serializable]
    public struct SpawnPayload
    {
        /// <summary>Prefab key to spawn (e.g., class selection).</summary>
        public string PrefabKey;

        /// <summary>Team ID to spawn on.</summary>
        public int TeamId;

        /// <summary>Optional spawn tag filter.</summary>
        public string SpawnTag;
    }

    /// <summary>
    /// Service that manages player spawning using spawn points and strategies.
    /// Only the server (in ServerAuthoritative mode) or the owner (in OwnerAuthoritative mode) can spawn.
    /// </summary>
    [Service(Priority = 7)]
    public class NetworkSpawnManager : IGameService
    {
        private readonly List<NetworkSpawnPoint> _spawnPoints = new();
        private readonly Dictionary<ulong, SpawnPayload> _clientPayloads = new();
        private readonly Dictionary<ulong, GameObject> _spawnedPlayers = new();

        private ISpawnStrategy _strategy = new RandomSpawnStrategy();
        private KDTree<NetworkSpawnPoint> _spatialIndex;
        private bool _autoSpawnEnabled = true;

        private NetworkManager _networkManager;
        private ConnectionManager _connectionManager;

        #region Properties

        /// <summary>Current spawn strategy.</summary>
        public ISpawnStrategy Strategy
        {
            get => _strategy;
            set => _strategy = value ?? new RandomSpawnStrategy();
        }

        /// <summary>Whether to automatically spawn players on connect.</summary>
        public bool AutoSpawnEnabled
        {
            get => _autoSpawnEnabled;
            set => _autoSpawnEnabled = value;
        }

        /// <summary>Default prefab key to use if no payload specifies one.</summary>
        public string DefaultPrefabKey { get; set; } = "Player";

        /// <summary>Read-only list of registered spawn points.</summary>
        public IReadOnlyList<NetworkSpawnPoint> SpawnPoints => _spawnPoints;

        #endregion

        #region Events

        /// <summary>Fired before spawning a player. Return false to cancel spawn.</summary>
        public event Func<ulong, SpawnPayload, bool> OnBeforeSpawn;

        /// <summary>Fired after a player has been spawned.</summary>
        public event Action<ulong, GameObject> OnPlayerSpawned;

        /// <summary>Fired when a player is despawned.</summary>
        public event Action<ulong, GameObject> OnPlayerDespawned;

        #endregion

        #region IGameService

        /// <summary>
        /// Initializes the spawn manager and registers for network events.
        /// </summary>
        public void Initialize()
        {
            _networkManager = App.Get<NetworkManager>();
            _connectionManager = App.Get<ConnectionManager>();

            if (_networkManager != null)
            {
                _networkManager.OnClientConnected += HandleClientConnected;
                _networkManager.OnClientDisconnected += HandleClientDisconnected;
            }

            RefreshSpawnPoints();
        }

        /// <summary>
        /// Shuts down the spawn manager and unregisters from network events.
        /// </summary>
        public void Shutdown()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnected -= HandleClientConnected;
                _networkManager.OnClientDisconnected -= HandleClientDisconnected;
            }

            _spawnPoints.Clear();
            _clientPayloads.Clear();
            _spawnedPlayers.Clear();
            _spatialIndex?.Clear();
        }

        #endregion

        #region Spawn Point Management

        /// <summary>
        /// Registers a spawn point. Called automatically by NetworkSpawnPoint.OnEnable.
        /// </summary>
        public void RegisterSpawnPoint(NetworkSpawnPoint point)
        {
            if (point == null || _spawnPoints.Contains(point))
                return;

            _spawnPoints.Add(point);
            _spatialIndex?.Insert(point, point.Position);
        }

        /// <summary>
        /// Unregisters a spawn point. Called automatically by NetworkSpawnPoint.OnDisable.
        /// </summary>
        public void UnregisterSpawnPoint(NetworkSpawnPoint point)
        {
            if (point == null)
                return;

            _spawnPoints.Remove(point);
            _spatialIndex?.Remove(point);
        }

        /// <summary>
        /// Refreshes the spawn point list by finding all in scene.
        /// </summary>
        public void RefreshSpawnPoints()
        {
            _spawnPoints.Clear();
            _spawnPoints.AddRange(UnityEngine.Object.FindObjectsByType<NetworkSpawnPoint>(FindObjectsSortMode.None));

            // Rebuild spatial index if we have many points
            if (_spawnPoints.Count > 20)
            {
                _spatialIndex = new KDTree<NetworkSpawnPoint>();
                var items = new List<(NetworkSpawnPoint, Vector3)>();
                foreach (var point in _spawnPoints)
                {
                    items.Add((point, point.Position));
                }
                _spatialIndex.BuildBalanced(items);
            }
            else
            {
                _spatialIndex = null;
            }
        }

        /// <summary>
        /// Gets spawn points near a position (uses spatial index if available).
        /// </summary>
        public IEnumerable<NetworkSpawnPoint> GetSpawnPointsNear(Vector3 position, float radius)
        {
            if (_spatialIndex != null)
            {
                return _spatialIndex.QueryRadius(position, radius);
            }

            // Fallback to linear search
            var results = new List<NetworkSpawnPoint>();
            float radiusSq = radius * radius;
            foreach (var point in _spawnPoints)
            {
                if ((point.Position - position).sqrMagnitude <= radiusSq)
                {
                    results.Add(point);
                }
            }
            return results;
        }

        #endregion

        #region Spawning

        /// <summary>
        /// Spawns a player for the specified client.
        /// </summary>
        /// <param name="clientId">Client to spawn for.</param>
        /// <param name="overridePayload">Optional payload override.</param>
        /// <returns>The spawned GameObject, or null if failed.</returns>
        public GameObject SpawnPlayerForClient(ulong clientId, SpawnPayload? overridePayload = null)
        {
            if (PackageSettings.Instance.NetworkDebugMode) Debug.Log($"[NetworkSpawnManager] Attempting to spawn player for client {clientId}");

            // Check authority - only server can spawn in ServerAuthoritative mode
            var authorityMode = PackageSettings.Instance.DefaultAuthorityMode;
            if (_networkManager == null)
            {
                Debug.LogWarning("[NetworkSpawnManager] NetworkManager not available.");
                return null;
            }

            if (authorityMode == AuthorityMode.ServerAuthoritative && !_networkManager.IsServer)
            {
                Debug.LogWarning("[NetworkSpawnManager] Only server can spawn players in ServerAuthoritative mode.");
                return null;
            }

            // Get payload
            SpawnPayload payload;
            if (overridePayload.HasValue)
            {
                payload = overridePayload.Value;
            }
            else if (_clientPayloads.TryGetValue(clientId, out var storedPayload))
            {
                payload = storedPayload;
            }
            else
            {
                payload = new SpawnPayload
                {
                    PrefabKey = DefaultPrefabKey,
                    TeamId = -1,
                    SpawnTag = ""
                };
            }

            // Pre-spawn hook
            if (OnBeforeSpawn != null && !OnBeforeSpawn.Invoke(clientId, payload))
            {
                Debug.Log($"[NetworkSpawnManager] Spawn cancelled for client {clientId}");
                return null;
            }

            // Select spawn point
            Debug.Log($"[NetworkSpawnManager] Selecting spawn point from {_spawnPoints.Count} points...");
            var spawnPoint = _strategy.SelectSpawnPoint(_spawnPoints, clientId, payload.TeamId, payload.SpawnTag);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"[NetworkSpawnManager] No available spawn point for client {clientId} (Points in list: {_spawnPoints.Count})");
                return null;
            }

            // Mark as occupied
            spawnPoint.MarkOccupied();

            // Backend handles actual network object creation and ownership assignment
            var player = _networkManager.SpawnPlayer(clientId, spawnPoint.Position, spawnPoint.Rotation);

            if (player != null)
            {
                _spawnedPlayers[clientId] = player;
            }

            OnPlayerSpawned?.Invoke(clientId, player);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkSpawnManager] Spawned player for client {clientId} at {spawnPoint.Position}");
            }

            return player;
        }

        /// <summary>
        /// Despawns a player for the specified client.
        /// </summary>
        public void DespawnPlayer(ulong clientId)
        {
            if (!_networkManager.IsServer)
                return;

            if (_spawnedPlayers.TryGetValue(clientId, out var player))
            {
                OnPlayerDespawned?.Invoke(clientId, player);

                // Return to pool or destroy
                if (player != null)
                {
                    var pool = App.Get<Pool>();
                    if (pool != null)
                    {
                        pool.DespawnDynamic(player);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(player);
                    }
                }

                _spawnedPlayers.Remove(clientId);
            }
        }

        /// <summary>
        /// Sets the spawn payload for a client (used by ConnectionManager).
        /// </summary>
        public void SetClientPayload(ulong clientId, SpawnPayload payload)
        {
            _clientPayloads[clientId] = payload;
        }

        /// <summary>
        /// Gets the spawn payload for a client.
        /// </summary>
        public bool TryGetClientPayload(ulong clientId, out SpawnPayload payload)
        {
            return _clientPayloads.TryGetValue(clientId, out payload);
        }

        #endregion

        #region Event Handlers

        private void HandleClientConnected(ulong clientId)
        {
            if (!_autoSpawnEnabled || !_networkManager.IsServer)
                return;

            // Try to extract spawn payload from connection payload
            if (_connectionManager != null)
            {
                try
                {
                    byte[] connectionPayload = _connectionManager.GetLocalPayload();
                    if (connectionPayload != null && connectionPayload.Length > 0)
                    {
                        var spawnPayload = NetworkSerializer.DeserializeValue<SpawnPayload>(connectionPayload);
                        SetClientPayload(clientId, spawnPayload);
                    }
                }
                catch
                {
                    // Payload wasn't a SpawnPayload, use defaults
                }
            }

            SpawnPlayerForClient(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _clientPayloads.Remove(clientId);
            DespawnPlayer(clientId);
        }

        #endregion
    }
}
