# API Reference

Complete API documentation for the Networking module.

---

## Services

### NetworkManager

| Property | Type | Description |
|----------|------|-------------|
| `IsServer` | `bool` | True if hosting |
| `IsClient` | `bool` | True if client |
| `IsConnected` | `bool` | Network active |
| `IsHost` | `bool` | True if both server and client |
| `LocalClientId` | `ulong` | Local client ID |
| `ServerClientId` | `ulong` | Server's client ID |
| `Backend` | `INetworkBackend` | Current backend |
| `Router` | `NetworkMessageRouter` | Message router |

| Method | Description |
|--------|-------------|
| `Send<T>(msg, target, delivery?)` | Send message |
| `SendToClient<T>(clientId, msg, delivery?)` | Send to client |
| `SendToClients<T>(ids, msg, delivery?)` | Send to multiple |
| `On<T>(handler)` | Subscribe to message |
| `Off<T>(handler)` | Unsubscribe |
| `SetBackend(backend)` | Change backend |
| `SpawnPlayer(id, pos?, rot?)` | Spawn player (Server only) |

| Event | When |
|-------|------|
| `OnConnected` | Connection established |
| `OnDisconnected` | Connection lost |
| `OnClientConnected(clientId)` | Client joined (server) |
| `OnClientDisconnected(clientId)` | Client left (server) |

---

### LobbyManager

| Method | Description |
|--------|-------------|
| `CreateLobby(options)` | Host a lobby |
| `JoinLobby(joinCode, password?)` | Join lobby |
| `SearchLobbies(timeoutMs?)` | List active lobbies |
| `LeaveLobby()` | Disconnect |

| Event | When |
|-------|------|
| `OnLobbyJoined(info)` | Joined lobby |
| `OnLobbyLeft` | Left lobby |
| `OnJoinFailed(reason)` | Join failed |

---

### ConnectionManager

| Property | Type | Description |
|----------|------|-------------|
| `SecurityConfig` | `ConnectionSecurityConfig` | Security settings |

| Method | Description |
|--------|-------------|
| `SetPayload<T>(data)` | Set connection payload |
| `SetRawPayload(bytes)` | Set raw payload |
| `GetLocalPayload()` | Get current payload |

| Event | When |
|-------|------|
| `OnValidateConnection(request) → response` | Validate client (server) |
| `OnPayloadChanged(bytes)` | Payload updated |

---

### NetworkIdManager

| Method | Description |
|--------|-------------|
| Method | Description |
|--------|-------------|
| `Register(id, obj)` | Map ID to instance |
| `Unregister(obj)` | Remove by instance |
| `UnregisterId(id)` | Remove by ID |
| `GetId(obj)` | Get ID of instance |
| `GetObject<T>(id)` | Get instance of type T |
| `Clear()` | Reset registry |

---

### NetworkOwnershipManager

| Method | Description |
|--------|-------------|
| `SetOwner(objId, clientId)` | Assign owner (Server only) |
| `GetOwner(objId)` | Get owner |
| `IsOwner(objId, clientId)` | Check ownership |
| `RemoveOwner(objId)` | Clear ownership tracking |

---

### NetworkSpawnManager

| Method | Description |
|--------|-------------|
| `SpawnPlayerForClient(id, payload?)` | Spawn player for client |
| `DespawnPlayer(id)` | Despawns client player |
| `RegisterSpawnPoint(point)` | Add point |
| `UnregisterSpawnPoint(point)` | Remove point |
| `RefreshSpawnPoints()` | Find all points in scene |

---

### VoiceManager

| Property | Type | Description |
|----------|------|-------------|
| `IsAvailable` | `bool` | True if initialized |
| `IsInChannel` | `bool` | True if in a channel |
| `IsSpeaking` | `bool` | Local user is talking |
| `IsMuted` | `bool` | Local user is muted |
| `CurrentChannel` | `string` | Current name or null |
| `MicrophoneVolume` | `float` | Mic gain (0-1) |
| `SpeakerVolume` | `float` | Output volume (0-1) |
| `MasterVolume` | `float` | Alias for SpeakerVolume |

