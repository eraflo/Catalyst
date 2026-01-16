using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eraflo.Catalyst.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst.Scenes.Networking
{
    /// <summary>
    /// Loading strategy that synchronizes scene changes across the network.
    /// Acts as a bridge between the SceneLoaderService and the INetworkBackend.
    /// </summary>
    public class SceneNetworkHandler : ISceneLoadingStrategy
    {
        private readonly NetworkManager _network;
        private readonly ISceneManager _localSceneManager;
        private readonly HashSet<string> _networkLoadedScenes = new HashSet<string>();
        private bool _isSynchronizing;

        public SceneNetworkHandler()
        {
            _network = App.Get<NetworkManager>();
            _localSceneManager = App.Get<ISceneManager>();
        }

        public async Task LoadAsync(List<string> sceneNames, Action<float> onProgress)
        {
            if (!_network.IsConnected)
            {
                // Fallback to local loading if disconnected
                var local = new LocalLoadingStrategy(_localSceneManager);
                await local.LoadAsync(sceneNames, onProgress);
                return;
            }

            if (_network.IsServer)
            {
                await HandleServerLoad(sceneNames, onProgress);
            }
            else
            {
                await HandleClientLoad(sceneNames, onProgress);
            }
        }

        public async Task UnloadAsync(List<Scene> scenes)
        {
            if (!_network.IsConnected || _network.IsServer)
            {
                foreach (var scene in scenes)
                {
                    if (!scene.isLoaded) continue;

                    // Only use network unload for scenes that were loaded via network
                    if (_network.IsServer && _networkLoadedScenes.Contains(scene.name) &&
                        _network.Backend is ISceneNetworkBackend backend)
                    {
                        await backend.UnloadSceneAsync(scene);
                        _networkLoadedScenes.Remove(scene.name);
                    }
                    else
                    {
                        await _localSceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
        }

        private async Task HandleServerLoad(List<string> sceneNames, Action<float> onProgress)
        {
            float total = sceneNames.Count;
            for (int i = 0; i < sceneNames.Count; i++)
            {
                var sceneName = sceneNames[i];
                int index = i;

                // Backend specific
                if (_network.Backend is ISceneNetworkBackend backend)
                {
                    await backend.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    _networkLoadedScenes.Add(sceneName); // Track it
                }
                else
                {
                    // Fallback or manual sync message
                    await _localSceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive, (p) =>
                    {
                        onProgress?.Invoke((index + p) / total);
                    });
                }
            }
            onProgress?.Invoke(1f);
        }

        private async Task HandleClientLoad(List<string> sceneNames, Action<float> onProgress)
        {
            _isSynchronizing = true;
            Debug.Log("[SceneNetworkHandler] Client waiting for server scene synchronization...");

            // Logic to track progress while the backend loads scenes
            while (_isSynchronizing)
            {
                bool allLoaded = true;
                int loadedCount = 0;

                foreach (var name in sceneNames)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);
                    if (scene.isLoaded) loadedCount++;
                    else allLoaded = false;
                }

                float progress = sceneNames.Count > 0 ? (float)loadedCount / sceneNames.Count : 1f;
                onProgress?.Invoke(progress);

                if (allLoaded) _isSynchronizing = false;
                else await Task.Delay(100);
            }

            onProgress?.Invoke(1f);
            Debug.Log("[SceneNetworkHandler] Client synchronization complete.");
        }

    }
}
