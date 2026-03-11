using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.InputSystem.Network;
using UnityEngine;

namespace Eraflo.Catalyst.InputSystem
{
    /// <summary>
    /// Represents a buffered input action.
    /// </summary>
    public struct BufferedInput
    {
        public string ActionId;
        public float Timestamp;
        public bool IsConsumed;
    }

    /// <summary>
    /// Event data for newly buffered inputs.
    /// </summary>
    public struct InputBufferedEvent
    {
        public string ActionId;
        public float Timestamp;
        public InputDeviceType DeviceType;
    }

    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad,
        Virtual
    }

    /// <summary>
    /// Service that manages input buffering and consumption.
    /// </summary>
    [Service(Priority = 50)]
    public class InputManager : IGameService, IUpdatable
    {
        private static readonly string[] _joystickButtonNames = new string[]
        {
            "joystick button 0",  "joystick button 1",  "joystick button 2",  "joystick button 3",
            "joystick button 4",  "joystick button 5",  "joystick button 6",  "joystick button 7",
            "joystick button 8",  "joystick button 9",  "joystick button 10", "joystick button 11",
            "joystick button 12", "joystick button 13", "joystick button 14", "joystick button 15",
            "joystick button 16", "joystick button 17", "joystick button 18", "joystick button 19"
        };

        private readonly List<BufferedInput> _buffer = new List<BufferedInput>();
        private readonly List<string> _registeredActions = new List<string>();
        private readonly Predicate<BufferedInput> _cleanupPred;
        private IInputProvider _provider;
        private ChronosManager _chronos;
        private float _currentTime;

        public InputManager()
        {
            _cleanupPred = frame => _currentTime - frame.Timestamp > BufferDuration;
        }

        // Haptics state
        private float _vibrationEndTime;

        public event Action<InputBufferedEvent> OnInputBuffered;

        /// <summary>
        /// Duration (in seconds) that an input remains valid in the buffer.
        /// </summary>
        public float BufferDuration { get; set; } = 0.2f;

        public void Initialize()
        {
            _chronos = App.Get<ChronosManager>();

            // Initialize provider from settings if not already set
            if (_provider == null)
            {
                var settings = PackageSettings.Instance;
                switch (settings.InputProvider)
                {
                    case InputProviderType.Legacy:
#if ENABLE_LEGACY_INPUT_MANAGER
                        _provider = new Providers.LegacyInputProvider();
#endif
                        break;
                    case InputProviderType.InputSystem:
#if UNITY_INPUT_SYSTEM
                        _provider = new Providers.InputSystemProvider(settings.InputActionAsset);
#endif
                        break;
                    default:
#if ENABLE_LEGACY_INPUT_MANAGER
                        _provider = new Providers.LegacyInputProvider();
#endif
                        break;
                }
            }

            // Spawn Debug Overlay if enabled and in Play mode
            if (Application.isPlaying && PackageSettings.Instance.EnableInputDebugger)
            {
                var go = new GameObject("InputDebugOverlay");
                go.AddComponent<Debugging.InputDebugOverlay>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
        }

        public void Shutdown()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// Sets the input provider.
        /// </summary>
        public void SetProvider(IInputProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Registers an action ID to be tracked by the buffer.
        /// </summary>
        public void RegisterAction(string actionId)
        {
            if (!_registeredActions.Contains(actionId))
            {
                _registeredActions.Add(actionId);
            }
        }

        /// <summary>
        /// Polls raw inputs and updates the buffer.
        /// </summary>
        public void OnUpdate()
        {
            // Use Chronos for time accumulation
            float dt = _chronos != null ? _chronos.GetDeltaTime(ChronosManager.DefaultChannel) : Time.unscaledDeltaTime;
            _currentTime += dt;

            if (_provider != null)
            {
                foreach (var action in _registeredActions)
                {
                    if (_provider.GetButtonDown(action))
                    {
                        var evt = new InputBufferedEvent
                        {
                            ActionId = action,
                            Timestamp = _currentTime,
                            DeviceType = DetectDeviceType()
                        };
                        _buffer.Add(new BufferedInput
                        {
                            ActionId = action,
                            Timestamp = _currentTime,
                            IsConsumed = false
                        });
                        OnInputBuffered?.Invoke(evt);
                    }
                }
            }

            // 2. Purge old inputs
            _buffer.RemoveAll(_cleanupPred);
        }

        /// <summary>
        /// Tries to consume an action from the buffer.
        /// </summary>
        /// <param name="actionId">The action to consume.</param>
        /// <returns>True if the action was available and consumed.</returns>
        public bool TryConsumeAction(string actionId)
        {
            // Search from oldest to newest (index 0 to Count-1) for FIFO
            for (int i = 0; i < _buffer.Count; i++)
            {
                var input = _buffer[i];
                if (input.ActionId == actionId && !input.IsConsumed)
                {
                    // Check if still valid
                    if (_currentTime - input.Timestamp <= BufferDuration)
                    {
                        input.IsConsumed = true;
                        _buffer[i] = input;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to consume an action, waiting if it's not immediately available.
        /// Useful for "input tolerance" where the player might be slightly frame-late.
        /// </summary>
        public async Task<bool> TryConsumeActionAsync(string actionId, float timeoutSeconds, CancellationToken ct = default)
        {
            if (TryConsumeAction(actionId)) return true;

            float startTime = _currentTime;
            while (_currentTime - startTime < timeoutSeconds)
            {
                if (ct.IsCancellationRequested) return false;
                if (TryConsumeAction(actionId)) return true;

                await Task.Yield();
            }

            return false;
        }

        /// <summary>
        /// Triggers a vibration effect on the current input device.
        /// </summary>
        public void Vibrate(float intensity, float duration)
        {
            _provider?.Vibrate(intensity, duration);

            // Basic support for New Input System if active
#if UNITY_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(intensity, intensity);
                StopVibrationAfterDelay((int)(duration * 1000));
            }
#endif
        }

        private async void StopVibrationAfterDelay(int delayMs)
        {
#if UNITY_INPUT_SYSTEM
            await Task.Delay(delayMs);
            UnityEngine.InputSystem.Gamepad.current?.SetMotorSpeeds(0, 0);
#endif
        }

        private InputDeviceType DetectDeviceType()
        {
#if UNITY_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.wasUpdatedThisFrame)
                return InputDeviceType.Gamepad;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.anyKey)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (UnityEngine.Input.GetKey(_joystickButtonNames[i])) return InputDeviceType.Gamepad;
                }
                return InputDeviceType.KeyboardMouse;
            }
#endif

            return InputDeviceType.KeyboardMouse;
        }

        /// <summary>
        /// Gets the current buffer (read-only for external systems like ComboSystem).
        /// </summary>
        public IReadOnlyList<BufferedInput> GetBuffer() => _buffer;

        /// <summary>
        /// Gets all registered action IDs (read-only).
        /// </summary>
        public IReadOnlyList<string> GetRegisteredActions() => _registeredActions;

        /// <summary>
        /// Clears all entries currently in the input buffer.
        /// </summary>
        public void ClearBuffer() => _buffer.Clear();

        /// <summary>
        /// Internal method to advance time for testing.
        /// </summary>
        internal void SetTimeForTesting(float time)
        {
            _currentTime = time;
        }
    }
}
