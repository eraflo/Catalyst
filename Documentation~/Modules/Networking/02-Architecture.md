# Architecture

System design and component relationships.

---

## Overview

The networking system uses a **layered architecture** with clear abstraction boundaries:

```mermaid
flowchart TB
    subgraph "Your Game"
        G[Game Logic]
    end
    
    subgraph "Service Layer"
        NM[NetworkManager]
        LM[LobbyManager]
        CM[ConnectionManager]
        SM[NetworkSpawnManager]
    end
    
    subgraph "Abstraction Layer"
        IB[INetworkBackend]
        IL[ILobbyProvider]
        IDT[IDiscoveryTransport]
        IDP[IDiscoveryProvider]
    end
    
    subgraph "Implementation Layer"
        MB[MockBackend]
        NB[NetcodeBackend]
        UDP[UdpBroadcast]
        WS[WebSocket]
        LDP[LanDiscoveryProvider]
    end
    
    G --> NM & LM & CM & SM
    NM --> IB
    LM --> IL
    IL --> IDP
    IDP --> IDT
    IB -.-> MB & NB
    IDT -.-> UDP & WS
```

---

## Design Principles

### 1. Backend Agnostic

All game code interacts with **interfaces**, not implementations:

```csharp
// ✅ Good - uses abstraction
var network = App.Get<NetworkManager>();
network.Send(message, target);

// ❌ Bad - couples to specific backend
var ngo = FindObjectOfType<NetworkManager>();
ngo.NetworkingConfig.SendMessage(...);
```

### 2. Provider Pattern

Swappable components for different scenarios:

| Interface | Default | Alternatives |
|-----------|---------|--------------|
| `INetworkBackend` | NetcodeBackend | MockBackend, Custom |
| `IDiscoveryTransport` | UdpBroadcast | WebSocket, Mock |
| `ILobbyProvider` | LanLobbyProvider | SteamProvider, Custom |

### 3. Security by Default

Security features are **opt-out**, not opt-in:

- Secure connections enabled by default
- Rate limiting available via attribute
- Message validation utilities included

---

## Service Hierarchy

```mermaid
classDiagram
    class NetworkManager {
        +IsServer: bool
        +IsClient: bool
        +Send~T~(msg, target)
        +On~T~(handler)
    }
    
    class LobbyManager {
        +OnLobbyJoined: event
        +CreateLobby(options)
        +JoinLobby(joinCode)
        +SearchLobbies()
    }
    
    class ConnectionManager {
        +OnValidateConnection: event
        +SetPayload~T~(data)
        +SecurityConfig
    }
    
    class NetworkIdManager {
        +Register(obj): uint
        +Get(id): GameObject
        +Unregister(id)
    }
    
    class NetworkOwnershipManager {
        +SetOwner(objId, clientId)
        +GetOwner(objId): ulong
        +IsOwner(objId, clientId): bool
    }
    
    NetworkManager --> LobbyManager
    NetworkManager --> ConnectionManager
    NetworkManager --> NetworkIdManager
    NetworkManager --> NetworkOwnershipManager
```

---

## Message Flow

### Client → Server

```mermaid
sequenceDiagram
    participant C as Client Code
    participant NM as NetworkManager
    participant R as Router
    participant B as Backend
    participant S as Server
    
    C->>NM: Send(msg, Server)
    NM->>R: GetId<T>()
    NM->>B: Serialize & Send
    B->>S: Transport Layer
    S->>R: Route(msgId, data)
    R->>R: Deserialize & Dispatch
```

### Server → All Clients

```mermaid
sequenceDiagram
    participant S as Server Code
    participant NM as NetworkManager
    participant B as Backend
    participant C1 as Client 1
    participant C2 as Client 2
    
    S->>NM: Send(msg, Clients)
    NM->>B: Broadcast
    par To Client 1
        B->>C1: Transport
    and To Client 2
        B->>C2: Transport
    end
```

---

## Connection Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Disconnected
    
    Disconnected --> Connecting: StartClient()
    Connecting --> Connected: Approved
    Connecting --> Disconnected: Rejected
    
    Disconnected --> Hosting: StartHost()
    Hosting --> Connected: Server Started
    
    Connected --> Disconnected: Disconnect()
    Connected --> Disconnected: Timeout/Error
```

---

## File Structure

```
Runtime/Networking/
├── Core/
│   ├── NetworkManager.cs
│   ├── NetworkIdManager.cs
│   ├── NetworkOwnershipManager.cs
│   ├── NetworkProperty.cs
│   └── NetworkSerializer.cs
├── Features/
│   ├── Connection/
│   ├── Discovery/
│   ├── Lobby/
│   ├── Spawn/
│   └── ...
├── Interfaces/
│   ├── INetworkBackend.cs
│   ├── IDiscoveryTransport.cs
│   └── ...
├── Backends/
│   ├── Mock/
│   └── Netcode/
└── Transports/
    ├── UdpBroadcastTransport.cs
    └── WebSocketDiscoveryTransport.cs
```

---

## Next

- [Core Services](./03-CoreServices.md) - Deep dive into each service
