using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Core.Settings
{
    /// <summary>
    /// Serializable data class containing all user preferences.
    /// This is a partial class to allow users to extend it in their own file.
    /// </summary>
    [Serializable]
    public partial class SettingsData
    {
        [Header("Audio")]
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 0.8f;

        [Header("Video")]
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public bool Fullscreen = true;
        public bool VSync = true;
        public int QualityLevel = 2;

        [Header("Gameplay")]
        public float MouseSensitivity = 1f;
        public bool InvertY = false;

        /// <summary>
        /// Fallback dictionary for custom settings that don't need a strongly typed field.
        /// </summary>
        public Dictionary<string, string> CustomSettings = new Dictionary<string, string>();
    }
}
