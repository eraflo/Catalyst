using UnityEngine;
using Eraflo.UnityImportPackage.EasingSystem;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Timer easing and interpolation methods.
    /// </summary>
    public static partial class Timer
    {
        /// <summary>
        /// Gets the progress (0-1) of a timer with easing applied.
        /// </summary>
        /// <param name="handle">Timer handle.</param>
        /// <param name="easing">Easing type to apply.</param>
        /// <returns>Eased progress value (0-1).</returns>
        public static float GetEasedProgress(TimerHandle handle, EasingType easing)
        {
            float progress = GetProgress(handle);
            return Easing.Evaluate(progress, easing);
        }

        /// <summary>
        /// Lerps between two float values based on timer progress with optional easing.
        /// </summary>
        /// <param name="handle">Timer handle.</param>
        /// <param name="from">Start value.</param>
        /// <param name="to">End value.</param>
        /// <param name="easing">Easing type (default Linear).</param>
        /// <returns>Interpolated value.</returns>
        public static float Lerp(TimerHandle handle, float from, float to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Mathf.Lerp(from, to, t);
        }

        /// <summary>
        /// Lerps between two Vector2 values based on timer progress with optional easing.
        /// </summary>
        public static Vector2 Lerp(TimerHandle handle, Vector2 from, Vector2 to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Vector2.Lerp(from, to, t);
        }

        /// <summary>
        /// Lerps between two Vector3 values based on timer progress with optional easing.
        /// </summary>
        public static Vector3 Lerp(TimerHandle handle, Vector3 from, Vector3 to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Vector3.Lerp(from, to, t);
        }

        /// <summary>
        /// Lerps between two Quaternion values based on timer progress with optional easing.
        /// </summary>
        public static Quaternion Lerp(TimerHandle handle, Quaternion from, Quaternion to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Quaternion.Lerp(from, to, t);
        }

        /// <summary>
        /// Lerps between two Color values based on timer progress with optional easing.
        /// </summary>
        public static Color Lerp(TimerHandle handle, Color from, Color to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Color.Lerp(from, to, t);
        }

        /// <summary>
        /// Unclamped lerp - allows values outside 0-1 range (useful for Elastic/Back easing).
        /// </summary>
        public static float LerpUnclamped(TimerHandle handle, float from, float to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Mathf.LerpUnclamped(from, to, t);
        }

        /// <summary>
        /// Unclamped Vector3 lerp.
        /// </summary>
        public static Vector3 LerpUnclamped(TimerHandle handle, Vector3 from, Vector3 to, EasingType easing = EasingType.Linear)
        {
            float t = GetEasedProgress(handle, easing);
            return Vector3.LerpUnclamped(from, to, t);
        }
    }
}
