using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eraflo.Catalyst.Networking
{
    public interface ILobbyProvider
    {
        string Name { get; }
        Task<LobbyResult> CreateLobby(LobbyOptions options);
        Task<LobbyResult> JoinLobby(string joinCode);
        Task<List<LobbyInfo>> SearchLobbies();
        Task LeaveLobby();
        void Shutdown();
    }

    public struct LobbyOptions
    {
        public string Name;
        public string Address;
        public int MaxPlayers;
        public ushort Port;
        public bool IsPrivate;
        public Dictionary<string, string> Metadata;
    }

    public struct LobbyInfo
    {
        public string Id;
        public string Name;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string JoinCode;
    }

    public struct LobbyResult
    {
        public bool Success;
        public string Message;
        public LobbyInfo Lobby;

        public static LobbyResult Failure(string msg) => new LobbyResult { Success = false, Message = msg };
        public static LobbyResult Ok(LobbyInfo info) => new LobbyResult { Success = true, Lobby = info };
    }
}
