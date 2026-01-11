using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Eraflo.Catalyst.Core.Settings
{
    /// <summary>
    /// Static utility class for settings-related calculations and Unity API wrappers.
    /// </summary>
    public static class SettingsUtils
    {
        private const float MinDecibels = -80f;
        private const float MaxDecibels = 0f;

        /// <summary>
        /// Converts a linear value (0.0 to 1.0) to decibels (-80dB to 0dB).
        /// </summary>
        public static float LinearToDecibel(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= 0.0001f) return MinDecibels;
            return Mathf.Log10(linear) * 20f;
        }

        /// <summary>
        /// Converts a decibel value (-80dB to 0dB) to linear (0.0 to 1.0).
        /// </summary>
        public static float DecibelToLinear(float db)
        {
            db = Mathf.Clamp(db, MinDecibels, MaxDecibels);
            return Mathf.Pow(10f, db / 20f);
        }

        /// <summary>
        /// Applies volume to an AudioMixer parameter.
        /// </summary>
        public static void ApplyVolume(AudioMixer mixer, string parameter, float linearValue)
        {
            if (mixer == null) return;
            float db = LinearToDecibel(linearValue);
            mixer.SetFloat(parameter, db);
        }

        /// <summary>
        /// Applies screen resolution and fullscreen mode.
        /// </summary>
        public static void ApplyResolution(int width, int height, bool fullscreen)
        {
            Screen.SetResolution(width, height, fullscreen);
        }

        /// <summary>
        /// Applies Unity quality level.
        /// </summary>
        public static void ApplyQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
        }

        /// <summary>
        /// Applies VSync count.
        /// </summary>
        public static void ApplyVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }
    }
}
