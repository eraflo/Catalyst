using System.Collections.Generic;

namespace Eraflo.Catalyst.InputSystem.Providers
{
    /// <summary>
    /// Implementation of IInputProvider for simulation and AI.
    /// Allows manual triggering of buttons and setting axis values.
    /// </summary>
    public class VirtualInputProvider : IInputProvider
    {
        private readonly HashSet<string> _buttonsDown = new HashSet<string>();
        private readonly Dictionary<string, float> _axes = new Dictionary<string, float>();

        /// <summary>
        /// Simulates a button press. The button will be considered "down" 
        /// until the next time it is polled by the InputManager.
        /// </summary>
        public void TriggerButton(string actionId)
        {
            lock (_buttonsDown)
            {
                _buttonsDown.Add(actionId);
            }
        }

        /// <summary>
        /// Sets the value of a virtual axis.
        /// </summary>
        public void SetAxis(string axisId, float value)
        {
            lock (_axes)
            {
                _axes[axisId] = value;
            }
        }

        public bool GetButtonDown(string actionId)
        {
            lock (_buttonsDown)
            {
                if (_buttonsDown.Contains(actionId))
                {
                    _buttonsDown.Remove(actionId);
                    return true;
                }
                return false;
            }
        }

        public float GetAxis(string axisId)
        {
            lock (_axes)
            {
                return _axes.TryGetValue(axisId, out var value) ? value : 0f;
            }
        }

        public void Vibrate(float intensity, float duration)
        {
            // Virtual vibration
        }
    }
}
