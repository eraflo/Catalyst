using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using System.IO;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Automatically registers EventChannel assets to Addressables.
    /// Creates a dedicated group and assigns addresses automatically.
    /// </summary>
    public class EventChannelAddressableProcessor : AssetPostprocessor
    {
        private const string GroupName = "EventChannels";
        private const string AddressPrefix = "Events/";

        /// <summary>
        /// Called when assets are imported, deleted, or moved.
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                ProcessAsset(assetPath);
            }

            foreach (string assetPath in movedAssets)
            {
                ProcessAsset(assetPath);
            }
        }

        private static void ProcessAsset(string assetPath)
        {
            // Only process .asset files
            if (!assetPath.EndsWith(".asset")) return;

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            
            // Check if it's an EventChannel
            if (asset == null) return;
            if (!IsEventChannel(asset)) return;

            RegisterToAddressables(assetPath, asset);
        }

        private static bool IsEventChannel(ScriptableObject asset)
        {
            var type = asset.GetType();
            
            // Check for EventChannel or EventChannel<T>
            while (type != null && type != typeof(object))
            {
                if (type == typeof(EventChannel)) return true;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventChannel<>)) return true;
                if (type.BaseType != null && type.BaseType.IsGenericType && 
                    type.BaseType.GetGenericTypeDefinition() == typeof(EventChannel<>)) return true;
                type = type.BaseType;
            }
            
            return false;
        }

        private static void RegisterToAddressables(string assetPath, ScriptableObject asset)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[EventChannelAddressable] Addressables not initialized. Please create Addressables Settings first.");
                return;
            }

            // Get or create the EventChannels group
            var group = GetOrCreateGroup(settings);
            if (group == null) return;

            // Get the asset GUID
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            
            // Check if already registered
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                // Update address if needed
                string expectedAddress = GenerateAddress(asset);
                if (entry.address != expectedAddress)
                {
                    entry.address = expectedAddress;
                    Debug.Log($"[EventChannelAddressable] Updated address: {expectedAddress}");
                }
                return;
            }

            // Create new entry
            entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = GenerateAddress(asset);
            
            Debug.Log($"[EventChannelAddressable] Registered: {entry.address}");
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(GroupName);
            
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, 
                    settings.DefaultGroup.Schemas, typeof(BundledAssetGroupSchema));
                
                Debug.Log($"[EventChannelAddressable] Created group: {GroupName}");
            }

            return group;
        }

        private static string GenerateAddress(ScriptableObject asset)
        {
            // Format: Events/TypeName/AssetName
            string typeName = asset.GetType().Name.Replace("EventChannel", "");
            if (string.IsNullOrEmpty(typeName)) typeName = "Void";
            
            return $"{AddressPrefix}{typeName}/{asset.name}";
        }

        /// <summary>
        /// Menu item to manually register all EventChannels.
        /// </summary>
        [MenuItem("Tools/Eraflo Catalyst/Register All EventChannels to Addressables")]
        public static void RegisterAllEventChannels()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (asset != null && IsEventChannel(asset))
                {
                    RegisterToAddressables(path, asset);
                    count++;
                }
            }

            Debug.Log($"[EventChannelAddressable] Registered {count} EventChannel(s) to Addressables");
        }
    }
}
