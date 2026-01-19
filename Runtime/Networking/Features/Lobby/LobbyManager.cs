using System;
using System.Collections.Generic;
using System.Threading;
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
        public event Action<string> OnJoinFailed;

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

        public async Task<LobbyResult> CreateLobby(LobbyOptions options, CancellationToken ct = default)
        {
            // Validation
            if (_provider == null)
                return LobbyResult.Failure("No lobby provider set.");
            
            if (string.IsNullOrWhiteSpace(options.Name))
                return LobbyResult.Failure("Lobby name cannot be empty.");
            
            if (options.MaxPlayers <= 0)
                return LobbyResult.Failure("MaxPlayers must be greater than 0.");

            var result = await _provider.CreateLobby(options, ct);
            
            if (result.Success)
            {
                _currentLobby = result.Lobby;
                OnLobbyJoined?.Invoke(result.Lobby);
            }
            
            return result;
        }

        public async Task<LobbyResult> JoinLobby(string joinCode, string password = null, CancellationToken ct = default)
        {
            if (_provider == null)
                return LobbyResult.Failure("No lobby provider set.");

            var result = await _provider.JoinLobby(joinCode, password, ct);
            
            if (result.Success)
            {
                _currentLobby = result.Lobby;
                OnLobbyJoined?.Invoke(result.Lobby);
            }
            else
            {
                OnJoinFailed?.Invoke(result.Message);
            }
            
            return result;
        }

        public async Task<List<LobbyInfo>> SearchLobbies(int timeoutMs = -1, CancellationToken ct = default)
        {
            if (_provider == null) return new List<LobbyInfo>();
            
            int timeout = timeoutMs > 0 ? timeoutMs : PackageSettings.Instance.LobbySearchTimeoutMs;
            return await _provider.SearchLobbies(timeout, ct);
        }

        public async Task LeaveLobby()
        {
            if (_provider != null)
            {
                await _provider.LeaveLobby();
                _currentLobby = null;
                OnLobbyLeft?.Invoke();
            }
        }
    }
}
