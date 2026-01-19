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

            // Register internal message handler for scene unloading
            if (_network != null)
            {
                _network.On<UnloadScenesMessage>(HandleUnloadScenesMessage);
            }
        }

        ~SceneNetworkHandler()
        {
            if (_network != null)
            {
                _network.Off<UnloadScenesMessage>(HandleUnloadScenesMessage);
            }
        }

        private string NormalizeName(string name)
            => System.IO.Path.GetFileNameWithoutExtension(name);

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

            // Add loaded scenes to the network loaded scenes set
            foreach (var sceneName in sceneNames)
            {
                _networkLoadedScenes.Add(NormalizeName(sceneName));
            }
        }

        public async Task UnloadAsync(List<Scene> scenes)
        {
            if (!_network.IsConnected)
            {
                foreach (var scene in scenes)
                {
                    if (scene.isLoaded)
                        await _localSceneManager.UnloadSceneAsync(scene);
                }
                _networkLoadedScenes.Clear();
                return;
            }

            var forceUnloadList = new List<string>();

            foreach (var scene in scenes)
            {
                if (!scene.isLoaded) continue;
                string normalizedName = NormalizeName(scene.name);

                // Strategy: 
                // 1. If it's a networked scene, the Backend must handle it (server unloads, client waits)
                // 2. If it's a local scene, we unload it locally and notify clients if we are server.

                if (_networkLoadedScenes.Contains(normalizedName) && _network.Backend is ISceneNetworkBackend backend)
                {
                    try
                    {
                        await backend.UnloadSceneAsync(scene);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SceneNetworkHandler] Failed to unload networked scene: {scene.name}");
                        Debug.LogException(e);
                    }
                    _networkLoadedScenes.Remove(normalizedName);
                }
                else
                {
                    // Local scene unloading
                    await _localSceneManager.UnloadSceneAsync(scene);
                    if (_network.IsServer)
                    {
                        forceUnloadList.Add(scene.name);
                    }
                }
            }

            if (_network.IsServer && forceUnloadList.Count > 0)
            {
                _network.SendToClients(new UnloadScenesMessage { SceneNames = forceUnloadList.ToArray() });
            }
        }

        private void HandleUnloadScenesMessage(UnloadScenesMessage msg)
        {
            if (_network.IsServer) return; // Server already handled it

            if (msg.SceneNames == null) return;

            foreach (var sceneName in msg.SceneNames)
            {
                string normalizedName = NormalizeName(sceneName);
                
                // CRITICAL: If the client considers this a networked scene, 
                // we MUST NOT unload it manually here because backend is likely 
                // already handling its synchronization.
                if (_networkLoadedScenes.Contains(normalizedName))
                {
                    continue;
                }

                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    _ = _localSceneManager.UnloadSceneAsync(scene);
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

            // Logic to track progress while the backend loads scenes
            while (_isSynchronizing)
            {
                if (sceneNames == null || sceneNames.Count == 0) break;

                bool allLoaded = true;
                int loadedCount = 0;

                foreach (var name in sceneNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);
                    if (scene.isLoaded)
                    {
                        _networkLoadedScenes.Add(name); // Track it on client
                        loadedCount++;
                    }
                    else allLoaded = false;
                }

                float progress = (float)loadedCount / sceneNames.Count;
                onProgress?.Invoke(progress);

                if (allLoaded) _isSynchronizing = false;
                else await Task.Delay(100);
            }

            onProgress?.Invoke(1f);
        }

    }

    [Serializable]
    public struct UnloadScenesMessage : INetworkMessage
    {
        public string[] SceneNames;

        public void Serialize(System.IO.BinaryWriter writer)
        {
            int count = SceneNames != null ? SceneNames.Length : 0;
            writer.Write(count);
            for (int i = 0; i < count; i++)
                writer.Write(SceneNames[i] ?? string.Empty);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            int count = reader.ReadInt32();
            SceneNames = new string[count];
            for (int i = 0; i < count; i++)
                SceneNames[i] = reader.ReadString();
        }
    }
}
