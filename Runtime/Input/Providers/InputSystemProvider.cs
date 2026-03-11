using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

#if UNITY_INPUT_SYSTEM
namespace Eraflo.Catalyst.InputSystem.Providers
{
    /// <summary>
    /// Implementation of IInputProvider using Unity's New Input System.
    /// This provider typically expects an InputActionAsset to be configured.
    /// </summary>
    public class InputSystemProvider : IInputProvider
    {
        private InputActionAsset _actionAsset;
        private readonly Dictionary<string, UnityEngine.InputSystem.InputAction> _actionCache = new Dictionary<string, UnityEngine.InputSystem.InputAction>();

        public InputSystemProvider(InputActionAsset actionAsset)
        {
            _actionAsset = actionAsset;
            if (_actionAsset != null)
            {
                _actionAsset.Enable();
            }
        }

        public bool GetButtonDown(string actionId)
        {
            if (_actionAsset == null) return false;

            var action = GetCachedAction(actionId);
            return action != null && action.WasPressedThisFrame();
        }

        public float GetAxis(string axisId)
        {
            if (_actionAsset == null) return 0f;

            var action = GetCachedAction(axisId);
            if (action == null) return 0f;

            return action.ReadValue<float>();
        }

        public void Vibrate(float intensity, float duration)
        {
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(intensity, intensity);
            }
        }

        /// <summary>
        /// Updates the action asset if needed.
        /// </summary>
        public void SetActionAsset(InputActionAsset asset)
        {
            if (_actionAsset != null)
                _actionAsset.Disable();

            _actionAsset = asset;
            _actionCache.Clear();

            if (_actionAsset != null)
                _actionAsset.Enable();
        }

        private UnityEngine.InputSystem.InputAction GetCachedAction(string actionId)
        {
            if (!_actionCache.TryGetValue(actionId, out var action))
            {
                action = _actionAsset?.FindAction(actionId);
                if (action != null) _actionCache[actionId] = action;
            }
            return action;
        }
    }
}
#endif

