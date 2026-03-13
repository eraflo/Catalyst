using System.Collections.Generic;
using Eraflo.Catalyst.Events;
using UnityEngine;

namespace Eraflo.Catalyst.Localization
{
    /// <summary>
    /// Manages the active locale and provides localized string lookups.
    ///
    /// Usage:
    ///   // Register your table(s) (e.g. from a bootstrap MonoBehaviour):
    ///   App.Get&lt;LocalizationService&gt;()?.RegisterTable(myEnglishTable);
    ///
    ///   // Retrieve a string (prefer the Loc static helper):
    ///   string label = App.Get&lt;LocalizationService&gt;()?.Get("hud.lantern.label", "Lantern");
    ///
    /// Language switching fires LocaleChangedEvent on the EventBus so UI components
    /// can refresh without polling.
    /// </summary>
    [Service(Priority = 5)]
    public class LocalizationService : IGameService
    {
        // ─── State ───────────────────────────────────────────────────────────

        private readonly Dictionary<SystemLanguage, LocalizationTable> _tables = new();
        private LocalizationTable _current;
        private EventBus _eventBus;

        // ─── Properties ──────────────────────────────────────────────────────

        /// <summary>Currently active language.</summary>
        public SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.English;

        // ─── IGameService ─────────────────────────────────────────────────────

        public void Initialize()
        {
            _eventBus = App.Get<EventBus>();

            // Auto-load all LocalizationTable assets placed in Resources/Localization/
            var tables = Resources.LoadAll<LocalizationTable>("Localization");
            foreach (var t in tables)
                RegisterTable(t);

            // Apply the system language if a table for it exists, otherwise stay English
            var system = Application.systemLanguage;
            if (_tables.ContainsKey(system))
                ApplyLanguage(system);
            else if (_tables.ContainsKey(SystemLanguage.English))
                ApplyLanguage(SystemLanguage.English);
        }

        public void Shutdown()
        {
            _tables.Clear();
            _current = null;
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Registers a LocalizationTable. Overwrites any previously registered table
        /// for the same language. Safe to call before Initialize().
        /// </summary>
        public void RegisterTable(LocalizationTable table)
        {
            if (table == null) return;
            _tables[table.Language] = table;

            // If this is the active language, refresh the current reference
            if (table.Language == CurrentLanguage)
                _current = table;
        }

        /// <summary>
        /// Switches the active locale and fires LocaleChangedEvent.
        /// No-op if no table is registered for <paramref name="language"/>.
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            if (!_tables.ContainsKey(language))
            {
                Debug.LogWarning($"[LocalizationService] No table registered for {language}.");
                return;
            }

            ApplyLanguage(language);
        }

        /// <summary>
        /// Returns the localized string for <paramref name="key"/> in the active language.
        /// Falls back to <paramref name="fallback"/> (or the key itself) if not found.
        /// </summary>
        public string Get(string key, string fallback = null)
        {
            if (_current != null && _current.TryGet(key, out var value))
                return value;

            return fallback ?? key;
        }

        /// <summary>Returns all registered languages.</summary>
        public IEnumerable<SystemLanguage> RegisteredLanguages => _tables.Keys;

        // ─── Private ─────────────────────────────────────────────────────────

        private void ApplyLanguage(SystemLanguage language)
        {
            var previous = CurrentLanguage;
            CurrentLanguage = language;
            _current = _tables[language];

            _eventBus?.Publish(new LocaleChangedEvent
            {
                PreviousLanguage = previous,
                NewLanguage      = language
            });
        }
    }
}
