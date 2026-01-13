namespace Eraflo.Catalyst.Networking.Features.Connection
{
    /// <summary>
    /// Interface that a network backend must implement to support custom connection approval.
    /// Owned by the Connection module.
    /// </summary>
    public interface IConnectionBackend
    {
        void Initialize();
    }
}
