using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Default loading strategy for local-only scene management.
    /// </summary>
    public class LocalLoadingStrategy : ISceneLoadingStrategy
    {
        private readonly ISceneManager _sceneManager;

        public LocalLoadingStrategy(ISceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public async Task LoadAsync(List<string> sceneNames, Action<float> onProgress)
        {
            float total = sceneNames.Count;
            for (int i = 0; i < sceneNames.Count; i++)
            {
                var sceneName = sceneNames[i];
                int index = i;
                
                await _sceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive, (p) => 
                {
                    float progress = (index + p) / total;
                    onProgress?.Invoke(progress);
                });
            }
            onProgress?.Invoke(1f);
        }

        public async Task UnloadAsync(List<Scene> scenes)
        {
            foreach (var scene in scenes)
            {
                if (scene.isLoaded)
                {
                    await _sceneManager.UnloadSceneAsync(scene);
                }
            }
        }
    }
}
