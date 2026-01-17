# Discovery & Lobbies

Finding and joining multiplayer games.

---

## Architecture

```mermaid
flowchart TB
    subgraph "Discovery Stack"
        LM[LobbyManager] --> LP[ILobbyProvider]
        LP --> DP[IDiscoveryProvider]
        DP --> DT[IDiscoveryTransport]
    end
    
    subgraph "Transports"
        DT --> UDP[UdpBroadcast]
        DT --> WS[WebSocket]
        DT --> MOCK[Mock]
    end
```

---

## Discovery vs Lobbies

While both help players find games, they serve different purposes:

| Feature | **NetworkDiscovery** | **LobbyManager** |
|---------|----------------------|------------------|
| **Scope** | Local Area Network (LAN) | Wide Area Network (Internet) |
| **Backend** | UDP Broadcast | Steam, PlayFab, Catalyst Relay |
| **Complexity**| Zero configuration | Requires external service account |
| **Features** | Simple broadcast | Metadata, sorting, passwords |

---

## 🔍 Server Discovery (LAN)

The `NetworkDiscovery` service is the easiest way to support LAN play. It uses UDP broadcasting to find servers on the same network.

### Basic Usage

```csharp
var discovery = App.Get<NetworkDiscovery>();

// 1. Host side: Start advertising
discovery.StartAdvertising("Killer Server", 7777);

// 2. Client side: Start scanning
discovery.OnServerFound += (info) =>
{
    Debug.Log($"Found server: {info.Name} at {info.Address}");
    // Connect using the address
    App.Get<NetworkManager>().Connect(info.Address);
};

discovery.StartScanning();
```

---

## Transport Types

| Transport | Use Case | Requirements |
|-----------|----------|--------------|
| `UdpBroadcast` | LAN games | Same network/subnet |
| `WebSocket` | Internet | Relay server URL |
| `Mock` | Testing | None |

### Configuring in PackageSettings

```
Tools > Catalyst > Settings

Discovery Transport:
├── Transport Type: [UdpBroadcast ▼]
├── Discovery Port: 47777
└── Relay URL: (for WebSocket only)
```

### Programmatic Override

```csharp
// Override transport per-instance
var transport = new WebSocketDiscoveryTransport("wss://relay.example.com");
var provider = new LanDiscoveryProvider(transport);

// Or use factory
var transport = DiscoveryTransportFactory.Create(DiscoveryTransportType.WebSocket);
```

---

## UDP Broadcast (LAN) Architecture

How it works:

```mermaid
sequenceDiagram
    participant H as Host
    participant N as Network (Broadcast)
    participant C as Client
    
    H->>N: Broadcast server info
    N->>C: Receive broadcast
    C->>C: OnServerFound event
    
    Note over H,C: Repeats every second
```

**Limitations:**
- Same network only
- May be blocked by firewalls
- Not for internet play

---

## WebSocket Relay (Internet)

For internet matchmaking:

```mermaid
sequenceDiagram
    participant H as Host
    participant R as Relay Server
    participant C as Client
    
    H->>R: Connect & advertise
    R->>R: Store server info
    C->>R: Connect & request list
    R->>C: Send server list
    C->>C: OnServerFound events
```

**Requirements:**
- WebSocket relay server
- URL configured in settings (e.g., `wss://relay.example.com`)

---

## LobbyManager (Internet)

### Creating a Lobby

```csharp
var lobby = App.Get<LobbyManager>();

await lobby.CreateLobby(new LobbyOptions
{
    Name = "My Awesome Game",
    MaxPlayers = 4,
    Password = null,              // No password
    IsDedicatedServer = false     // Host plays too
});
```

### With Password

```csharp
await lobby.CreateLobby(new LobbyOptions
{
    Name = "Private Game",
    MaxPlayers = 4,
    Password = "secret123"  // Hashed automatically
});
```

---

## Finding Lobbies

### Search

```csharp
// Search for available lobbies
List<LobbyInfo> lobbies = await lobby.SearchLobbies();
foreach (var info in lobbies)
{
    Debug.Log($"Found lobby: {info.Name} ({info.CurrentPlayers}/{info.MaxPlayers})");
}
```

### Handle Results

```csharp
private void JoinLobby(LobbyInfo info)
{
    Debug.Log($"Joining lobby: {info.Name} with code: {info.JoinCode}");
    _lobby.JoinLobby(info.JoinCode);
}
```

### LobbyInfo Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Name` | `string` | Display name |
| `CurrentPlayers`| `int` | Connected players |
| `MaxPlayers` | `int` | Maximum capacity |
| `JoinCode` | `string` | Unique join code |
| `IsPasswordProtected` | `bool` | Requires password |

---

## Joining Lobbies

### Without Password

```csharp
var result = await lobby.JoinLobby(joinCode);

if (result.Success)
{
    Debug.Log("Connected!");
}
else
{
    Debug.LogError($"Failed: {result.Error}");
}
```

---

## Complete Example

```csharp
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Transform _serverListContent;
    [SerializeField] private GameObject _serverItemPrefab;

    private LobbyManager _lobby;
    private List<LobbyInfo> _lobbies = new();

    void Start()
    {
        _lobby = App.Get<LobbyManager>();
    }

    public async void OnHostClicked()
    {
        await _lobby.CreateLobby(new LobbyOptions
        {
            Name = "My Game",
            MaxPlayers = 4
        });
    }

    public async void OnRefreshClicked()
    {
        ClearServerList();
        var results = await _lobby.SearchLobbies();
        foreach (var l in results) AddLobby(l);
    }

    private void AddLobby(LobbyInfo info)
    {
        _lobbies.Add(info);
        var item = Instantiate(_serverItemPrefab, _serverListContent);
        item.GetComponent<ServerListItem>().Setup(info);
    }

    private void ClearServerList()
    {
        _lobbies.Clear();
        foreach (Transform child in _serverListContent)
            Destroy(child.gameObject);
    }
}
```

---

## Next

- [Connection Security](./07-ConnectionSecurity.md) - Secure connections
