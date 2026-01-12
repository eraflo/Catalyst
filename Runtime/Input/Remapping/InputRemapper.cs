using System.Collections.Generic;
using System.Linq;
using Eraflo.Catalyst.Core.Settings;
using UnityEngine;
using System.Threading.Tasks;

namespace Eraflo.Catalyst.InputSystem.Remapping
{
    /// <summary>
    /// Service for managing input remapping and persistence.
    /// </summary>
    [Service(Priority = 40)]
    public class InputRemapper : IGameService
    {
        private SettingsManager _settingsManager;
        private readonly Dictionary<string, string> _legacyBindings = new Dictionary<string, string>();

        public void Initialize()
        {
            _settingsManager = App.Get<SettingsManager>();
            LoadBindings();
        }

        public void Shutdown() { }

        public void LoadBindings()
        {
            if (_settingsManager?.Data == null) return;

            _legacyBindings.Clear();
            foreach (var binding in _settingsManager.Data.Bindings)
            {
                if (!string.IsNullOrEmpty(binding.ActionId))
                {
                    _legacyBindings[binding.ActionId] = binding.Key;
                }
            }
        }

        public string GetLegacyBinding(string actionId, string defaultKey)
        {
            return _legacyBindings.TryGetValue(actionId, out var key) ? key : defaultKey;
        }

        public void RemapLegacy(string actionId, string newKey)
        {
            _legacyBindings[actionId] = newKey;
            
            var binding = _settingsManager.Data.Bindings.FirstOrDefault(b => b.ActionId == actionId);
            if (binding == null)
            {
                binding = new SettingsData.InputBinding { ActionId = actionId };
                _settingsManager.Data.Bindings.Add(binding);
            }
            binding.Key = newKey;
            
            _ = _settingsManager.SaveAsync();
        }
    }
}
