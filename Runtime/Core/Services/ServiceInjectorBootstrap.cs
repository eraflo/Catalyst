using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Hidden, persistent component that drives automatic <see cref="ServiceInjector"/>
    /// injection for every scene loaded during the session.
    /// <para>
    /// Created once by <see cref="ServiceInjector"/> during the
    /// <c>RuntimeInitializeLoadType.BeforeSceneLoad</c> phase and kept alive via
    /// <c>DontDestroyOnLoad</c>. The <c>[DefaultExecutionOrder(int.MinValue)]</c> attribute
    /// ensures this component's <c>Awake</c> runs before every other script in the same scene
    /// if it were ever placed there directly.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(int.MinValue)]
    internal sealed class ServiceInjectorBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Injects all <see cref="InjectAttribute"/> fields in the newly loaded scene.
        /// Fires after the scene's <c>Awake</c> calls but before any <c>Start</c> call.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ServiceInjector.InjectScene(scene);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
