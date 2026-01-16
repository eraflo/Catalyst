using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Security;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Lobby
{
    /// <summary>
    /// Lobby provider for LAN games using IDiscoveryProvider.
    /// Supports password protection and dedicated server mode.
    /// </summary>
    public class LanLobbyProvider : ILobbyProvider
    {
        public string Name => "LAN";

        private readonly NetworkDiscovery _discovery;
        private readonly NetworkManager _network;
        private readonly List<LobbyInfo> _foundLobbies = new List<LobbyInfo>();
        private readonly object _lock = new object();
        private volatile bool _isSearching;
        private string _passwordHash;

        public LanLobbyProvider()
        {
            _discovery = App.Get<NetworkDiscovery>();
            _network = App.Get<NetworkManager>();
        }

        public Task<LobbyResult> CreateLobby(LobbyOptions options, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                
                ushort port = options.Port > 0 ? options.Port : (ushort)7777;
                string bindAddress = string.IsNullOrEmpty(options.Address) ? "0.0.0.0" : options.Address;
                bool hasPassword = !string.IsNullOrEmpty(options.Password);

                // Store password hash for validation
                if (hasPassword)
                {
                    var security = App.Get<SecurityManager>();
                    _passwordHash = security.Hash.HashToHex(options.Password);
                    RegisterPasswordValidation();
                }
                else
                {
                    _passwordHash = null;
                }

                // Start as dedicated server or host
                bool started;
                if (options.IsDedicatedServer)
                {
                    started = _network.StartServer(bindAddress, port);
                }
                else
                {
                    started = _network.StartHost(bindAddress, port);
                }

                if (!started)
                    return Task.FromResult(LobbyResult.Failure("Failed to start network."));

                // Advertise with password flag
                var discoveryInfo = new DiscoveryInfo
                {
                    Name = options.Name,
                    Port = port,
                    CurrentPlayers = options.IsDedicatedServer ? 0 : 1,
                    MaxPlayers = options.MaxPlayers,
                    IsPasswordProtected = hasPassword
                };
                _discovery.StartAdvertising(discoveryInfo);

                var lobbyInfo = new LobbyInfo
                {
                    Id = "lan_host",
                    Name = options.Name,
                    CurrentPlayers = options.IsDedicatedServer ? 0 : 1,
                    MaxPlayers = options.MaxPlayers,
                    JoinCode = $"{bindAddress}:{port}",
                    IsPasswordProtected = hasPassword
                };

                return Task.FromResult(LobbyResult.Ok(lobbyInfo));
            }
            catch (OperationCanceledException)
            {
                return Task.FromResult(LobbyResult.Failure("Operation cancelled."));
            }
            catch (Exception e)
            {
                return Task.FromResult(LobbyResult.Failure(e.Message));
            }
        }

        public Task<LobbyResult> JoinLobby(string joinCode, string password = null, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                
                string address = joinCode;
                ushort port = 7777;

                if (joinCode.Contains(":"))
                {
                    string[] parts = joinCode.Split(':');
                    address = parts[0];
                    if (ushort.TryParse(parts[1], out ushort p))
                        port = p;
                }

                // Set password in connection payload
                if (!string.IsNullOrEmpty(password))
                {
                    var security = App.Get<SecurityManager>();
                    var connectionMgr = App.Get<ConnectionManager>();
                    connectionMgr.SetPayload(new PasswordPayload
                    {
                        PasswordHash = security.Hash.HashToHex(password)
                    });
                }

                bool started = _network.StartClient(address, port);
                if (!started)
                    return Task.FromResult(LobbyResult.Failure($"Failed to connect to {address}:{port}"));

                return Task.FromResult(LobbyResult.Ok(new LobbyInfo
                {
                    Id = joinCode,
                    JoinCode = joinCode
                }));
            }
            catch (OperationCanceledException)
            {
                return Task.FromResult(LobbyResult.Failure("Operation cancelled."));
            }
            catch (Exception e)
            {
                return Task.FromResult(LobbyResult.Failure(e.Message));
            }
        }

        public async Task<List<LobbyInfo>> SearchLobbies(int timeoutMs = -1, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_isSearching)
                    return new List<LobbyInfo>(_foundLobbies);
                _isSearching = true;
                _foundLobbies.Clear();
            }

            try
            {
                _discovery.OnServerFound += HandleServerFound;
                _discovery.StartScanning();

                int timeout = timeoutMs > 0 ? timeoutMs : 3500;
                await Task.Delay(timeout, ct);
            }
            catch (OperationCanceledException)
            {
                // Cancelled, return what we have
            }
            finally
            {
                _discovery.OnServerFound -= HandleServerFound;
                _discovery.StopScanning();
                
                lock (_lock)
                {
                    _isSearching = false;
                }
            }

            lock (_lock)
            {
                return new List<LobbyInfo>(_foundLobbies);
            }
        }

        private void HandleServerFound(DiscoveryInfo info)
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
                    MaxPlayers = info.MaxPlayers,
                    IsPasswordProtected = info.IsPasswordProtected
                });
            }
        }

        public Task LeaveLobby()
        {
            _discovery.StopScanning();
            _discovery.StopAdvertising();
            _network.Stop();
            _passwordHash = null;
            return Task.CompletedTask;
        }

        public void Shutdown()
        {
            _discovery.StopScanning();
            _discovery.StopAdvertising();
            _passwordHash = null;
        }

        private void RegisterPasswordValidation()
        {
            var connectionMgr = App.Get<ConnectionManager>();
            connectionMgr.OnValidateConnection += ValidatePassword;
        }

        private ConnectionResponse ValidatePassword(ConnectionRequest request)
        {
            if (string.IsNullOrEmpty(_passwordHash))
                return ConnectionResponse.Success();

            try
            {
                var payload = NetworkSerializer.DeserializeValue<PasswordPayload>(request.Payload);
                
                if (payload.PasswordHash == _passwordHash)
                    return ConnectionResponse.Success();
                
                return ConnectionResponse.Reject("Invalid password.");
            }
            catch
            {
                return ConnectionResponse.Reject("Invalid password format.");
            }
        }

        /// <summary>Password payload for connection validation.</summary>
        [Serializable]
        private struct PasswordPayload
        {
            public string PasswordHash;
        }
    }
}
