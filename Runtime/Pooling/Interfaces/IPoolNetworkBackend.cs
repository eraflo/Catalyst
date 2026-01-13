using UnityEngine;

namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Interface that a network backend must implement to support synchronized pooling.
    /// Owned by the Pooling module.
    /// </summary>
    public interface IPoolNetworkBackend
    {
        void SynchronizeInstance(GameObject instance, uint networkId);
    }
}
