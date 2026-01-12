namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Defines who has the authority over a particular game logic.
    /// </summary>
    public enum AuthorityMode
    {
        /// <summary>
        /// The client's logic is trusted. The server relays the result to other clients.
        /// </summary>
        ClientAuthoritative,

        /// <summary>
        /// The server validates all logic. Clients only send raw inputs or requests.
        /// </summary>
        ServerAuthoritative
    }
}
