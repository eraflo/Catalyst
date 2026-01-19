# Utilities Module

The **Utilities** module provides essential helper classes and services that don't fit into larger specific modules but are critical for production development.

---

## Table of Contents

1. [Log Exporter](#1-log-exporter)
2. [Serializable Callback](#2-serializable-callback)

---

## 1. Log Exporter

The `LogExporter` is a core service that buffers Unity logs in memory and allows you to export them to a file on demand. This is invaluable for debugging builds where you can't attach a debugger.

### Quick Start

```csharp
using Eraflo.Catalyst;
using Eraflo.Catalyst.Utilities;

public class DebugMenu : MonoBehaviour
{
    public void OnExportLogsClicked()
    {
        // Export logs to device storage
        string path = App.Get<LogExporter>().Export();
        
        Debug.Log($"Logs saved to: {path}");
    }
}
```

### Features

- **Buffer Limiting**: Keeps the last 5000 lines (default) to prevent memory issues.
- **Thread Safe**: Captures logs from any thread.
- **Rich Context**: Includes timestamps, log types, and stack traces for errors.
- **Automatic Path**: Saves to `Application.persistentDataPath/Logs`.

### API Reference

| Method | Description |
|--------|-------------|
| `Export()` | Writes buffered logs to a text file and returns the path. |
| `Clear()` | Clears the current in-memory log buffer. |

---

## 2. Serializable Callback

`SerializableCallback` is a lightweight alternative to `UnityEvent`. It allows you to serialize method calls in the Inspector but with better performance and more flexibility in code.

### Usage

```csharp
using UnityEngine;
using Eraflo.Catalyst.Utilities;

public class ButtonHandler : MonoBehaviour
{
    // Shows in Inspector like UnityEvent
    public SerializableCallback OnClick;
    
    public void Click()
    {
        // Invoke serialized methods
        OnClick.Invoke();
    }
}
```

### Features

- **Inspector Support**: Assign methods via drag-and-drop in the Editor.
- **Performance**: Generally faster invocation than `UnityEvent`.
- **Serialization**: Fully supports Unity's serialization system.
