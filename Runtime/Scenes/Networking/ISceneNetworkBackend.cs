using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Eraflo.Catalyst.Scenes.Networking
{
    /// <summary>
    /// Interface that a network backend must implement to support networked scene loading.
    /// Owned by the Scenes module.
    /// </summary>
    public interface ISceneNetworkBackend
    {
        Task LoadSceneAsync(string sceneName, LoadSceneMode mode);
    }
}
