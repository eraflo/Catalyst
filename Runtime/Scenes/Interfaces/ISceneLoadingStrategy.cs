using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Contract for scene loading operations, allowing decoupling between the 
    /// SceneLoaderService and the actual loading implementation (Local, Networked, etc.).
    /// </summary>
    public interface ISceneLoadingStrategy
    {
        /// <summary>
        /// Loads a set of scenes using the specific strategy.
        /// </summary>
        /// <param name="sceneNames">Names of scenes to load.</param>
        /// <param name="onProgress">Callback for progress updates (0 to 1).</param>
        /// <returns>Task representing the loading operation.</returns>
        Task LoadAsync(List<string> sceneNames, Action<float> onProgress);

        /// <summary>
        /// Unloads a set of scenes.
        /// </summary>
        /// <param name="scenes">Scenes to unload.</param>
        /// <returns>Task representing the unloading operation.</returns>
        Task UnloadAsync(List<Scene> scenes);
    }
}
