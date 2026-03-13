using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Localization
{
    /// <summary>
    /// ScriptableObject holding all localized strings for one language.
    /// Place under Resources/Localization/ and register with LocalizationService at startup.
    ///
    /// Create via: Assets > Create > Catalyst > Localization > Localization Table
    /// </summary>
    [CreateAssetMenu(menuName = "Catalyst/Localization/Localization Table", fileName = "LocalizationTable_en")]
    public class LocalizationTable : ScriptableObject
    {
        [Tooltip("Language this table covers.")]
        public SystemLanguage Language = SystemLanguage.English;

        [SerializeField]
        private List<LocalizationEntry> _entries = new();

        // Runtime lookup built on first access
        private Dictionary<string, string> _lookup;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the localized string for <paramref name="key"/>, or
        /// <paramref name="fallback"/> if the key is missing.
        /// </summary>
        public string Get(string key, string fallback = null)
        {
            EnsureBuilt();
            return _lookup.TryGetValue(key, out var value) ? value : fallback ?? key;
        }

        /// <summary>Returns true and the value if the key exists in this table.</summary>
        public bool TryGet(string key, out string value)
        {
            EnsureBuilt();
            return _lookup.TryGetValue(key, out value);
        }

        /// <summary>
        /// Adds or overwrites an entry at runtime (e.g. dynamically loaded DLC text).
        /// </summary>
        public void Set(string key, string value)
        {
            EnsureBuilt();
            _lookup[key] = value;
        }

        /// <summary>Forces a rebuild of the internal dictionary from the serialized list.</summary>
        public void Rebuild()
        {
            _lookup = new Dictionary<string, string>(_entries.Count, StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;
                _lookup[entry.Key] = entry.Value ?? string.Empty;
            }
        }

        // ─── Editor helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Exposes the raw entry list so editor tools can bulk-import CSV/JSON.
        /// Returns a copy — call Rebuild() to apply changes.
        /// </summary>
        public List<LocalizationEntry> GetEntries() => new(_entries);

        /// <summary>Replaces all entries (editor import only). Calls Rebuild() automatically.</summary>
        public void SetEntries(List<LocalizationEntry> entries)
        {
            _entries = new List<LocalizationEntry>(entries);
            Rebuild();
        }

        // ─── Private ──────────────────────────────────────────────────────────

        private void EnsureBuilt()
        {
            if (_lookup == null) Rebuild();
        }

        private void OnEnable()
        {
            // Invalidate dictionary when the asset is reloaded in the editor
            _lookup = null;
        }
    }
}
