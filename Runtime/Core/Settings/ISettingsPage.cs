using System.Collections.Generic;

namespace Eraflo.Catalyst.Core.Settings
{
    /// <summary>
    /// Interface for modular settings pages (Categories).
    /// Each page handles a subset of the SettingsData.
    /// </summary>
    public interface ISettingsPage
    {
        /// <summary>
        /// Unique identifier for the page.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable display name for the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Lists all the setting keys managed by this page.
        /// Useful for the custom editor.
        /// </summary>
        IEnumerable<string> GetSettingKeys();

        /// <summary>
        /// Applies the settings from the data package to the engine.
        /// </summary>
        void Apply(SettingsData data);
    }
}
