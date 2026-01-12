using UnityEngine;
using System.Collections.Generic;
using Eraflo.Catalyst.InputSystem.Combos;

namespace Eraflo.Catalyst.InputSystem.Debugging
{
    /// <summary>
    /// Debug overlay that visualizes the input buffer and combo system state.
    /// Auto-instantiates if EnableInputDebugger is true in PackageSettings.
    /// </summary>
    public class InputDebugOverlay : MonoBehaviour
    {
        private InputManager _inputManager;
        private ComboSystem _comboSystem; // Optional, can be multiple

        private void Start()
        {
            _inputManager = App.Get<InputManager>();
            // Combo systems are often localized, we might need a way to track them.
            // For now, we'll look for any active one or just show the buffer.
        }

        private void OnGUI()
        {
            if (!PackageSettings.Instance.EnableInputDebugger) return;
            if (_inputManager == null) return;

            float x = 20;
            float y = 50;
            float width = 300;
            
            GUI.Box(new Rect(x, y, width, 200), "Input System Debug");
            
            y += 30;
            GUI.Label(new Rect(x + 10, y, width - 20, 20), $"Buffer Count: {_inputManager.GetBuffer().Count}");
            
            y += 20;
            GUI.Label(new Rect(x + 10, y, width - 20, 20), $"Last Device: (Searching...)");
            y += 20;
            var buffer = _inputManager.GetBuffer();
            for (int i = Mathf.Max(0, buffer.Count - 8); i < buffer.Count; i++)
            {
                var input = buffer[i];
                string status = input.IsConsumed ? "[X]" : "[ ]";
                GUI.Label(new Rect(x + 10, y, width - 20, 20), $"{status} {input.ActionId} ({input.Timestamp:F2}s)");
                y += 20;
            }

            // Draw a separator
            y += 5;
            GUI.Box(new Rect(x + 10, y, width - 20, 1), "");
            y += 10;
            
            GUI.Label(new Rect(x + 10, y, width - 20, 20), "Active Combos: (Searching...)");
        }
    }
}