| Method | Description |
|--------|-------------|
| `SetProvider(provider)` | Set voice backend |
| `JoinChannel(name?, 3d?)` | Join voice channel |
| `LeaveChannel()` | Stop voice |
| `SetMuted(muted)` | Local mute |
| `SetMicEnabled(enabled)` | Enable/Disable mic |
| `ToggleMute()` | Quick toggle |
| `SetPlayerMuted(id, muted)` | Mute specific player |
| `UpdateListenerPosition(pos)` | Update 3D audio |

---

### NetworkDiagnostics

| Property | Description |
|----------|-------------|
| `RTT` | Round Trip Time (ms) |
| `PacketLoss` | Loss percentage (0-100) |
| `BandwidthIn` | Inbound speed (KB/s) |
| `BandwidthOut` | Outbound speed (KB/s) |
| `IsSimulationActive` | True if simulating |

| Method | Description |
|--------|-------------|
| `SetSimulation(lat, loss, jit)` | Set parameters |
| `DisableSimulation()` | Reset to zero |
| `GetMetricsString()` | Formatted summary |

---

### NetworkDiscovery

| Property | Description |
|----------|-------------|
| `IsAdvertising` | True if server is broadcast |
| `IsScanning` | True if looking for servers |

| Method | Description |
|--------|-------------|
| `SetProvider(provider)` | Set discovery backend |
| `StartAdvertising(info)`| Start broadcasting |
| `StopAdvertising()` | Stop broadcast |
| `StartScanning()` | Search for servers |
| `StopScanning()` | Stop search |
| `OnServerFound` | Event: `Action<DiscoveryInfo>` |

---

### NetworkActionManager

| Method | Description |
|--------|-------------|
| `RegisterAction(id, callback)` | Register handler |
| `UnregisterAction(id)` | Remove handler |
| `Trigger(id, ...args)` | Call on others |
| `TriggerToTarget(id, target, ...args)` | Call on target |
| `HasAction(id)` | Check registration |
| `ClearAllActions()` | Reset all |

---

### NetworkCullingManager

| Method | Description |
|--------|-------------|
| `RegisterCullable(obj)` | Start tracking |
| `UnregisterCullable(obj)` | Stop tracking |
| `UpdateCullablePosition(obj)`| Update spatial index |
| `RegisterCullingArea(id, area)`| Client camera/area |
| `UpdateCulling()` | Step visibility (Server) |
| `GetVisibleObjects(id)` | Set of visible IDs |

---

### NetworkAttachmentManager

| Method | Description |
|--------|-------------|
| `RequestAttach(child, parent, pos?, rot?)` | Network parenting |
| `RequestDetach(child, inheritVel?)` | Unparent |
| `IsAttached(child)` | Check status |
| `TryGetParent(child, out parentId)` | Get parent ID |

---

## Interfaces

### IVoiceProvider

```csharp
public interface IVoiceProvider
{
    // Properties
    bool IsInitialized { get; }
    bool IsInChannel { get; }
    string CurrentChannel { get; }
    bool IsMuted { get; }
    bool IsSpeaking { get; }

    // Events
    event Action<bool> OnSpeakingStateChanged;
    event Action<ulong, bool> OnRemoteSpeakingChanged;
    event Action<bool, string> OnChannelJoined;
    event Action OnChannelLeft;

    // Methods
    void Initialize();
    void Shutdown();
    void JoinChannel(string name, bool is3D = false);
    void LeaveChannel();
    void SetMuted(bool muted);
    void SetMicEnabled(bool enabled);
    void SetMicrophoneVolume(float volume);
    void SetSpeakerVolume(float volume);
    void UpdateListenerPosition(Vector3 pos, Vector3 fwd, Vector3 up);
    void SetParticipantMuted(ulong id, bool muted);
}
```

### INetworkBackend

```csharp
public interface INetworkBackend
{
    bool IsServer { get; }
    bool IsClient { get; }
    bool IsConnected { get; }
    ulong LocalClientId { get; }
    ulong ServerClientId { get; }
    bool SupportsNativeGameObjectReplication { get; }
    IDiscoveryProvider DiscoveryProvider { get; }

    void Send(ushort type, byte[] data, NetworkTarget target, NetworkDelivery delivery);
    void SendToClient(ushort type, byte[] data, ulong id, NetworkDelivery delivery);
    void SendToClients(ushort type, byte[] data, ulong[] ids, NetworkDelivery delivery);
    void RegisterHandler(ushort type, Action<byte[], ulong> handler);
    void UnregisterHandler(ushort type);
    GameObject SpawnPlayer(ulong id, Vector3? pos, Quaternion? rot);
    ulong GetOwner(GameObject go);
}
```

