using System.Collections.Generic;
using Eraflo.UnityImportPackage.EasingSystem;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Preset configuration for creating timers with predefined settings.
    /// </summary>
    public struct TimerPreset
    {
        public string Name;
        public float Duration;
        public float TimeScale;
        public bool UseUnscaledTime;
        public EasingType Easing;
        public System.Type TimerType;

        public TimerConfig ToConfig() => new TimerConfig
        {
            Duration = Duration,
            TimeScale = TimeScale,
            UseUnscaledTime = UseUnscaledTime
        };
    }

    /// <summary>
    /// Registry for timer presets. Define reusable timer configurations.
    /// </summary>
    public static class TimerPresets
    {
        private static readonly Dictionary<string, TimerPreset> _presets = new Dictionary<string, TimerPreset>();

        /// <summary>
        /// Defines a simple timer preset.
        /// </summary>
        /// <param name="name">Unique preset name.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="easing">Optional easing type.</param>
        public static void Define(string name, float duration, EasingType easing = EasingType.Linear)
        {
            Define<CountdownTimer>(name, duration, easing);
        }

        /// <summary>
        /// Defines a timer preset with specific timer type.
        /// </summary>
        /// <typeparam name="T">Timer type.</typeparam>
        /// <param name="name">Unique preset name.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="easing">Optional easing type.</param>
        /// <param name="timeScale">Time scale multiplier.</param>
        /// <param name="useUnscaledTime">Whether to use unscaled time.</param>
        public static void Define<T>(
            string name, 
            float duration, 
            EasingType easing = EasingType.Linear,
            float timeScale = 1f,
            bool useUnscaledTime = false) where T : struct, ITimer
        {
            _presets[name] = new TimerPreset
            {
                Name = name,
                Duration = duration,
                TimeScale = timeScale,
                UseUnscaledTime = useUnscaledTime,
                Easing = easing,
                TimerType = typeof(T)
            };
        }

        /// <summary>
        /// Gets a preset by name.
        /// </summary>
        /// <param name="name">Preset name.</param>
        /// <returns>The preset, or default if not found.</returns>
        public static TimerPreset Get(string name)
        {
            return _presets.TryGetValue(name, out var preset) ? preset : default;
        }

        /// <summary>
        /// Checks if a preset exists.
        /// </summary>
        public static bool Exists(string name) => _presets.ContainsKey(name);

        /// <summary>
        /// Removes a preset.
        /// </summary>
        public static void Remove(string name) => _presets.Remove(name);

        /// <summary>
        /// Clears all presets.
        /// </summary>
        public static void Clear() => _presets.Clear();

        /// <summary>
        /// Gets all preset names.
        /// </summary>
        public static IEnumerable<string> GetAllNames() => _presets.Keys;
    }
}
