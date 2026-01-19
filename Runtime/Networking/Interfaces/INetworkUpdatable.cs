namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for message handlers that require periodic updates.
    /// </summary>
    public interface INetworkUpdatable : INetworkMessageHandler
{
    /// <summary>
    /// Called during the NetworkManager update loop.
    /// </summary>
    void OnUpdate();
}
}
