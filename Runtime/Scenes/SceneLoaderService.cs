using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eraflo.Catalyst.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Service responsible for orchestrating complex scene loading flows.
    /// Handles additive loading, loading screens, and memory management.
    /// </summary>
    [Service(Priority = 16)]
    public class SceneLoaderService : IGameService
    {
        private SceneTransitionChannel _onTransitionStarted;
        private SceneTransitionChannel _onTransitionCompleted;

        private ISceneLoadingStrategy _strategy;
        private ILoadingScreen _loadingScreen;
        private ISceneManager _sceneManager;
        private readonly List<SceneGroup> _groups = new List<SceneGroup>();
        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Sets the loading strategy (Local, Networked, etc.).
        /// </summary>
        public void SetStrategy(ISceneLoadingStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Sets the LoadingScreen implementation (useful for testing).
        /// </summary>
        public void SetLoadingScreen(ILoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen;
        }

        /// <summary>
        /// Sets the SceneManager implementation (useful for testing).
        /// </summary>
        public void SetSceneManager(ISceneManager sceneManager)
        {
            _sceneManager = sceneManager;
            // Re-initialize default strategy if none set
            if (_strategy == null) _strategy = new LocalLoadingStrategy(_sceneManager);
        }

        #region IGameService

        public void Initialize()
        {
            var settings = PackageSettings.Instance;
            _onTransitionStarted = settings.OnTransitionStarted;
            _onTransitionCompleted = settings.OnTransitionCompleted;

            if (_sceneManager == null)
            {
                _sceneManager = new UnitySceneManager();
                // Register as service so strategies can find it
                App.Register<ISceneManager>(_sceneManager);
            }

            if (_strategy == null)
            {
                _strategy = new LocalLoadingStrategy(_sceneManager);
            }
        }

        public void Shutdown()
        {
            _groups.Clear();
            _isTransitioning = false;
        }

        #endregion

        /// <summary>
        /// Registers a scene group.
        /// </summary>
        public void RegisterGroup(SceneGroup group)
        {
            if (group == null || string.IsNullOrEmpty(group.Name)) return;
            if (_groups.Any(g => g.Name == group.Name)) return;
            _groups.Add(group);
        }

        /// <summary>
        /// Loads a group of scenes asynchronously with a transition flow.
        /// </summary>
        /// <param name="groupName">The name of the scene group to load.</param>
        /// <param name="showLoadingScreen">Whether to show the ILoadingScreen during transition.</param>
        /// <param name="waitForInput">Whether to wait for user input before hiding the loading screen.</param>
        public async Task LoadGroupAsync(string groupName, bool showLoadingScreen = true, bool waitForInput = false)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneLoaderService] Already transitioning. Ignoring request to load '{groupName}'.");
                return;
            }

            var group = _groups.FirstOrDefault(g => g.Name == groupName);
            if (group == null)
            {
                Debug.LogError($"[SceneLoaderService] Scene group '{groupName}' not found.");
                return;
            }

            _isTransitioning = true;
            ILoadingScreen loadingScreen = null;

            try
            {
                // 1. Notify start
                _onTransitionStarted?.Raise(groupName);

                // 2. Show loading screen
                if (showLoadingScreen)
                {
                    loadingScreen = _loadingScreen ?? App.Get<ILoadingScreen>();
                    if (loadingScreen != null)
                    {
                        await loadingScreen.Show();
                    }
                }

                // 3. Keep track of current scenes to unload later
                // CRITICAL: We scan ALL scenes, but we must NOT unload scenes that are part of the new group
                int sceneCount = _sceneManager.SceneCount;
                var scenesToUnload = new List<Scene>();

                for (int i = 0; i < sceneCount; i++)
                {
                    var scene = _sceneManager.GetSceneAt(i);
                    if (group.Scenes.Contains(scene.name))
                    {
                        continue;
                    }

                    scenesToUnload.Add(scene);
                }

                // 4. Load new scenes via strategy first
                if (_strategy == null)
                {
                    Debug.LogError("[SceneLoaderService] Cannot load: No strategy set (likely shutting down).");
                    return;
                }

                await _strategy.LoadAsync(group.Scenes, (p) =>
                {
                    loadingScreen?.UpdateProgress(p * 0.8f); // 80% for loading
                });

                // 5. Set active scene BEFORE unloading old ones
                if (!string.IsNullOrEmpty(group.ActiveScene))
                {
                    var activeScene = _sceneManager.GetSceneByName(group.ActiveScene);
                    if (activeScene.IsValid())
                    {
                        _sceneManager.SetActiveScene(activeScene);
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneLoaderService] Could not set active scene: '{group.ActiveScene}' not found or invalid.");
                    }
                }

                // 6. Unload old scenes using strategy
                if (_strategy != null)
                {
                    await _strategy.UnloadAsync(scenesToUnload);
                }
                loadingScreen?.UpdateProgress(0.9f); // 90% after unload

                // 7. Memory Cleanup
                await UnloadUnusedAssetsAsync();
                GC.Collect();

                loadingScreen?.UpdateProgress(1f);

                // 8. Wait for input
                if (waitForInput)
                {
                    // TODO: This is a placeholder for actual input detection. 
                    // In a real framework, you'd check for a specific input action or button press.
                    await WaitForInputAsync();
                }

                // 9. Hide loading screen
                if (loadingScreen != null)
                {
                    await loadingScreen.Hide();
                }

                // 10. Notify completion
                _onTransitionCompleted?.Raise(groupName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                // Safety: always try to hide UI on error
                if (loadingScreen != null)
                {
                    await loadingScreen.Hide();
                }
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async Task UnloadUnusedAssetsAsync()
        {
            var op = Resources.UnloadUnusedAssets();
            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        private async Task WaitForInputAsync()
        {
            // Simple wait for any key or click for demonstration
            while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0))
            {
                await Task.Yield();
            }
        }
    }
}
