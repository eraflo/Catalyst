using System;
using UnityEngine;

namespace Eraflo.Catalyst.Localization
{
    /// <summary>
    /// A single key-value pair in a localization table.
    /// </summary>
    [Serializable]
    public struct LocalizationEntry
    {
        [Tooltip("The unique identifier for this string (e.g. \"hud.lantern.label\").")]
        public string Key;

        [Tooltip("The localized string for the table's language.")]
        [TextArea(1, 4)]
        public string Value;
    }
}
