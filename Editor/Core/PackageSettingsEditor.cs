using UnityEditor;
using UnityEngine;
using System.IO;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Editor script that auto-creates PackageSettings on package import.
    /// Also provides a menu item to access settings.
    /// </summary>
    [InitializeOnLoad]
    public static class PackageSettingsEditor
    {
        private const string SettingsPath = "Assets/Resources/CatalystSettings.asset";
        private const string ResourcesPath = "Assets/Resources";

        static PackageSettingsEditor()
        {
            // Delay to ensure Unity is fully loaded
            EditorApplication.delayCall += EnsureSettingsExist;
        }

        private static void EnsureSettingsExist()
        {
            if (!File.Exists(SettingsPath))
            {
                CreateSettings();
            }
        }

        [MenuItem("Tools/Eraflo Catalyst/Settings", priority = 0)]
        public static void OpenSettings()
        {
            var settings = GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Tools/Eraflo Catalyst/Create Settings", priority = 1)]
        public static void CreateSettingsMenu()
        {
            var settings = CreateSettings();
            Selection.activeObject = settings;
        }

        private static PackageSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(SettingsPath);
            
            if (settings == null)
            {
                settings = CreateSettings();
            }

            return settings;
        }

        private static PackageSettings CreateSettings()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(ResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<PackageSettings>(SettingsPath);
            if (existing != null)
            {
                return existing;
            }

            // Create new settings
            var settings = ScriptableObject.CreateInstance<PackageSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PackageSettingsEditor] Created settings at {SettingsPath}");

            return settings;
        }
    }
}
