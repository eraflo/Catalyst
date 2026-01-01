using System;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst;
using Newtonsoft.Json;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// A shared data container for passing information between nodes in a behaviour tree.
    /// Thread-safe when PackageRuntime.IsThreadSafe is enabled.
    /// </summary>
    [Serializable]
    public class Blackboard : ISerializationCallbackReceiver
    {
        [Serializable]
        public class BlackboardEntry
        {
            public string Key;
            public string TypeName;
            public string JsonValue;
            
            [NonSerialized] public object CachedValue;
            [NonSerialized] public bool IsCached;
        }
        
        [SerializeField] private List<BlackboardEntry> _entries = new();
        
        private readonly Dictionary<string, object> _runtimeData = new();
        private readonly object _lock = new();
        
        /// <summary>
        /// Triggered when a value is changed in the blackboard.
        /// (object oldValue, object newValue)
        /// </summary>
        public Action<string, object, object> OnValueChanged;
        
        private readonly Dictionary<string, Action<object, object>> _keyListeners = new();
        
        private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

        private bool _initialized = false;
        
        // OPTIMIZATION: Reusable buffer for GetAllKeys to reduce allocations
        [NonSerialized] private List<string> _keysBuffer;

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() => _initialized = false;

        private void EnsureInitialized()
        {
            if (_initialized) return;
            
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    PerformInitialization();
                }
            }
            else
            {
                PerformInitialization();
            }
        }

        private void PerformInitialization()
        {
            if (_initialized) return;
            _initialized = true;

            _runtimeData.Clear();
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;

                object value = null;
                if (!string.IsNullOrEmpty(entry.TypeName))
                {
                    var type = Type.GetType(entry.TypeName);
                    if (type == null)
                    {
                        // Fallback: search all assemblies
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = assembly.GetType(entry.TypeName);
                            if (type != null) break;
                        }
                    }

                    if (type != null)
                    {
                        try {
                            value = JsonConvert.DeserializeObject(entry.JsonValue, type);
                        } catch (Exception e) {
                            Debug.LogWarning($"[Blackboard] Failed to deserialize '{entry.Key}': {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Blackboard] Could not find type '{entry.TypeName}' for key '{entry.Key}'");
                    }
                }

                if (value != null)
                {
                    _runtimeData[entry.Key] = value;
                }
            }
        }
        
        /// <summary>
        /// Sets a value in the blackboard.
        /// </summary>
        public void Set<T>(string key, T value)
        {
            EnsureInitialized();
            object oldValue = null;
            bool found = false;

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    found = _runtimeData.TryGetValue(key, out oldValue);
                    _runtimeData[key] = value;
                    SyncEntry(key, value);
                }
            }
            else
            {
                found = _runtimeData.TryGetValue(key, out oldValue);
                _runtimeData[key] = value;
                SyncEntry(key, value);
            }

            // Trigger global event
            OnValueChanged?.Invoke(key, oldValue, value);

            // Trigger specific key event
            if (_keyListeners.TryGetValue(key, out var action))
            {
                action?.Invoke(oldValue, value);
            }
        }

        private void SyncEntry(string key, object value)
        {
            var entry = _entries.Find(e => e.Key == key);
            if (entry == null)
            {
                entry = new BlackboardEntry { Key = key };
                _entries.Add(entry);
            }
            
            if (value != null)
            {
                entry.TypeName = value.GetType().AssemblyQualifiedName;
                entry.JsonValue = JsonConvert.SerializeObject(value);
            }
            else
            {
                entry.TypeName = null;
                entry.JsonValue = null;
            }
            
            entry.CachedValue = value;
            entry.IsCached = true;
        }
        
        /// <summary>
        /// Gets a value from the blackboard.
        /// </summary>
        public T Get<T>(string key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return default;
        }
        
        /// <summary>
        /// Tries to get a value from the blackboard.
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            EnsureInitialized();
            object obj;
            
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (!_runtimeData.TryGetValue(key, out obj))
                    {
                        value = default;
                        return false;
                    }
                }
            }
            else
            {
                if (!_runtimeData.TryGetValue(key, out obj))
                {
                    value = default;
                    return false;
                }
            }
            
            if (obj is T typedValue)
            {
                value = typedValue;
                return true;
            }
            
            value = default;
            return false;
        }
        
        /// <summary>
        /// Checks if a key exists in the blackboard.
        /// </summary>
        public bool Contains(string key)
        {
            EnsureInitialized();
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    return _runtimeData.ContainsKey(key);
                }
            }
            return _runtimeData.ContainsKey(key);
        }
        
        /// <summary>
        /// Removes a key from the blackboard.
        /// </summary>
        public bool Remove(string key)
        {
            EnsureInitialized();
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _entries.RemoveAll(e => e.Key == key);
                    return _runtimeData.Remove(key);
                }
            }
            _entries.RemoveAll(e => e.Key == key);
            return _runtimeData.Remove(key);
        }

        /// <summary>
        /// Renames an existing key in the blackboard.
        /// </summary>
        public void Rename(string oldKey, string newKey)
        {
            if (string.IsNullOrEmpty(newKey) || oldKey == newKey) return;
            if (Contains(newKey)) return;

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    PerformRename(oldKey, newKey);
                }
            }
            else
            {
                PerformRename(oldKey, newKey);
            }
        }

        private void PerformRename(string oldKey, string newKey)
        {
            var entry = _entries.Find(e => e.Key == oldKey);
            if (entry != null) entry.Key = newKey;

            if (_runtimeData.TryGetValue(oldKey, out var value))
            {
                _runtimeData.Remove(oldKey);
                _runtimeData[newKey] = value;
                
                // Trigger events for the new key as a "change" from null to value
                OnValueChanged?.Invoke(newKey, null, value);
                if (_keyListeners.TryGetValue(newKey, out var action))
                {
                    action?.Invoke(null, value);
                }
            }
        }
        
        /// <summary>
        /// Clears all data from the blackboard.
        /// </summary>
        public void Clear()
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _runtimeData.Clear();
                    _entries.Clear();
                }
            }
            else
            {
                _runtimeData.Clear();
                _entries.Clear();
            }
        }
        
        /// <summary>
        /// Gets a snapshot of all keys in the blackboard.
        /// </summary>
        public string[] GetAllKeys()
        {
            EnsureInitialized();
            
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    return GetAllKeysInternal();
                }
            }
            return GetAllKeysInternal();
        }
        
        private string[] GetAllKeysInternal()
        {
            var keys = new string[_runtimeData.Count];
            _runtimeData.Keys.CopyTo(keys, 0);
            return keys;
        }
        
        /// <summary>
        /// Gets the keys collection directly for iteration without allocation.
        /// WARNING: Not thread-safe, use only when IsThreadSafe is false.
        /// </summary>
        public IReadOnlyCollection<string> GetKeysNoAlloc()
        {
            EnsureInitialized();
            return _runtimeData.Keys;
        }

        /// <summary>
        /// Gets all keys and their value types.
        /// </summary>
        public Dictionary<string, Type> GetKeysAndTypes()
        {
            EnsureInitialized();
            var result = new Dictionary<string, Type>();
            
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    foreach (var kvp in _runtimeData)
                    {
                        result[kvp.Key] = kvp.Value?.GetType();
                    }
                }
            }
            else
            {
                foreach (var kvp in _runtimeData)
                {
                    result[kvp.Key] = kvp.Value?.GetType();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Registers a listener for a specific key.
        /// (object oldValue, object newValue)
        /// </summary>
        public void RegisterListener(string key, Action<object, object> callback)
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (!_keyListeners.ContainsKey(key)) _keyListeners[key] = null;
                    _keyListeners[key] += callback;
                }
            }
            else
            {
                if (!_keyListeners.ContainsKey(key)) _keyListeners[key] = null;
                _keyListeners[key] += callback;
            }
        }

        /// <summary>
        /// Unregisters a listener from a specific key.
        /// </summary>
        public void UnregisterListener(string key, Action<object, object> callback)
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (_keyListeners.ContainsKey(key))
                    {
                        _keyListeners[key] -= callback;
                        if (_keyListeners[key] == null) _keyListeners.Remove(key);
                    }
                }
            }
            else
            {
                if (_keyListeners.ContainsKey(key))
                {
                    _keyListeners[key] -= callback;
                    if (_keyListeners[key] == null) _keyListeners.Remove(key);
                }
            }
        }

        /// <summary>
        /// Creates a copy of this blackboard.
        /// </summary>
        public Blackboard Clone()
        {
            EnsureInitialized();
            var clone = new Blackboard();
            
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    foreach (var kvp in _runtimeData)
                    {
                        clone.Set(kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                foreach (var kvp in _runtimeData)
                {
                    clone.Set(kvp.Key, kvp.Value);
                }
            }
            
            return clone;
        }
    }
}