### INetworkMessage

```csharp
public interface INetworkMessage
{
    void Serialize(BinaryWriter writer);
    void Deserialize(BinaryReader reader);
}
```

### IDiscoveryTransport

```csharp
public interface IDiscoveryTransport : IDisposable
{
    string Name { get; }
    Task InitializeAsync(CancellationToken ct);
    Task SendBroadcastAsync(byte[] data, CancellationToken ct);
    Task ListenAsync(Action<byte[], string> onReceived, CancellationToken ct);
}
```

### ILobbyProvider

```csharp
public interface ILobbyProvider
{
    string Name { get; }
    Task<LobbyResult> CreateLobby(LobbyOptions options, CancellationToken ct);
    Task<LobbyResult> JoinLobby(string joinCode, string password, CancellationToken ct);
    Task<List<LobbyInfo>> SearchLobbies(int timeoutMs, CancellationToken ct);
    Task LeaveLobby();
    void Shutdown();
}
```

---

## Enums

### NetworkTarget

| Value | Description |
|-------|-------------|
| `Server` | To server |
| `Clients` | All clients |
| `All` | Everyone including self |
| `Others` | Everyone except self |

### NetworkDelivery

| Value | Guaranteed | Ordered |
|-------|------------|---------|
| `Reliable` | ✅ | ✅ |
| `ReliableSequenced` | ✅ | ✅ |
| `ReliableFragmented` | ✅ | ❌ |
| `Unreliable` | ❌ | ❌ |
| `UnreliableSequenced` | ❌ | ✅ |

### Spawn Strategies (Classes)
Implementing `ISpawnStrategy`:

- `RandomSpawnStrategy`
- `RoundRobinSpawnStrategy`
- `TeamBasedSpawnStrategy`
- `FurthestFromEnemiesStrategy`

### DiscoveryTransportType

| Value | Description |
|-------|-------------|
| `UdpBroadcast` | LAN broadcast |
| `WebSocket` | Relay server |
| `Mock` | Testing |

---

## Attributes

### RateLimitAttribute

```csharp
[RateLimit(maxMessages: 10, windowSeconds: 1.0f, Action = RateLimitAction.Warn)]
```

| Action | Behavior |
|--------|----------|
| `Reject` | Drop silently |
| `Warn` | Log + drop |
| `Disconnect` | Boot client |

### ValidateMessageAttribute

```csharp
[ValidateMessage(RejectInvalid = true)]
public struct MyMessage : INetworkMessage { ... }
```

| Property | Description |
|----------|-------------|
| `RejectInvalid` | If true, invalid messages are dropped |

### MaxLengthAttribute

```csharp
[MaxLength(64)]  // Max string length
public string Name;
```

### MaxSizeAttribute

```csharp
[MaxSize(1024)]  // Max array size
public byte[] Data;
```

---

## Structs

### LobbyOptions

| Property | Type | Default |
|----------|------|---------|
| `Name` | `string` | Required |
| `MaxPlayers` | `int` | Required |
| `Password` | `string` | null |
| `IsDedicatedServer` | `bool` | false |
| `Metadata` | `Dictionary<string,string>` | null |

### DiscoveryInfo

| Property | Type |
|----------|------|
| `Name` | `string` |
| `Address` | `string` |
| `PlayerCount` | `int` |
| `MaxPlayers` | `int` |
| `IsPasswordProtected` | `bool` |
| `Metadata` | `Dictionary<string,string>` |

### ConnectionRequest

| Property | Type |
|----------|------|
| `ClientId` | `ulong` |
| `Payload` | `byte[]` |
| `GetPayload<T>()` | `T` |

### ConnectionResponse

| Factory | Parameters |
|---------|------------|
| `Success()` | Default spawn |
| `Success(pos, rot)` | Custom spawn |
| `Reject(reason)` | Rejection |

---

## Next

- [Tutorials](./13-Tutorials.md) - Step-by-step guides
