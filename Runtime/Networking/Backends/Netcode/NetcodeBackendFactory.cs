#if UNITY_NETCODE
using UnityEngine;

namespace Eraflo.UnityImportPackage.Networking.Backends
{
    /// <summary>
    /// Factory for Netcode backend.
    /// </summary>
    public class NetcodeBackendFactory : INetworkBackendFactory
    {
        public string Id => "netcode";
        public string DisplayName => "Unity Netcode for GameObjects";
        public bool IsAvailable => true;
        
        public bool OnInitialize()
        {
            // If NetworkManager.Singleton exists, initialize immediately
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                return NetworkManager.SetBackendById("netcode");
            }

            // Otherwise, defer initialization
            var waiter = new GameObject("[NetcodeInitWaiter]");
            waiter.AddComponent<NetcodeInitWaiter>();
            Object.DontDestroyOnLoad(waiter);
            return false; // Deferred
        }
        
        public INetworkBackend Create()
        {
            return new NetcodeBackend();
        }
    }

    /// <summary>
    /// Waits for Netcode NetworkManager.Singleton.
    /// </summary>
    internal class NetcodeInitWaiter : MonoBehaviour
    {
        private void Update()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                NetworkManager.SetBackendById("netcode");
                Destroy(gameObject);
            }
        }
    }
}
#endif
