using System;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Timers.Backends;
using UnityEngine;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Main API for the Timer system.
    /// Can be used as a service via Service Locator.
    /// Backend (Standard/Burst) is selected automatically based on PackageSettings.
    /// </summary>
    [Service(Priority = 0)]
    public partial class Timer : IGameService, IUpdatable
    {
        private ITimerBackend _backend;
        private bool _initialized;


        #region Properties

        /// <summary>
        /// The backend used for timer operations.
        /// </summary>
        public ITimerBackend Backend => _backend;

        /// <summary>
        /// Whether Burst mode is active.
        /// </summary>
        public bool IsBurstMode => PackageSettings.Instance.UseBurstTimers;

        /// <summary>
        /// Number of active timers.
        /// </summary>
        public int Count => _backend?.Count ?? 0;

        /// <summary>
        /// Timer system metrics for debugging and profiling.
        /// </summary>
        public TimerMetrics Metrics { get; } = new TimerMetrics();

        #endregion

        #region IGameService & Lifecycle

        void IGameService.Initialize()
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
        void IGameService.Shutdown()
        {
            _backend?.Dispose();
            _backend = null;
            _initialized = false;
            TimerCallbacks.Clear();
        }

        void IUpdatable.OnUpdate()
        {
            Update();
        }

        /// <summary>
        /// Updates all timers.
        /// </summary>
        public void Update()
        {
            _backend?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                ((IGameService)this).Initialize();
            }
        }

        #endregion


        #region Instance Methods

        /// <summary>
        /// Creates a timer of the specified type.
        /// </summary>
        /// <typeparam name="T">Timer type implementing ITimer.</typeparam>
        /// <param name="duration">Duration in seconds.</param>
        /// <returns>Handle to the created timer.</returns>
        public TimerHandle CreateTimer<T>(float duration) where T : struct, ITimer
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
        public TimerHandle CreateTimer<T>(TimerConfig config) where T : struct, ITimer
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
        public TimerHandle CreateDelay(float delay, Action onComplete, bool useUnscaledTime = false)
        {
            var handle = CreateTimer<DelayTimer>(TimerConfig.Create(delay, useUnscaledTime: useUnscaledTime));
            TimerCallbacks.Register<OnComplete>(handle, onComplete);
            return handle;
        }

        public void CancelTimer(TimerHandle handle)
        {
            _backend?.Cancel(handle);
            Metrics.RecordCancellation();
        }

        public void ResetTimer(TimerHandle handle)
        {
            _backend?.Reset(handle);
            Metrics.RecordReset();
        }

        /// <summary>Pauses a timer.</summary>
        public void Pause(TimerHandle handle) => _backend?.Pause(handle);

        /// <summary>Resumes a paused timer.</summary>
        public void Resume(TimerHandle handle) => _backend?.Resume(handle);

        /// <summary>Starts a timer that was created but not yet running.</summary>
        public void Start(TimerHandle handle) => _backend?.Start(handle);

        /// <summary>Sets the time scale of a timer.</summary>
        public void SetTimeScale(TimerHandle handle, float scale) => _backend?.SetTimeScale(handle, scale);

        /// <summary>Sets the time channel of a timer.</summary>
        public void SetChannel(TimerHandle handle, string channel) => _backend?.SetChannel(handle, channel);

        /// <summary>Gets the current time of a timer.</summary>
        public float GetCurrentTime(TimerHandle handle) => _backend?.GetCurrentTime(handle) ?? 0f;

        /// <summary>Forces the current time of a timer (used for synchronization).</summary>
        public void SetCurrentTime(TimerHandle handle, float time) => _backend?.SetCurrentTime(handle, time);

        /// <summary>Gets the progress (0-1) of a timer.</summary>
        public float GetProgress(TimerHandle handle) => _backend?.GetProgress(handle) ?? 0f;

        /// <summary>Checks if a timer has finished.</summary>
        public bool IsFinished(TimerHandle handle) => _backend?.IsFinished(handle) ?? true;

        /// <summary>Checks if a timer is running.</summary>
        public bool IsRunning(TimerHandle handle) => _backend?.IsRunning(handle) ?? false;

        /// <summary>Clears all timers.</summary>
        public void Clear() => _backend?.Clear();

        /// <summary>Gets debug info for all active timers.</summary>
        public System.Collections.Generic.List<TimerDebugInfo> GetActiveTimers()
            => _backend?.GetActiveTimers() ?? new System.Collections.Generic.List<TimerDebugInfo>();

        #endregion

        #region Presets

        /// <summary>
        /// Creates a timer from a preset.
        /// </summary>
        /// <param name="presetName">Name of the preset to use.</param>
        /// <returns>Timer handle, or None if preset not found.</returns>

        public TimerHandle CreateFromPreset(string presetName)
        {
            var preset = TimerPresets.Get(presetName);
            if (preset.TimerType == null) return TimerHandle.None;

            EnsureInitialized();

            // Use reflection to call generic CreateTimer<T>
            var method = typeof(Timer).GetMethod(nameof(CreateTimer), new[] { typeof(TimerConfig) });
            var generic = method.MakeGenericMethod(preset.TimerType);
            return (TimerHandle)generic.Invoke(this, new object[] { preset.ToConfig() });
        }

        /// <summary>
        /// Creates a timer from a preset with an OnComplete callback.
        /// </summary>
        /// <param name="presetName">Name of the preset to use.</param>
        /// <param name="onComplete">Callback when timer completes.</param>
        /// <returns>Timer handle, or None if preset not found.</returns>
        public TimerHandle CreateFromPreset(string presetName, Action onComplete)
        {
            var handle = CreateFromPreset(presetName);
            if (handle.IsValid)
            {
                TimerCallbacks.Register<OnComplete>(handle, onComplete);
            }
            return handle;
        }

        #endregion
    }
}
