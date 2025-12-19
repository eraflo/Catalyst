# Package Settings

Central configuration for the package.

## Location

```
Assets/Resources/UnityImportPackageSettings.asset
```

**Menu**: Tools > Unity Import Package > Settings

## Settings

### Global

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Thread Mode** | `PackageThreadMode` | `SingleThread` | Thread safety mode |

### Networking

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Network Backend ID** | `string` | *(empty)* | Backend to use |
| **Network Debug Mode** | `bool` | `false` | Log debug messages |

#### Backend IDs

| ID | Description |
|----|-------------|
| *(empty)* | Disabled |
| `mock` | Testing backend |
| `netcode` | Unity Netcode for GameObjects |

Custom backends: Register via `NetworkBackendRegistry.Register()`.

### Timer System

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| **Use Burst Timers** | `bool` | `false` | Optimized backend |
| **Enable Timer Debug Logs** | `bool` | `false` | Log timer events |
| **Enable Debug Overlay** | `bool` | `false` | Show overlay |

## Runtime Initialization

At startup, `NetworkBootstrapper`:

1. Registers built-in backends (`mock`, `netcode`)
2. Creates backend from `NetworkBackendId` setting

## Code Access

```csharp
var settings = PackageSettings.Instance;
settings.NetworkBackendId   // Current backend ID
settings.EnableNetworking   // Is networking enabled?
settings.ThreadMode         // Thread safety mode
```

## Reload

```csharp
PackageSettings.Reload();
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No settings found" | Create via menu |
| Network not working | Check `NetworkBackendId` |
| Backend not found | Register factory first |
