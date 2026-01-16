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

            void OnSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
            {
                if (sceneEvent.SceneName == sceneName &&
                    sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadEventCompleted)
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
            if (!_netcodeMgr.IsServer) return;

            var tcs = new TaskCompletionSource<bool>();

            void OnSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
            {
                if (sceneEvent.SceneName == scene.name &&
                    sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.UnloadEventCompleted)
                {
                    _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                    tcs.TrySetResult(true);
                }
            }

            _netcodeMgr.SceneManager.OnSceneEvent += OnSceneEvent;

            var status = _netcodeMgr.SceneManager.UnloadScene(scene);
            if (status != Unity.Netcode.SceneEventProgressStatus.Started)
            {
                _netcodeMgr.SceneManager.OnSceneEvent -= OnSceneEvent;
                Debug.LogError($"[NetcodeSceneHandler] Failed to start scene unload: {status}");
                tcs.TrySetResult(false);
            }

            await tcs.Task;
        }
    }
}
#endif
