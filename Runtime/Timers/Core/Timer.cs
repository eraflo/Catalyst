using System;
using UnityEngine;
using Eraflo.UnityImportPackage.Timers.Backends;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Main static API for the Timer system.
    /// Use Timer.Create&lt;T&gt;() to create any timer type.
    /// Backend (Standard/Burst) is selected automatically based on PackageSettings.
    /// </summary>
    public static partial class Timer
    {
        private static ITimerBackend _backend;
        private static bool _initialized;

        #region Properties

        /// <summary>
        /// Whether Burst mode is active.
        /// </summary>
        public static bool IsBurstMode => PackageSettings.Instance.UseBurstTimers;

        /// <summary>
        /// Number of active timers.
        /// </summary>
        public static int Count => _backend?.Count ?? 0;

        /// <summary>
        /// Timer system metrics for debugging and profiling.
        /// </summary>
        public static TimerMetrics Metrics { get; } = new TimerMetrics();

        #endregion

        #region Internal Lifecycle

        /// <summary>
        /// Initializes the timer system. Called automatically by TimerBootstrapper.
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized) return;

            if (IsBurstMode)
            {
                _backend = new BurstBackend();
                Debug.Log("[Timer] Initialized with Burst backend");
            }
            else
            {
                _backend = new StandardBackend();
                Debug.Log("[Timer] Initialized with Standard backend");
            }

            _initialized = true;
        }

        /// <summary>
        /// Shuts down the timer system.
        /// </summary>
        internal static void Shutdown()
        {
            _backend?.Dispose();
            _backend = null;
            _initialized = false;
            TimerCallbacks.Clear();
        }

        /// <summary>
        /// Updates all timers. Called automatically by PlayerLoop.
        /// </summary>
        internal static void Update()
        {
            _backend?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates a timer of the specified type.
        /// </summary>
        /// <typeparam name="T">Timer type implementing ITimer.</typeparam>
        /// <param name="duration">Duration in seconds.</param>
        /// <returns>Handle to the created timer.</returns>
        public static TimerHandle Create<T>(float duration) where T : struct, ITimer
        {
            EnsureInitialized();
            var handle = _backend.Create<T>(TimerConfig.FromDuration(duration));
            Metrics.RecordCreation(duration);
            return handle;
        }

        /// <summary>
        /// Creates a timer of the specified type with full configuration.
        /// </summary>
        /// <typeparam name="T">Timer type implementing ITimer.</typeparam>
        /// <param name="config">Timer configuration.</param>
        /// <returns>Handle to the created timer.</returns>
        public static TimerHandle Create<T>(TimerConfig config) where T : struct, ITimer
        {
            EnsureInitialized();
            var handle = _backend.Create<T>(config);
            Metrics.RecordCreation(config.Duration);
            return handle;
        }

        /// <summary>
        /// Creates a delay that executes a callback after the specified time.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        /// <param name="onComplete">Callback to execute.</param>
        /// <param name="useUnscaledTime">If true, ignores Time.timeScale.</param>
        /// <returns>Handle to the delay timer.</returns>
        public static TimerHandle Delay(float delay, Action onComplete, bool useUnscaledTime = false)
        {
            var handle = Create<DelayTimer>(TimerConfig.Create(delay, useUnscaledTime: useUnscaledTime));
            On<OnComplete>(handle, onComplete);
            return handle;
        }

        #endregion

        #region Control

        /// <summary>Pauses a timer.</summary>
        public static void Pause(TimerHandle handle) => _backend?.Pause(handle);

        /// <summary>Resumes a paused timer.</summary>
        public static void Resume(TimerHandle handle) => _backend?.Resume(handle);

        /// <summary>Starts a timer that was created but not yet running.</summary>
        public static void Start(TimerHandle handle) => _backend?.Start(handle);

        /// <summary>Cancels and removes a timer.</summary>
        public static void Cancel(TimerHandle handle)
        {
            _backend?.Cancel(handle);
            Metrics.RecordCancellation();
        }

        /// <summary>Resets a timer to its initial state.</summary>
        public static void Reset(TimerHandle handle)
        {
            _backend?.Reset(handle);
            Metrics.RecordReset();
        }

        /// <summary>Sets the time scale of a timer.</summary>
        public static void SetTimeScale(TimerHandle handle, float scale) => _backend?.SetTimeScale(handle, scale);

        #endregion

        #region Query

        /// <summary>Gets the current time of a timer.</summary>
        public static float GetCurrentTime(TimerHandle handle) => _backend?.GetCurrentTime(handle) ?? 0f;

        /// <summary>Gets the progress (0-1) of a timer.</summary>
        public static float GetProgress(TimerHandle handle) => _backend?.GetProgress(handle) ?? 0f;

        /// <summary>Checks if a timer has finished.</summary>
        public static bool IsFinished(TimerHandle handle) => _backend?.IsFinished(handle) ?? true;

        /// <summary>Checks if a timer is running.</summary>
        public static bool IsRunning(TimerHandle handle) => _backend?.IsRunning(handle) ?? false;

        #endregion

        #region Utility

        /// <summary>Clears all timers.</summary>
        public static void Clear() => _backend?.Clear();

        /// <summary>Gets debug info for all active timers.</summary>
        public static System.Collections.Generic.List<TimerDebugInfo> GetActiveTimers() 
            => _backend?.GetActiveTimers() ?? new System.Collections.Generic.List<TimerDebugInfo>();

        #endregion

        #region Presets

        /// <summary>
        /// Creates a timer from a preset.
        /// </summary>
        /// <param name="presetName">Name of the preset to use.</param>
        /// <returns>Timer handle, or None if preset not found.</returns>
        public static TimerHandle FromPreset(string presetName)
        {
            var preset = TimerPresets.Get(presetName);
            if (preset.TimerType == null)
            {
                UnityEngine.Debug.LogWarning($"[Timer] Preset '{presetName}' not found.");
                return TimerHandle.None;
            }

            EnsureInitialized();
            
            // Use reflection to call generic Create<T>
            var method = typeof(Timer).GetMethod(nameof(Create), new[] { typeof(TimerConfig) });
            var generic = method.MakeGenericMethod(preset.TimerType);
            return (TimerHandle)generic.Invoke(null, new object[] { preset.ToConfig() });
        }

        /// <summary>
        /// Creates a timer from a preset with an OnComplete callback.
        /// </summary>
        /// <param name="presetName">Name of the preset to use.</param>
        /// <param name="onComplete">Callback when timer completes.</param>
        /// <returns>Timer handle, or None if preset not found.</returns>
        public static TimerHandle FromPreset(string presetName, System.Action onComplete)
        {
            var handle = FromPreset(presetName);
            if (handle.IsValid && onComplete != null)
            {
                On<OnComplete>(handle, onComplete);
            }
            return handle;
        }

        /// <summary>
        /// Gets the easing type associated with a preset.
        /// </summary>
        public static EasingSystem.EasingType GetPresetEasing(string presetName)
        {
            return TimerPresets.Get(presetName).Easing;
        }

        #endregion
    }
}
