using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Interface for lobby providers (LAN, Relay, Steam, etc.).
    /// </summary>
    public interface ILobbyProvider
    {
        /// <summary>Provider name.</summary>
        string Name { get; }
        
        /// <summary>Creates a new lobby.</summary>
        Task<LobbyResult> CreateLobby(LobbyOptions options, CancellationToken ct = default);
        
        /// <summary>Joins an existing lobby.</summary>
        Task<LobbyResult> JoinLobby(string joinCode, string password = null, CancellationToken ct = default);
        
        /// <summary>Searches for available lobbies.</summary>
        Task<List<LobbyInfo>> SearchLobbies(int timeoutMs = -1, CancellationToken ct = default);
        
        /// <summary>Leaves the current lobby.</summary>
        Task LeaveLobby();
        
        /// <summary>Cleanup resources.</summary>
        void Shutdown();
    }

    /// <summary>
    /// Options for creating a lobby.
    /// </summary>
    public struct LobbyOptions
    {
        public string Name;
        public string Address;
        public int MaxPlayers;
        public ushort Port;
        public bool IsPrivate;
        public bool IsDedicatedServer;
        public string Password;
        public Dictionary<string, string> Metadata;
    }

    /// <summary>
    /// Information about a lobby.
    /// </summary>
    public struct LobbyInfo
    {
        public string Id;
        public string Name;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string JoinCode;
        public bool IsPasswordProtected;
    }

    /// <summary>
    /// Result of a lobby operation.
    /// </summary>
    public struct LobbyResult
    {
        public bool Success;
        public string Message;
        public LobbyInfo Lobby;

        public static LobbyResult Failure(string msg) => new LobbyResult { Success = false, Message = msg };
        public static LobbyResult Ok(LobbyInfo info) => new LobbyResult { Success = true, Lobby = info };
    }
}
