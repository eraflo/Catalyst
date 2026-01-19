using System.Collections.Generic;
using UnityEngine;

#if ENABLE_LEGACY_INPUT_MANAGER
namespace Eraflo.Catalyst.InputSystem.Providers
{
    /// <summary>
    /// Implementation of IInputProvider using Unity's Legacy Input system.
    /// Supports remapping via InputRemapper.
    /// </summary>
    public class LegacyInputProvider : IInputProvider
    {
        private readonly Dictionary<string, KeyCode> _buttonMappings = new Dictionary<string, KeyCode>();
        private readonly Dictionary<string, string> _axisMappings = new Dictionary<string, string>();

        public void MapButton(string actionId, KeyCode keyCode)
        {
            _buttonMappings[actionId] = keyCode;
        }

        public void MapAxis(string actionId, string axisName)
        {
            _axisMappings[actionId] = axisName;
        }

        public bool GetButtonDown(string actionId)
        {
            var remapper = App.Get<Remapping.InputRemapper>();

            // 1. Check remapper for dynamic bindings
            // If remapper returns default actionId, we use our local mappings
            var binding = remapper?.GetLegacyBinding(actionId, null);
            if (!string.IsNullOrEmpty(binding))
            {
                if (System.Enum.TryParse<KeyCode>(binding, out var keyCode))
                {
                    return UnityEngine.Input.GetKeyDown(keyCode);
                }
            }

            // 2. Fallback to local hardcoded mappings
            if (_buttonMappings.TryGetValue(actionId, out var mappedKey))
            {
                if (UnityEngine.Input.GetKeyDown(mappedKey))
                    return true;
            }

            // 3. Fallback to raw string (e.g. "Space")
            try
            {
                return UnityEngine.Input.GetButtonDown(actionId);
            }
            catch
            {
                return false;
            }
        }

        public float GetAxis(string axisId)
        {
            if (_axisMappings.TryGetValue(axisId, out var axisName))
            {
                return UnityEngine.Input.GetAxis(axisName);
            }
            return UnityEngine.Input.GetAxis(axisId);
        }

        public void Vibrate(float intensity, float duration)
        {
            // Implementation handled in InputManager for now
        }
    }
}
#endif

