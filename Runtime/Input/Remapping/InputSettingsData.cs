using System;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Core.Settings
{
    public partial class SettingsData
    {
        [Serializable]
        public class InputBinding
        {
            public string ActionId;
            public string Key; // For Legacy (e.g. "Space", "Mouse0")
            public string Path; // For New Input System (e.g. "<Keyboard>/space")
        }

        public List<InputBinding> Bindings = new List<InputBinding>();
    }
}
