# Package Settings

Central configuration for the UnityImportPackage.

---

## Location

The configuration file is **automatically created** when the package is imported:

```
Assets/Resources/UnityImportPackageSettings.asset
```

---

## Access

### Via Menu
**Tools > Unity Import Package > Settings**

### Via Code
```csharp
using Eraflo.UnityImportPackage;

var settings = PackageSettings.Instance;
bool networkEnabled = settings.EnableNetworking;
bool debugMode = settings.NetworkDebugMode;
```

---

## Settings

### Network Events

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Enable Networking** | `bool` | `false` | Auto-instantiate `NetworkEventManager` on startup |
| **Network Debug Mode** | `bool` | `false` | Display network debug logs |

### Timer System

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Thread Mode** | `enum` | `SingleThread` | `SingleThread` = faster, `ThreadSafe` = safe from any thread |
| **Enable Timer Debug Logs** | `bool` | `false` | Display timer debug logs |

### Timer Pool

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Enable Timer Pooling** | `bool` | `true` | Enable/disable object pooling |
| **Default Capacity** | `int` | `10` | Initial pool size per timer type |
| **Max Capacity** | `int` | `50` | Maximum pooled timers per type |
| **Prewarm Count** | `int` | `0` | Timers to prewarm on startup |

---

## Runtime Behavior

### If Enable Networking = true

At game launch (`RuntimeInitializeOnLoadMethod`):
1. A `[NetworkEventManager]` GameObject is created
2. `DontDestroyOnLoad` is applied → persists across scenes
3. Ready to receive an `INetworkEventHandler`

```
[PackageInitializer] NetworkEventManager initialized
```

### If Enable Networking = false

No automatic action. You must handle it manually:
```csharp
var go = new GameObject("NetworkEventManager");
go.AddComponent<NetworkEventManagerBehaviour>();
DontDestroyOnLoad(go);
```

---

## Force Reload

If you modify settings during execution:
```csharp
PackageSettings.Reload();
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No settings found" warning | Create via **Tools > Unity Import Package > Create Settings** |
| Settings not applied | Verify the file is in `Assets/Resources/` |
| NetworkEventManager missing | Enable **Enable Networking** in settings |
