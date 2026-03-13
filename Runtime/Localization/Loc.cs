using UnityEngine;

namespace Eraflo.Catalyst.Localization
{
    /// <summary>
    /// Static helper for concise localized string lookups from anywhere in game code.
    ///
    /// Falls back to the <c>fallback</c> parameter (or the raw key) when the
    /// LocalizationService is unavailable or has no entry for the key — safe
    /// to call before locale tables are loaded.
    ///
    /// Examples:
    /// <code>
    ///   label.text = Loc.Get("hud.time.label", "Time");
    ///   label.text = Loc.Get("report.confirm", $"Confirm — {count} report(s)");
    /// </code>
    /// </summary>
    public static class Loc
    {
        /// <summary>Returns the localized string for <paramref name="key"/>.</summary>
        public static string Get(string key, string fallback = null)
        {
            var svc = App.Get<LocalizationService>();
            return svc != null ? svc.Get(key, fallback) : fallback ?? key;
        }

        /// <summary>
        /// Switches the active locale. No-op if no table is registered for the language.
        /// </summary>
        public static void SetLanguage(SystemLanguage language)
            => App.Get<LocalizationService>()?.SetLanguage(language);

        /// <summary>Currently active language (English if service is not initialized).</summary>
        public static SystemLanguage CurrentLanguage
            => App.Get<LocalizationService>()?.CurrentLanguage ?? SystemLanguage.English;
    }
}
