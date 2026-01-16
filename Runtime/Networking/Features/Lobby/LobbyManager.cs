using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Lobby
{
    /// <summary>
    /// Service for managing game lobbies.
    /// Delegates actual lobby logic to an ILobbyProvider.
    /// </summary>
    [Service(Priority = 6)]
    public class LobbyManager : IGameService
    {
        private ILobbyProvider _provider;
        private LobbyInfo? _currentLobby;

        public LobbyInfo? Lobby => _currentLobby;
        public bool HasProvider => _provider != null;

        public event Action<LobbyInfo> OnLobbyJoined;
        public event Action OnLobbyLeft;

        public void Initialize() { }
        public void Shutdown()
        {
            _provider?.Shutdown();
            _provider = null;
        }

        public void SetProvider(ILobbyProvider provider)
        {
            _provider = provider;
            Debug.Log($"[LobbyManager] Provider set to: {provider?.Name ?? "none"}");
        }

        public async Task<LobbyResult> CreateLobby(LobbyOptions options)
        {
            if (_provider == null) return LobbyResult.Failure("No lobby provider set.");

            var result = await _provider.CreateLobby(options);
            if (result.Success)
            {
                _currentLobby = result.Lobby;
                OnLobbyJoined?.Invoke(result.Lobby);
            }
            return result;
        }

        public async Task<LobbyResult> JoinLobby(string joinCode)
        {
            if (_provider == null) return LobbyResult.Failure("No lobby provider set.");

            var result = await _provider.JoinLobby(joinCode);
            if (result.Success)
            {
                _currentLobby = result.Lobby;
                OnLobbyJoined?.Invoke(result.Lobby);
            }
            return result;
        }

        public async Task<List<LobbyInfo>> SearchLobbies()
        {
            if (_provider == null) return new List<LobbyInfo>();
            return await _provider.SearchLobbies();
        }

        public async Task LeaveLobby()
        {
            if (_provider != null)
            {
                await _provider.LeaveLobby();
                OnLobbyLeft?.Invoke();
            }
        }
    }
}
