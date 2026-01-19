# Package Settings

Central configuration for the package.

## Location

```
Assets/Resources/CatalystSettings.asset
```

**Menu**: Tools > Catalyst > Settings

---

## Settings Reference

### Global

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Thread Mode** | `PackageThreadMode` | `SingleThread` | Thread safety mode |

---

### Networking

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Network Backend ID** | `string` | *(empty)* | Backend to use |
| **Network Debug Mode** | `bool` | `false` | Log debug messages |
| **Default Authority** | `AuthorityMode` | `ServerAuthoritative` | Global authority model |
| **Handler Mode** | `NetworkHandlerMode` | `Auto` | Handler registration |
| **Allow Port Sharing** | `bool` | `true` | Share discovery port |

#### Backend IDs

| ID | Description |
|----|-------------|
| *(empty)* | Disabled |
| `mock` | Testing backend |
| `netcode` | Unity Netcode for GameObjects |

---

### Discovery Transport

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Transport Type** | `DiscoveryTransportType` | `UdpBroadcast` | Transport for discovery |
| **Relay URL** | `string` | *(empty)* | WebSocket relay URL |
| **Discovery Port** | `int` | `47777` | UDP broadcast port |
| **Max Message Size** | `int` | `512` | Security: max packet size |
| **Max Name Length** | `int` | `64` | Security: max lobby name |
| **Rate Limit/sec** | `int` | `10` | Security: max packets/IP/sec |

#### Transport Types

| Type | Use Case |
|------|----------|
| `UdpBroadcast` | LAN discovery |
| `WebSocket` | Internet via relay |
| `Mock` | Unit testing |

---

### Lobby

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Search Timeout (ms)** | `int` | `3500` | Lobby search duration |
| **Enable Passwords** | `bool` | `true` | Allow password lobbies |

---

### Network Simulation (Editor Only)

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Simulate Latency (ms)** | `int` | `0` | Artificial latency |
| **Simulate Loss (%)** | `float` | `0` | Packet loss simulation |
| **Simulate Jitter (ms)** | `int` | `0` | Jitter simulation |

---

### Timer System

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Use Burst Timers** | `bool` | `false` | Optimized backend |
| **Enable Debug Logs** | `bool` | `false` | Log timer events |
| **Enable Debug Overlay** | `bool` | `false` | Show overlay |

---

### Assets

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Provider Type** | `AssetProviderType` | `Resources` | `Resources` or `Addressables` |

---

### Input

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Input Provider** | `InputProviderType` | `Legacy` | `Legacy` or `InputSystem` |
| **Action Asset** | `InputActionAsset` | `null` | For New Input System |
| **Enable Debugger** | `bool` | `false` | Show input buffer |

---

### Scene Flow

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **On Transition Started** | `SceneTransitionChannel` | `null` | Event channel |
| **On Transition Completed** | `SceneTransitionChannel` | `null` | Event channel |

---

### Persistence

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Settings Filename** | `string` | `"settings.json"` | Persistence file |

---

## Code Access

```csharp
var settings = PackageSettings.Instance;

// Networking
settings.NetworkBackendId
settings.EnableNetworking
settings.DefaultAuthorityMode

// Discovery
settings.DiscoveryTransportType
settings.DiscoveryRelayUrl
settings.DiscoveryPort

// Lobby
settings.LobbySearchTimeoutMs
settings.EnableRoomPasswords
```

## Reload Settings

```csharp
PackageSettings.Reload();
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No settings found" | Create via Tools > Catalyst > Settings |
| Network not working | Check `NetworkBackendId` |
| Backend not found | Register factory first |
| Discovery fails | Check firewall for port 47777 |
