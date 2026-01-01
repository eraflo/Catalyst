using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Utility for loading EventChannels via Addressables.
    /// </summary>
    public static class EventChannelLoader
    {
        private static readonly Dictionary<string, ScriptableObject> LoadedChannels = 
            new Dictionary<string, ScriptableObject>();

        /// <summary>
        /// Loads an EventChannel by its address asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of EventChannel.</typeparam>
        /// <param name="address">The Addressable address.</param>
        /// <param name="onLoaded">Callback when loaded.</param>
        public static void LoadAsync<T>(string address, Action<T> onLoaded) where T : ScriptableObject
        {
            if (LoadedChannels.TryGetValue(address, out var cached))
            {
                onLoaded?.Invoke(cached as T);
                return;
            }

            Addressables.LoadAssetAsync<T>(address).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadedChannels[address] = handle.Result;
                    onLoaded?.Invoke(handle.Result);
                }
                else
                {
                    Debug.LogError($"[EventChannelLoader] Failed to load: {address}");
                    onLoaded?.Invoke(null);
                }
            };
        }

        /// <summary>
        /// Loads an EventChannel by its address synchronously (blocking).
        /// Use with caution - prefer async loading.
        /// </summary>
        /// <typeparam name="T">The type of EventChannel.</typeparam>
        /// <param name="address">The Addressable address.</param>
        /// <returns>The loaded channel or null.</returns>
        public static T LoadSync<T>(string address) where T : ScriptableObject
        {
            if (LoadedChannels.TryGetValue(address, out var cached))
            {
                return cached as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            var result = handle.WaitForCompletion();
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                LoadedChannels[address] = result;
                return result;
            }

            Debug.LogError($"[EventChannelLoader] Failed to load: {address}");
            return null;
        }

        /// <summary>
        /// Preloads multiple EventChannels by label or addresses.
        /// </summary>
        /// <param name="labelOrAddresses">Label or list of addresses.</param>
        /// <param name="onComplete">Callback when all are loaded.</param>
        public static void PreloadByLabel(string label, Action onComplete = null)
        {
            Addressables.LoadAssetsAsync<ScriptableObject>(label, channel =>
            {
                if (channel != null)
                {
                    // Use the asset name as a simple key
                    LoadedChannels[channel.name] = channel;
                }
            }).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"[EventChannelLoader] Preloaded {handle.Result.Count} channels with label: {label}");
                }
                onComplete?.Invoke();
            };
        }

        /// <summary>
        /// Gets a previously loaded channel from cache.
        /// </summary>
        /// <typeparam name="T">The type of EventChannel.</typeparam>
        /// <param name="nameOrAddress">The name or address of the channel.</param>
        /// <returns>The cached channel or null.</returns>
        public static T Get<T>(string nameOrAddress) where T : ScriptableObject
        {
            return LoadedChannels.TryGetValue(nameOrAddress, out var channel) ? channel as T : null;
        }

        /// <summary>
        /// Releases all loaded channels from memory.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var channel in LoadedChannels.Values)
            {
                Addressables.Release(channel);
            }
            LoadedChannels.Clear();
        }
    }
}
