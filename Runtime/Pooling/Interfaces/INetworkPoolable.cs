namespace Eraflo.Catalyst.Pooling
{
    /// <summary>
    /// Interface for objects that need custom logic when spawned or despawned via network.
    /// Works for both GameObjects and C# classes.
    /// </summary>
    public interface INetworkPoolable
    {
        /// <summary>Called when the object is spawned via the network.</summary>
        /// <param name="spawnData">Custom data sent from the server.</param>
        void OnNetworkSpawn(byte[] spawnData);

        /// <summary>Called when the object is despawned via the network.</summary>
        void OnNetworkDespawn();
    }
}
