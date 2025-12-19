namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Target recipients for network messages.
    /// </summary>
    public enum NetworkTarget
    {
        /// <summary>Send to everyone including self.</summary>
        All,
        
        /// <summary>Send to everyone except self.</summary>
        Others,
        
        /// <summary>Send to server only (from client).</summary>
        Server,
        
        /// <summary>Send to all clients (from server).</summary>
        Clients
    }
}
