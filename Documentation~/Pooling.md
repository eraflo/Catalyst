# Object Pooling System

A generic, thread-safe, async-safe, multiplayer-ready object pooling system.

## Quick Start

```csharp
using Eraflo.UnityImportPackage.Pooling;

// Generic objects
var handle = Pool.Get<Bullet>();
Pool.Release(handle);

// Prefabs (GameObjects)
var vfx = Pool.Spawn(explosionPrefab, transform.position);
Pool.Despawn(vfx);

// Auto-despawn after 2 seconds
Pool.SpawnTimed(particlePrefab, pos, 2f);

// Pre-allocate for performance
Pool.Warmup<Bullet>(100);
Pool.Warmup(enemyPrefab, 20);
```

---

## Architecture

```mermaid
graph TB
    subgraph "API"
        PF["Pool (Static Facade)"]
        PH["PoolHandle&lt;T&gt;"]
    end

    subgraph "Core"
        GP["GenericPool&lt;T&gt;"]
        PP["PrefabPool"]
        PO["PooledObject"]
    end

    subgraph "Features"
        PM["PoolMetrics"]
        NPS["NetworkPoolSync"]
    end

    PF --> GP
    PF --> PP
    PP --> PO
    PF --> PM
    PF --> NPS
```

---

## Features

### Generic Pool (Any Class)

```csharp
// Get from pool
var handle = Pool.Get<MyClass>();
var instance = handle.Instance;

// Use the object...
instance.DoSomething();

// Return to pool
Pool.Release(handle);
```

### Prefab Pool (GameObjects)

```csharp
// Spawn prefab
var handle = Pool.Spawn(prefab, position, rotation);

// Access the GameObject
handle.Instance.transform.LookAt(target);

// Despawn (return to pool)
Pool.Despawn(handle);
```

### IPoolable Callbacks

```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        // Reset state when spawned
        velocity = Vector3.zero;
        damage = 10;
    }

    public void OnDespawn()
    {
        // Cleanup when returned to pool
        trail.Clear();
    }
}
```

### Timer Integration

```csharp
// Auto-despawn after duration
Pool.SpawnTimed(explosionPrefab, pos, 2f);

// PooledObject component also supports this:
pooledObject.DespawnAfter(3f);
```

### Warmup (Pre-allocation)

```csharp
// Generic pool warmup
Pool.Warmup<Bullet>(100);

// Prefab pool warmup
Pool.Warmup(enemyPrefab, 20);
```

---

## Thread Safety

The pool system is thread-safe when `PackageRuntime.IsThreadSafe` is enabled.

```csharp
// Operations from any thread are safe
await Task.Run(() =>
{
    var handle = Pool.Get<MyClass>();
    Pool.Release(handle);
});
```

---

## Networking

Handlers are auto-registered via `PackageSettings`.

### Spawning (Server → Target)

```csharp
// SERVER: Spawn and broadcast to all clients (default)
var (handle, networkId) = PoolNetworkExtensions.SpawnNetworked(prefab, pos);

// SERVER: Spawn and broadcast to specific target
var (h, id) = PoolNetworkExtensions.SpawnNetworked(
    prefab, pos, Quaternion.identity,
    serverAuth: true,
    target: NetworkTarget.All  // or Others, Server, Clients
);

// LOCAL ONLY: Spawn without network broadcast (UI elements, etc.)
var localHandle = PoolNetworkExtensions.SpawnLocal(prefab, pos);
```

### Despawning (Server → Target)

```csharp
// SERVER: Despawn and notify all clients (default)
handle.DespawnNetworked();

// SERVER: Despawn with specific target
handle.DespawnNetworked(NetworkTarget.All);
```

### Manual Registration

```csharp
// SERVER: Register existing object for networking
var handle = Pool.Spawn(prefab, pos);
handle.RegisterNetworked();

// SERVER: Unregister when done
handle.UnregisterNetworked();
```

### Client Handling

```csharp
// CLIENT: React to spawn/despawn messages
var handler = NetworkManager.Handlers.Get<PoolNetworkHandler>();
handler.OnSpawnReceived += msg => { /* spawn locally */ };
handler.OnDespawnReceived += id => { /* despawn locally */ };
```

See [Networking.md](Networking.md) for details.

---

## Metrics

```csharp
var metrics = Pool.Metrics;

metrics.TotalSpawned;     // Total objects spawned
metrics.TotalDespawned;   // Total objects despawned
metrics.ActiveCount;      // Currently active
metrics.PeakActiveCount;  // Peak simultaneous active
```

---

## Editor Tools

### Pool Debugger Window

**Tools > Unity Import Package > Pool Debugger**

Shows:
- List of all active pools
- Active/Available counts per pool
- Real-time metrics
- Clear pool buttons

### PooledObject Inspector

Shows:
- Current spawn state (Spawned/Pooled)
- Handle ID and Pool ID
- Time since spawn
- Quick Despawn buttons

---

## API Reference

### Pool

| Method | Description |
|--------|-------------|
| `Get<T>()` | Get object from generic pool |
| `Release<T>(handle)` | Return object to generic pool |
| `Spawn(prefab, pos, rot)` | Spawn prefab from pool |
| `SpawnTimed(prefab, pos, duration)` | Spawn with auto-despawn |
| `Despawn(handle)` | Return prefab to pool |
| `Warmup<T>(count)` | Pre-allocate generic objects |
| `Warmup(prefab, count)` | Pre-allocate prefabs |
| `Clear<T>()` | Clear specific generic pool |
| `Clear(prefab)` | Clear specific prefab pool |
| `ClearAll()` | Clear all pools |

### PoolHandle<T>

| Property | Description |
|----------|-------------|
| `Id` | Unique handle ID |
| `Instance` | The pooled object |
| `PoolId` | Pool identifier |
| `SpawnTime` | Timestamp when spawned |
| `IsValid` | Whether handle is valid |

### PooledObject

| Property | Description |
|----------|-------------|
| `HandleId` | Current handle ID |
| `PoolId` | Pool identifier |
| `IsSpawned` | Whether currently spawned |
| `TimeSinceSpawn` | Seconds since spawn |
| `Despawn()` | Return to pool |
| `DespawnAfter(delay)` | Delayed despawn |

---

## File Structure

```
Runtime/Pooling/
├── Core/
│   ├── IPoolable.cs
│   ├── PoolHandle.cs
│   ├── GenericPool.cs
│   └── Pool.cs
├── Prefabs/
│   ├── PrefabPool.cs
│   └── PooledObject.cs
└── Features/
    ├── PoolMetrics.cs
    └── NetworkPoolSync.cs

Editor/Pooling/
├── PoolDebuggerWindow.cs
└── PooledObjectEditor.cs
```
