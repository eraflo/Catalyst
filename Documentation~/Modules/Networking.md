# Networking System

Unified, extensible networking abstraction with auto-registration.

## Architecture

```mermaid
graph TB
    subgraph "Service Locator"
        SL["ServiceLocator / App"]
    end

    subgraph "Bootstrap"
        NB[NetworkBootstrapper]
        NB --> |Register| BR
        NB --> |Register| HR
    end

    subgraph "NetworkManager Service"
        NM[NetworkManager]
        NM --> BR[Backends Registry]
        NM --> MR[Message Router]
        NM --> HR[Handler Registry]
    end

    subgraph "Backends"
        BR --> MB[MockBackend]
        BR --> NCB[NetcodeBackend]
        BR --> CB[Custom Backend]
    end

    subgraph "System Handlers"
        HR --> TH[TimerNetworkHandler]
        HR --> PH[PoolNetworkHandler]
        HR --> EH[EventNetworkHandler]
    end

    subgraph "Usage"
        SL -- "Get<NetworkManager>()" --> NM
        T[Timer] --> |Extension| TH
        P[Pool] --> |Extension| PH
        E[NetworkEventChannel] --> |OnEnable| EH
    end
```

Everything is automatic! Configure in `PackageSettings` and use:

```csharp
// Timers
var timer = App.Get<Timer>();
var handle = timer.CreateTimer<CountdownTimer>(10f);
handle.MakeNetworked();
timer.Start(handle);
TimerNetworkExtensions.BroadcastTimerSync();

// Pool - with target selection
var pool = App.Get<Pool>();
var (h, id) = PoolNetworkExtensions.SpawnNetworked(prefab, pos, Quaternion.identity, true, NetworkTarget.Clients);
h.DespawnNetworked(NetworkTarget.All);

// Local-only spawn (no network broadcast)
var localHandle = PoolNetworkExtensions.SpawnLocal(prefab, pos);

// Events - with target selection
myNetworkChannel.Raise();                    // Use default target
myNetworkChannel.Raise(NetworkTarget.Server); // Send to server only
myNetworkChannel.RaiseLocal();               // Local only
```

---

## Configuration

### PackageSettings

| Setting | Values | Description |
|---------|--------|-------------|
| **Backend ID** | `mock`, `netcode`, custom | Which backend to use |
| **Handler Mode** | `Auto`, `Manual` | Handler registration |
| **Debug Mode** | bool | Network logs |

---

## NetworkTarget

```csharp
public enum NetworkTarget
{
    All,      // Server + all clients
    Others,   // Everyone except sender
    Server,   // Server only
    Clients   // All clients only
}
```

## Client-Specific Targeting

Send messages to specific clients (server only):

```csharp
var nm = App.Get<NetworkManager>();

// SERVER: Send to one specific client
nm.SendToClient(new MyMessage { Data = 42 }, clientId);

// SERVER: Send to multiple specific clients
nm.SendToClients(new MyMessage { Data = 42 }, clientA, clientB, clientC);

// SERVER: Send to array of clients
ulong[] teamMembers = GetTeamMembers();
nm.SendToClients(new TeamUpdate { Score = 100 }, teamMembers);

// Get local client ID
ulong myId = nm.LocalClientId;
```

> [!NOTE]
> These methods are server-only. Clients send to server with `SendToServer`, then the server relays.

---

## Extension Methods

### Timer

```csharp
// SERVER: Create and network a timer
var timer = App.Get<Timer>();
var handle = timer.CreateTimer<CountdownTimer>(5f);
handle.MakeNetworked();
timer.Start(handle);

// SERVER: Sync all timers to clients
TimerNetworkExtensions.BroadcastTimerSync();

// Cleanup
handle.RemoveNetworking();
handle.GetNetworkId();
```

### Pool

```csharp
// SERVER: Spawn with target
var (h, id) = PoolNetworkExtensions.SpawnNetworked(
    prefab, pos, Quaternion.identity,
    serverAuth: true,
    target: NetworkTarget.Clients  // or All, Others, Server
);

// LOCAL ONLY: Spawn without broadcast (e.g., UI elements)
var localHandle = PoolNetworkExtensions.SpawnLocal(prefab, pos);

// Despawn with target
handle.DespawnNetworked(NetworkTarget.All);

// Manual registration
handle.RegisterNetworked();
handle.UnregisterNetworked();
handle.GetNetworkId();
```

### Events

```csharp
// Use default target from inspector
myNetworkChannel.Raise();

// Override target at runtime
myNetworkChannel.Raise(NetworkTarget.Server);
myNetworkChannel.Raise(NetworkTarget.Others);

// Local only
myNetworkChannel.RaiseLocal();
```

### Chronos Synchronization

Time scale transitions on the server are automatically replicated to all clients via the [Chronos Manager](../Core/ChronosManager.md).

```mermaid
graph LR
    subgraph "Server"
        S_CM[ChronosManager] --> |OnTransitionStarted| S_NH[ChronosNetworkHandler]
        S_NH --> |Send| S_NM[NetworkManager]
    end

    S_NM --> |"ChronosSyncMessage"| C_NM[NetworkManager Client]

    subgraph "Client"
        C_NM --> |Route| C_NH[ChronosNetworkHandler]
        C_NH --> |SetTimeScale| C_CM[ChronosManager]
    end
```

**Usage (Server only):**
```csharp
var chronos = App.Get<ChronosManager>();
// This will smoothly slow down the "World" channel on all clients
chronos.SetTimeScale("World", 0.1f, 2.0f, EasingType.CubicInOut);
```

---

## Custom Backend

```csharp
public class MyBackend : INetworkBackend
{
    public bool IsServer => ...;
    public bool IsClient => ...;
    public bool IsConnected => ...;
    
    public void Initialize() { }
    public void Shutdown() { }
    public void Send(ushort msgType, byte[] data, NetworkTarget target) { }
    public void RegisterHandler(ushort msgType, Action<byte[], ulong> h) { }
    public void UnregisterHandler(ushort msgType) { }
}

public class MyFactory : INetworkBackendFactory
{
    public string Id => "mybackend";
    public string DisplayName => "My Backend";
    public bool IsAvailable => true;
    public bool OnInitialize()
    {
        App.Get<NetworkManager>().SetBackendById(Id);
        return true;
    }
    public INetworkBackend Create() => new MyBackend();
}
```

---

## Custom Message

```csharp
var nm = App.Get<NetworkManager>();
nm.Send(new MyMessage { Data = 42 }, NetworkTarget.All);
nm.On<MyMessage>(msg => Debug.Log(msg.Data));
```

---

## File Structure

```
Runtime/Networking/
├── Core/            NetworkManager, NetworkSerializer
├── Registries/      BackendRegistry, MessageRouter, HandlerRegistry
├── Bootstrap/       NetworkBootstrapper
├── Backends/
│   ├── Mock/
│   └── Netcode/
└── Messages/

Runtime/Timers/Features/   TimerNetworkHandler, Extensions
Runtime/Pooling/Features/  PoolNetworkHandler, Extensions
Runtime/Events/Network/    EventNetworkHandler, NetworkEventChannel
```
