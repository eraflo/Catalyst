namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for handlers that need to define an authority model.
    /// </summary>
    public interface IAuthoritativeHandler
    {
        /// <summary>
        /// Gets or sets the authority mode for this handler.
        /// </summary>
        AuthorityMode Authority { get; set; }
    }
}
