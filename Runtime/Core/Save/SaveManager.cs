using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Core.Save
{
    /// <summary>
    /// Central service for managing game saves.
    /// Handles serialization, storage, and state restoration.
    /// </summary>
    [Service(Priority = 15)]
    public class SaveManager : IGameService
    {
        private ISerializer _serializer;
        private IStorageBackend _storage;
        private readonly HashSet<SaveableEntity> _entities = new HashSet<SaveableEntity>();
        private readonly Dictionary<string, SaveableEntity> _entityIndex = new Dictionary<string, SaveableEntity>();

        public ISerializer Serializer { get => _serializer; set => _serializer = value; }
        public IStorageBackend Storage { get => _storage; set => _storage = value; }

        public void Initialize()
        {
            // Default implementations if none provided
            _serializer = _serializer ?? new JsonSerializer();
            _storage = _storage ?? new LocalDiskStorage();
        }

        public void Shutdown()
        {
            _entities.Clear();
            _entityIndex.Clear();
        }

        /// <summary>Registers an entity for saving.</summary>
        public void Register(SaveableEntity entity)
        {
            _entities.Add(entity);
            _entityIndex[entity.Guid] = entity;
        }

        /// <summary>Unregisters an entity.</summary>
        public void Unregister(SaveableEntity entity)
        {
            _entities.Remove(entity);
            _entityIndex.Remove(entity.Guid);
        }

        /// <summary>Finds an entity by its unique GUID.</summary>
        public SaveableEntity GetEntity(string guid) => _entityIndex.TryGetValue(guid, out var entity) ? entity : null;

        /// <summary>
        /// Saves the current game state.
        /// Only allowed on the server/host.
        /// </summary>
        public async Task<bool> SaveGame(string saveName)
        {
            var network = App.Get<INetworkService>();
            
            // Constraint check: Only server can save
            if (network != null && !network.IsServer)
            {
                Debug.LogWarning("[SaveManager] Aborting save: Only the server can perform game saves.");
                return false;
            }

            try
            {
                var data = new GameData(saveName);

                foreach (var entity in _entities)
                {
                    data.Entities[entity.Guid] = entity.CaptureState();
                }

                byte[] serializedData = _serializer.Serialize(data);
                await _storage.SaveAsync(saveName, serializedData);

                Debug.Log($"[SaveManager] Game saved successfully: {saveName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save game '{saveName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a game state and restores entities.
        /// </summary>
        public async Task<bool> LoadGame(string saveName)
        {
            try
            {
                byte[] serializedData = await _storage.LoadAsync(saveName);
                if (serializedData == null)
                {
                    Debug.LogWarning($"[SaveManager] Save file not found: {saveName}");
                    return false;
                }

                var data = _serializer.Deserialize<GameData>(serializedData);
                var entityMap = _entities.ToDictionary(e => e.Guid);

                foreach (var kvp in data.Entities)
                {
                    if (entityMap.TryGetValue(kvp.Key, out var entity))
                    {
                        entity.RestoreState(kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveManager] Could not find entity with GUID {kvp.Key} during load.");
                    }
                }

                Debug.Log($"[SaveManager] Game loaded successfully: {saveName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load game '{saveName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves metadata for a specific save.
        /// </summary>
        public async Task<SaveMetadata> GetSaveMetadata(string saveName)
        {
            try
            {
                byte[] serializedData = await _storage.LoadAsync(saveName);
                if (serializedData == null) return null;

                if (_serializer.TryReadHeader<SaveMetadata>(serializedData, "Metadata", out var metadata))
                {
                    return metadata;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
