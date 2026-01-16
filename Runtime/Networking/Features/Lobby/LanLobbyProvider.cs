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
        private readonly object _lock = new object();
        private bool _isSearching;

        public LanLobbyProvider()
        {
            _discovery = App.Get<NetworkDiscovery>();
            _network = App.Get<NetworkManager>();
        }

        public Task<LobbyResult> CreateLobby(LobbyOptions options)
        {
            try
            {
                ushort port = options.Port > 0 ? options.Port : (ushort)7777;
                string bindAddress = string.IsNullOrEmpty(options.Address) ? "127.0.0.1" : options.Address;

                // 1. Start Host - Bind to specified address (or all interfaces)
                bool started = _network.StartHost(bindAddress, port);
                if (!started) return Task.FromResult(LobbyResult.Failure("Failed to start network host."));

                // 2. Start Advertising (only if not local-only)
                if (bindAddress != "127.0.0.1")
                {
                    _discovery.StartAdvertising(options.Name, port, 1, options.MaxPlayers);
                }

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
            // In LAN, joinCode is usually the IP address, possibly with a port (address:port)
            try
            {
                string address = joinCode;
                ushort port = 7777;

                if (joinCode.Contains(":"))
                {
                    string[] parts = joinCode.Split(':');
                    address = parts[0];
                    if (ushort.TryParse(parts[1], out ushort p))
                    {
                        port = p;
                    }
                }

                bool started = _network.StartClient(address, port);
                if (!started) return Task.FromResult(LobbyResult.Failure($"Failed to connect to {address}:{port}"));

                return Task.FromResult(LobbyResult.Ok(new LobbyInfo { Id = address, JoinCode = joinCode }));
            }
            catch (Exception e)
            {
                return Task.FromResult(LobbyResult.Failure(e.Message));
            }
        }

        public async Task<List<LobbyInfo>> SearchLobbies()
        {
            if (_isSearching) return new List<LobbyInfo>(_foundLobbies);

            _isSearching = true;
            try
            {
                lock (_lock)
                {
                    _foundLobbies.Clear();
                }

                _discovery.OnServerFound += HandleServerFound;
                _discovery.StartScanning();

                await Task.Delay(3500); // Pulse scan for 3.5s (Discovery heartbeats are 2s)

                _discovery.OnServerFound -= HandleServerFound;
                _discovery.StopScanning(); // Explicitly stop scanning after pulse

                lock (_lock)
                {
                    return new List<LobbyInfo>(_foundLobbies);
                }
            }
            finally
            {
                _isSearching = false;
            }
        }

        private void HandleServerFound(NetworkDiscovery.DiscoveryInfo info)
        {
            string id = $"{info.Address}:{info.Port}";

            lock (_lock)
            {
                if (_foundLobbies.Exists(l => l.Id == id)) return;

                _foundLobbies.Add(new LobbyInfo
                {
                    Id = id,
                    Name = info.Name,
                    JoinCode = id,
                    CurrentPlayers = info.CurrentPlayers,
                    MaxPlayers = info.MaxPlayers
                });
            }
        }

        public Task LeaveLobby()
        {
            _discovery.StopScanning();
            _discovery.StopAdvertising();
            _network.Stop();
            return Task.CompletedTask;
        }

        public void Shutdown()
        {
            _discovery.StopScanning();
            _discovery.StopAdvertising();
        }
    }
}
