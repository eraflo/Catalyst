---
description: Universal guidelines for implementing systems in Eraflo.UnityImportPackage
---

# System Implementation Guidelines

These guidelines define the standard for implementing robust, scalable, and safe systems.

## 1. Thread Safety
**Requirement:** Support both high-performance single-threaded usage and safe multi-threaded usage.

- **Check:** `PackageRuntime.IsThreadSafe`
- **Pattern:** Conditional Locking

```csharp
public class GenericSystem
{
    private readonly object _lock = new object();
    private readonly List<IItem> _items = new List<IItem>();

    private static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

    public void Add(IItem item)
    {
        if (IsThreadSafe)
        {
            lock (_lock) _items.Add(item);
        }
        else
        {
            _items.Add(item);
        }
    }
}
```

- **Iteration:** Use snapshots to safely iterate collections.

```csharp
public void ProcessAll()
{
    // Create snapshot based on thread safety
    IItem[] snapshot;
    if (IsThreadSafe)
    {
        lock (_lock) snapshot = _items.ToArray();
    }
    else
    {
        snapshot = _items.ToArray();
    }

    // Iterate safely
    foreach (var item in snapshot) item.Process();
}
```

## 2. Async Safety
- **Main Thread:** Use `PackageRuntime.IsMainThread` to verify thread affinity.
- **Queuing:** Use `ConcurrentQueue` to move work from background threads to the main thread.

```csharp
public void ScheduleWork(Action work)
{
    if (PackageRuntime.IsMainThread)
    {
        work();
    }
    else
    {
        _pendingWork.Enqueue(work); // Consume in Update()
    }
}
```

## 3. Editor & Runtime Lifecycle
- **Static State:** Must be cleared when exiting Play Mode to prevent leaking state between runs (when Domain Reload is disabled).

```csharp
#if UNITY_EDITOR
[InitializeOnLoadMethod]
static void InitEditor()
{
    EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.ExitingPlayMode) Shutdown();
    };
}
#endif
```

## 4. Architecture
- **Settings:** `PackageSettings` (ScriptableObject) for configuration.
- **Runtime:** `PackageRuntime` (Static) for global state (ThreadMode, IsMainThread).
- **Core:** `Runtime/<System>/Core` for Managers and Logic.
- **API:** Use Interfaces (`IHandle`, `IProcessor`) for extensibility.

## 5. Networking
- **Local First:** Logic should work without networking.
- **Adapters:** Use separate "Networked" components to bridge logic to the network layer (e.g. `NetworkedPhysics`, `NetworkedHealth`).
