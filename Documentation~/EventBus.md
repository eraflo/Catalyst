# Event Bus System

A unified, thread-safe event system for Unity. Works via **code**, **inspector**, or **network**.

---

## Table of Contents

1. [Package Settings](#package-settings)
2. [Quick Start](#quick-start)
3. [Architecture](#architecture)
4. [Built-in Channels](#built-in-channels)
5. [Auto-Subscribe Attribute](#auto-subscribe-attribute)
6. [Creating Custom Channels](#creating-custom-channels)
7. [Network Events](#network-events)
8. [Netcode for GameObjects Integration](#netcode-for-gameobjects-integration)
9. [API Reference](#api-reference)
10. [Best Practices](#best-practices)

---

## Package Settings

A configuration ScriptableObject is **automatically created** when you import the package.

**Location**: `Assets/Resources/UnityImportPackageSettings.asset`

**Access**: Menu > **Tools > Eraflo Catalyst > Settings**

### Settings

| Setting | Description |
|---------|-------------|
| **Network Backend** | Backend to use: None, Mock, Netcode, or Custom |
| **Debug Mode** | Log network event messages to console |

When **Network Backend** is not `None`:
- The backend is automatically initialized by `NetworkBootstrapper`
- Network event channels will sync across the network
- No manual setup required!

---

## Quick Start

### Via Code
```csharp
using Eraflo.UnityImportPackage.Events;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] IntEventChannel onScoreChanged;

    void OnEnable() => onScoreChanged.Subscribe(OnScore);
    void OnDisable() => onScoreChanged.Unsubscribe(OnScore);
    
    void OnScore(int score) => Debug.Log($"Score: {score}");
    
    public void AddScore(int points) => onScoreChanged.Raise(points);
}
```

### Via Inspector
1. **Create Channel**: Right-click > Create > Events > [Type] Channel
2. **Add Listener**: Add Component > Events > [Type] Channel Listener
3. **Configure**: Drag channel asset, set up UnityEvent response
4. **Raise**: Call `channel.Raise()` from any script

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│               EventBus (static)                     │
│          Thread-safe subscription manager           │
└──────────────────────┬──────────────────────────────┘
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│ Code Subscribe │ │ Inspector      │ │ Network        │
│ channel.Sub()  │ │ Listener       │ │ Sync           │
└────────────────┘ └────────────────┘ └────────────────┘
```

### Safety Features

| Feature | Implementation |
|---------|----------------|
| **Thread-safe** | `lock` on all dictionary operations |
| **Async-safe** | Copy subscribers before iteration |
| **Editor-safe** | Raise button disabled outside Play mode |

---

## Built-in Channels

### Local Channels

| Type | Channel | Listener | Menu |
|------|---------|----------|------|
| void | `EventChannel` | `EventChannelListener` | Events/Event Channel |
| int | `IntEventChannel` | `IntEventChannelListener` | Events/Int Channel |
| float | `FloatEventChannel` | `FloatEventChannelListener` | Events/Float Channel |
| string | `StringEventChannel` | `StringEventChannelListener` | Events/String Channel |
| bool | `BoolEventChannel` | `BoolEventChannelListener` | Events/Bool Channel |
| Vector3 | `Vector3EventChannel` | `Vector3EventChannelListener` | Events/Vector3 Channel |

### Network Channels

| Type | Channel | Menu |
|------|---------|------|
| void | `NetworkEventChannel` | Events/Network/Event Channel |
| int | `NetworkIntEventChannel` | Events/Network/Int Channel |
| float | `NetworkFloatEventChannel` | Events/Network/Float Channel |
| string | `NetworkStringEventChannel` | Events/Network/String Channel |
| bool | `NetworkBoolEventChannel` | Events/Network/Bool Channel |
| Vector3 | `NetworkVector3EventChannel` | Events/Network/Vector3 Channel |

### Addressables Integration

EventChannels are **automatically registered** to Addressables when created.

**Auto-generated structure:**
- **Group**: `EventChannels`
- **Address**: `Events/{Type}/{AssetName}` (e.g., `Events/Int/OnScoreChanged`)

**Manual registration**: Menu > **Tools > Eraflo Catalyst > Register All EventChannels to Addressables**

#### Concrete Example: Mod System

Imagine a game with mods. Each mod can add its own events via a JSON file:

```json
// mod_config.json
{
    "events": [
        "Events/Int/OnCustomBossDefeated",
        "Events/Void/OnSecretFound"
    ]
}
```

The `EventChannelLoader` allows loading these events dynamically:

```csharp
using Eraflo.UnityImportPackage.Events;
using UnityEngine;
using System.Collections.Generic;

public class ModLoader : MonoBehaviour
{
    private List<IntEventChannel> _loadedEvents = new();

    void Start()
    {
        // Read the mod config file
        var modConfig = LoadModConfig("mod_config.json");
        
        // Load each event dynamically
        foreach (string eventAddress in modConfig.events)
        {
            EventChannelLoader.LoadAsync<IntEventChannel>(eventAddress, channel =>
            {
                if (channel != null)
                {
                    _loadedEvents.Add(channel);
                    channel.Subscribe(OnModEvent);
                    Debug.Log($"Mod event loaded: {eventAddress}");
                }
            });
        }
    }

    void OnModEvent(int value)
    {
        Debug.Log($"Mod event triggered with value: {value}");
    }
}
```

> **Note**: If you only use `[SerializeField]` to reference your events, you **don't need** the `EventChannelLoader`. It's only useful for dynamic loading.

---

## Auto-Subscribe Attribute

Simplify subscription with `[SubscribeTo]` attribute:

```csharp
using Eraflo.UnityImportPackage.Events;

public class PlayerUI : EventSubscriber  // Inherit from EventSubscriber
{
    [SerializeField] IntEventChannel onHealthChanged;
    [SerializeField] EventChannel onPlayerDied;

    [SubscribeTo(nameof(onHealthChanged))]
    void OnHealthChanged(int health)
    {
        healthBar.value = health;
    }

    [SubscribeTo(nameof(onPlayerDied))]
    void OnPlayerDied()
    {
        gameOverScreen.SetActive(true);
    }
}
```

> **Note**: No need for `OnEnable`/`OnDisable` - subscription is handled automatically!

---

## Creating Custom Channels

### Step 1: Create the Channel

```csharp
using UnityEngine;
using Eraflo.UnityImportPackage.Events;

[CreateAssetMenu(menuName = "Events/Player Data Channel")]
public class PlayerDataChannel : EventChannel<PlayerData> { }

[System.Serializable]
public struct PlayerData
{
    public string Name;
    public int Score;
    public Vector3 Position;
}
```

### Step 2: Create the Listener (Optional)

```csharp
using Eraflo.UnityImportPackage.Events;

public class PlayerDataChannelListener : EventChannelListener<PlayerDataChannel, PlayerData> { }
```

> **Note**: A custom editor is automatically applied to all `EventChannel<T>` types!

---

## Network Events

Handlers are auto-registered via `PackageSettings`.

### Settings

| Setting | Description |
|---------|-------------|
| `EnableNetwork` | Send over network |
| `NetworkTarget` | `All`, `Others`, `Server`, `Clients` |
| `RaiseLocally` | Also trigger locally |

### Usage

```csharp
// Use default target from inspector settings
myNetworkChannel.Raise();

// Override target at runtime
myNetworkChannel.Raise(NetworkTarget.Server);   // Send to server only
myNetworkChannel.Raise(NetworkTarget.Others);   // Send to others
myNetworkChannel.Raise(NetworkTarget.Clients);  // Send to all clients

// Local only (no network)
myNetworkChannel.RaiseLocal();
```

### Receiving Events (Client/Server)

```csharp
// CLIENT or SERVER: Subscribe to receive
var handler = NetworkManager.Handlers.Get<EventNetworkHandler>();
handler.OnEventReceived += (channelId, payload) => { /* handle */ };
```

See [Networking.md](Networking.md) for details.

---

## API Reference

### EventChannel / EventChannel\<T\>

| Method | Description |
|--------|-------------|
| `Raise()` | Notifies subscribers |
| `Subscribe(Action)` | Adds subscriber |
| `Unsubscribe(Action)` | Removes |

### NetworkEventChannel / NetworkEventChannel\<T\>

| Property | Description |
|----------|-------------|
| `EnableNetwork` | Network sync |
| `NetworkTarget` | Recipients |
| `RaiseLocally` | Also local |

| Method | Description |
|--------|-------------|
| `Raise()` | With network |
| `RaiseLocal()` | Local only |

---

## Best Practices

1. **Unsubscribe** in `OnDisable()` or use `EventSubscriber`
2. **Use typed channels** for data events
3. **Set Handler** on network channels before use
4. **Use `RaiseLocal()`** for received network events
