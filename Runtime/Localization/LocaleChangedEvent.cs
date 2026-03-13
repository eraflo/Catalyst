using UnityEngine;

namespace Eraflo.Catalyst.Localization
{
    /// <summary>
    /// Published on the EventBus when the active locale changes.
    /// UI components should subscribe to refresh displayed text.
    /// </summary>
    public struct LocaleChangedEvent
    {
        public SystemLanguage PreviousLanguage;
        public SystemLanguage NewLanguage;
    }
}
