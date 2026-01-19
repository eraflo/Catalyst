#if UNITY_NETCODE
using System.Threading.Tasks;
using Eraflo.Catalyst.Scenes.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetcodeMgr = Unity.Netcode.NetworkManager;

namespace Eraflo.Catalyst.Networking.Backends.Netcode
{
    /// <summary>
    /// Handles networked scene management for Netcode for GameObjects.
    /// </summary>
    public class NetcodeSceneHandler : ISceneNetworkBackend
    {
        private readonly NetcodeMgr _netcodeMgr;

        public NetcodeSceneHandler(NetcodeMgr netcodeMgr)
        {
            _netcodeMgr = netcodeMgr;
        }

        public async Task LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            if (!_netcodeMgr.IsServer) return;

            var tcs = new TaskCompletionSource<bool>();
            string targetName = sceneName;
            if (targetName.EndsWith(".unity")) targetName = System.IO.Path.GetFileNameWithoutExtension(targetName);

            void OnSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
            {
                // On Server, we wait for 'Completed' which means ALL clients have finished.
                if (IsSceneMatch(sceneEvent.SceneName, targetName) && 
                    (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadEventCompleted))
                {
                    _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                    tcs.TrySetResult(true);
                }
            }

            _netcodeMgr.SceneManager.OnSceneEvent += OnSceneEvent;

            var status = _netcodeMgr.SceneManager.LoadScene(sceneName, mode);
            if (status != Unity.Netcode.SceneEventProgressStatus.Started)
            {
                _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                Debug.LogError($"[NetcodeSceneHandler] Failed to start scene load: {status}");
                tcs.TrySetResult(false);
            }

            await tcs.Task;
        }

        public async Task UnloadSceneAsync(Scene scene)
        {
            if (_netcodeMgr == null || _netcodeMgr.SceneManager == null) return;

            var tcs = new TaskCompletionSource<bool>();
            string targetName = scene.name;

            void OnSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
            {
                // Server: Wait for all clients to finish (UnloadEventCompleted)
                // Client: Wait for local completion (UnloadComplete)
                bool isCompleted = _netcodeMgr.IsServer 
                    ? sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.UnloadEventCompleted
                    : sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.UnloadComplete;

                if (IsSceneMatch(sceneEvent.SceneName, targetName) && isCompleted)
                {
                    _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                    tcs.TrySetResult(true);
                }
            }

            _netcodeMgr.SceneManager.OnSceneEvent += OnSceneEvent;

            if (_netcodeMgr.IsServer)
            {
                var status = _netcodeMgr.SceneManager.UnloadScene(scene);
                if (status != Unity.Netcode.SceneEventProgressStatus.Started)
                {
                    _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                    Debug.LogError($"[NetcodeSceneHandler] Failed to start server scene unload for '{targetName}': {status}");
                    tcs.TrySetResult(false);
                }
            }

            // Safety timeout to prevent indefinite hangs
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            if (completedTask != tcs.Task)
            {
                _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                if (!_netcodeMgr.IsServer)
                {
                    // On client, a timeout might just mean NGO already unloaded it before we started listening
                    var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(targetName);
                    if (!s.IsValid() || !s.isLoaded) tcs.TrySetResult(true);
                }
            }
            
            await tcs.Task;
        }

        private bool IsSceneMatch(string scenePathOrName, string targetName)
        {
            if (string.IsNullOrEmpty(scenePathOrName)) return false;
            
            return scenePathOrName == targetName ||
                   scenePathOrName.EndsWith("/" + targetName) ||
                   scenePathOrName.EndsWith("/" + targetName + ".unity") ||
                   System.IO.Path.GetFileNameWithoutExtension(scenePathOrName) == targetName;
        }
    }
}
#endif
