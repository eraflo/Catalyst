using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Lobby
{
    /// <summary>
    /// Lobby provider implementation for LAN games using UDP discovery.
    /// </summary>
    public class LanLobbyProvider : ILobbyProvider
    {
        public string Name => "LAN";
        
        private readonly NetworkDiscovery _discovery;
        private readonly NetworkManager _network;
        private readonly List<LobbyInfo> _foundLobbies = new List<LobbyInfo>();

        public LanLobbyProvider()
        {
            _discovery = App.Get<NetworkDiscovery>();
            _network = App.Get<NetworkManager>();
        }

        public Task<LobbyResult> CreateLobby(LobbyOptions options)
        {
            try
            {
                // 1. Start Host
                bool started = _network.StartHost(7777); 
                if (!started) return Task.FromResult(LobbyResult.Failure("Failed to start network host."));

                // 2. Start Advertising
                _discovery.StartAdvertising(options.Name, 7777);

                var info = new LobbyInfo
                {
                    Id = "lan_host",
                    Name = options.Name,
                    CurrentPlayers = 1,
                    MaxPlayers = options.MaxPlayers,
                    JoinCode = "local"
                };

                return Task.FromResult(LobbyResult.Ok(info));
            }
            catch (Exception e)
            {
                return Task.FromResult(LobbyResult.Failure(e.Message));
            }
        }

        public Task<LobbyResult> JoinLobby(string joinCode)
        {
            // In LAN, joinCode is usually the IP address
            try
            {
                bool started = _network.StartClient(joinCode, 7777);
                if (!started) return Task.FromResult(LobbyResult.Failure($"Failed to connect to {joinCode}"));

                return Task.FromResult(LobbyResult.Ok(new LobbyInfo { Id = joinCode, JoinCode = joinCode }));
            }
            catch (Exception e)
            {
                return Task.FromResult(LobbyResult.Failure(e.Message));
            }
        }

        public async Task<List<LobbyInfo>> SearchLobbies()
        {
            _foundLobbies.Clear();
            
            _discovery.OnServerFound += HandleServerFound;
            _discovery.StartScanning();
            
            await Task.Delay(1500); // Pulse scan for 1.5s
            
            _discovery.OnServerFound -= HandleServerFound;
            _discovery.StopAll(); // Explicitly stop scanning after pulse
            
            return new List<LobbyInfo>(_foundLobbies);
        }

        private void HandleServerFound(NetworkDiscovery.DiscoveryInfo info)
        {
            if (_foundLobbies.Exists(l => l.Id == info.Address)) return;
            
            _foundLobbies.Add(new LobbyInfo
            {
                Id = info.Address,
                Name = info.Name,
                JoinCode = info.Address,
                CurrentPlayers = 0, // Discovery doesn't provide this yet
                MaxPlayers = 0
            });
        }

        public Task LeaveLobby()
        {
            _discovery.StopAll();
            _network.Stop();
            return Task.CompletedTask;
        }

        public void Shutdown()
        {
            _discovery.StopAll();
        }
    }
}
