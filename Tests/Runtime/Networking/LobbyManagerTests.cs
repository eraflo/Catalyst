using NUnit.Framework;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends.Mock;
using Eraflo.Catalyst.Networking.Features.Lobby;
using System.Threading.Tasks;

namespace Eraflo.Catalyst.Tests.Networking
{
    public class LobbyManagerTests
    {
        private LobbyManager _lobbyManager;

        [SetUp]
        public void SetUp()
        {
            App.Get<NetworkManager>().Reset();
            App.Get<NetworkManager>().Backends.Register(new MockBackendFactory());
            App.Get<NetworkManager>().SetBackendById("mock");
            
            _lobbyManager = App.Get<LobbyManager>();
        }

        [TearDown]
        public void TearDown()
        {
            App.Shutdown();
        }

        [Test]
        public async Task CreateLobby_ReturnsFailure_WhenNoProvider()
        {
            _lobbyManager.SetProvider(null);
            
            var result = await _lobbyManager.CreateLobby(new LobbyOptions { Name = "Test", MaxPlayers = 4 });
            
            Assert.IsFalse(result.Success);
            Assert.That(result.Message, Does.Contain("No lobby provider"));
        }

        [Test]
        public async Task CreateLobby_ReturnsFailure_WhenNameEmpty()
        {
            _lobbyManager.SetProvider(new LanLobbyProvider());
            
            var result = await _lobbyManager.CreateLobby(new LobbyOptions { Name = "", MaxPlayers = 4 });
            
            Assert.IsFalse(result.Success);
            Assert.That(result.Message, Does.Contain("name"));
        }

        [Test]
        public async Task CreateLobby_ReturnsFailure_WhenMaxPlayersZero()
        {
            _lobbyManager.SetProvider(new LanLobbyProvider());
            
            var result = await _lobbyManager.CreateLobby(new LobbyOptions { Name = "Test", MaxPlayers = 0 });
            
            Assert.IsFalse(result.Success);
            Assert.That(result.Message, Does.Contain("MaxPlayers"));
        }

        [Test]
        public async Task JoinLobby_ReturnsFailure_WhenNoProvider()
        {
            _lobbyManager.SetProvider(null);
            
            var result = await _lobbyManager.JoinLobby("127.0.0.1:7777");
            
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void HasProvider_ReturnsFalse_WhenNoProviderSet()
        {
            _lobbyManager.SetProvider(null);
            Assert.IsFalse(_lobbyManager.HasProvider);
        }

        [Test]
        public void HasProvider_ReturnsTrue_WhenProviderSet()
        {
            _lobbyManager.SetProvider(new LanLobbyProvider());
            Assert.IsTrue(_lobbyManager.HasProvider);
        }
    }
}
