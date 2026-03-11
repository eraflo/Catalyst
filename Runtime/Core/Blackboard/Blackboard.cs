using System;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.Core.Save;
using Newtonsoft.Json;

namespace Eraflo.Catalyst.Core.Blackboard
{
    /// <summary>
    /// A hierarchy-aware shared data container.
    /// Searches in parent if a key is not found locally.
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
            [NonSerialized] public bool IsJsonDirty;
        }
        
        [SerializeField] private List<BlackboardEntry> _entries = new();

        // O(1) lookup by key into _entries (kept in sync with _entries)
        private readonly Dictionary<string, BlackboardEntry> _entryIndex = new();

        private readonly Dictionary<string, object> _runtimeData = new();
        private readonly object _lock = new();
        private Blackboard _parent;
        
        /// <summary>
        /// Triggered when a value is changed in the blackboard.
        /// (object oldValue, object newValue)
        /// </summary>
        public Action<string, object, object> OnValueChanged;
        
        private readonly Dictionary<string, Action<object, object>> _keyListeners = new();
        
        private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

        private bool _initialized = false;

        public void OnBeforeSerialize()
        {
            // Lazily serialize only entries that were mutated since the last serialize
            foreach (var entry in _entries)
            {
                if (entry.IsJsonDirty && entry.CachedValue != null)
                {
                    try
                    {
                        entry.JsonValue = JsonConvert.SerializeObject(entry.CachedValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Blackboard] Failed to serialize '{entry.Key}': {e.Message}");
                    }
                    entry.IsJsonDirty = false;
                }
            }
        }
        public void OnAfterDeserialize() => _initialized = false;

        public void SetParent(Blackboard parent)
        {
            _parent = parent;
        }

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
            _entryIndex.Clear();
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;

                _entryIndex[entry.Key] = entry;

                object value = null;
                if (!string.IsNullOrEmpty(entry.TypeName))
                {
                    var type = Type.GetType(entry.TypeName);
                    if (type == null)
                    {
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
                }

                if (value != null)
                {
                    _runtimeData[entry.Key] = value;
                }
            }
        }
        
        public void Set<T>(string key, T value)
        {
            EnsureInitialized();
            object oldValue = null;

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _runtimeData.TryGetValue(key, out oldValue);
                    _runtimeData[key] = value;
                    SyncEntry(key, value);
                }
            }
            else
            {
                _runtimeData.TryGetValue(key, out oldValue);
                _runtimeData[key] = value;
                SyncEntry(key, value);
            }

            OnValueChanged?.Invoke(key, oldValue, value);

            if (_keyListeners.TryGetValue(key, out var action))
            {
                action?.Invoke(oldValue, value);
            }
        }

        private void SyncEntry(string key, object value)
        {
            // O(1) lookup via _entryIndex instead of O(n) _entries.Find()
            if (!_entryIndex.TryGetValue(key, out var entry))
            {
                entry = new BlackboardEntry { Key = key };
                _entries.Add(entry);
                _entryIndex[key] = entry;
            }

            if (value != null)
            {
                entry.TypeName = value.GetType().AssemblyQualifiedName;
                // JSON serialization is deferred to OnBeforeSerialize (lazy)
                entry.IsJsonDirty = true;
            }
            else
            {
                entry.TypeName = null;
                entry.JsonValue = null;
                entry.IsJsonDirty = false;
            }

            entry.CachedValue = value;
            entry.IsCached = true;
        }
        
        public T Get<T>(string key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return default;
        }
        
        public bool TryGet<T>(string key, out T value)
        {
            EnsureInitialized();
            object obj;
            
            bool foundLocal = false;
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    foundLocal = _runtimeData.TryGetValue(key, out obj);
                }
            }
            else
            {
                foundLocal = _runtimeData.TryGetValue(key, out obj);
            }

            if (foundLocal)
            {
                if (obj is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }
            else if (_parent != null)
            {
                return _parent.TryGet<T>(key, out value);
            }
            
            value = default;
            return false;
        }
        
        public bool Contains(string key)
        {
            EnsureInitialized();
            bool hasLocal = false;
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    hasLocal = _runtimeData.ContainsKey(key);
                }
            }
            else
            {
                hasLocal = _runtimeData.ContainsKey(key);
            }

            if (hasLocal) return true;
            return _parent != null && _parent.Contains(key);
        }
        
        public bool Remove(string key)
        {
            EnsureInitialized();
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _entries.RemoveAll(e => e.Key == key);
                    _entryIndex.Remove(key);
                    return _runtimeData.Remove(key);
                }
            }
            _entries.RemoveAll(e => e.Key == key);
            _entryIndex.Remove(key);
            return _runtimeData.Remove(key);
        }

        public void Clear()
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _runtimeData.Clear();
                    _entries.Clear();
                    _entryIndex.Clear();
                }
            }
            else
            {
                _runtimeData.Clear();
                _entries.Clear();
                _entryIndex.Clear();
            }
        }

        /// <summary>
        /// Gets all local keys in the blackboard.
        /// </summary>
        public List<string> GetAllKeys()
        {
            EnsureInitialized();
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    return new List<string>(_runtimeData.Keys);
                }
            }
            return new List<string>(_runtimeData.Keys);
        }

        /// <summary>
        /// Gets a dictionary of all keys and their associated types.
        /// </summary>
        public Dictionary<string, Type> GetKeysAndTypes()
        {
            EnsureInitialized();
            var dict = new Dictionary<string, Type>();
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    foreach (var kvp in _runtimeData)
                    {
                        dict[kvp.Key] = kvp.Value?.GetType();
                    }
                }
            }
            else
            {
                foreach (var kvp in _runtimeData)
                {
                    dict[kvp.Key] = kvp.Value?.GetType();
                }
            }
            return dict;
        }

        /// <summary>
        /// Renames a key while preserving its value.
        /// </summary>
        public void Rename(string oldKey, string newKey)
        {
            if (oldKey == newKey) return;
            EnsureInitialized();

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (_runtimeData.TryGetValue(oldKey, out var value))
                    {
                        _runtimeData.Remove(oldKey);
                        _runtimeData[newKey] = value;

                        // O(1) lookup via _entryIndex instead of O(n) _entries.Find()
                        if (_entryIndex.TryGetValue(oldKey, out var entry))
                        {
                            _entryIndex.Remove(oldKey);
                            entry.Key = newKey;
                            _entryIndex[newKey] = entry;
                        }

                        OnValueChanged?.Invoke(oldKey, value, null);
                        OnValueChanged?.Invoke(newKey, null, value);
                    }
                }
            }
            else
            {
                if (_runtimeData.TryGetValue(oldKey, out var value))
                {
                    _runtimeData.Remove(oldKey);
                    _runtimeData[newKey] = value;

                    // O(1) lookup via _entryIndex instead of O(n) _entries.Find()
                    if (_entryIndex.TryGetValue(oldKey, out var entry))
                    {
                        _entryIndex.Remove(oldKey);
                        entry.Key = newKey;
                        _entryIndex[newKey] = entry;
                    }

                    OnValueChanged?.Invoke(oldKey, value, null);
                    OnValueChanged?.Invoke(newKey, null, value);
                }
            }
        }

        /// <summary>
        /// Creates a deep clone of the blackboard's entries.
        /// </summary>
        public Blackboard Clone()
        {
            var clone = new Blackboard();
            clone._parent = _parent;
            foreach (var entry in _entries)
            {
                clone._entries.Add(new BlackboardEntry
                {
                    Key = entry.Key,
                    TypeName = entry.TypeName,
                    JsonValue = entry.JsonValue
                });
            }
            return clone;
        }

        /// <summary>
        /// Restores entries from a list of BlackboardEntry.
        /// </summary>
        public void RestoreEntries(List<BlackboardEntry> entries)
        {
            if (entries == null) return;

            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _entries = entries;
                    _entryIndex.Clear();
                    foreach (var e in _entries)
                    {
                        if (!string.IsNullOrEmpty(e.Key))
                            _entryIndex[e.Key] = e;
                    }
                    _initialized = false;
                    EnsureInitialized();
                }
            }
            else
            {
                _entries = entries;
                _entryIndex.Clear();
                foreach (var e in _entries)
                {
                    if (!string.IsNullOrEmpty(e.Key))
                        _entryIndex[e.Key] = e;
                }
                _initialized = false;
                EnsureInitialized();
            }
        }

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

        // internal for Save System
        internal List<BlackboardEntry> GetEntries()
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    return CopyEntries();
                }
            }
            return CopyEntries();
        }

        private List<BlackboardEntry> CopyEntries()
        {
            var copy = new List<BlackboardEntry>(_entries.Count);
            foreach (var entry in _entries)
            {
                copy.Add(new BlackboardEntry
                {
                    Key = entry.Key,
                    TypeName = entry.TypeName,
                    JsonValue = entry.JsonValue
                });
            }
            return copy;
        }
    }
}
