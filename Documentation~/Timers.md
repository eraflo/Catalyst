# Timer System

A high-performance, handle-based timer system with automatic backend selection and Burst support.

## Table of Contents
- [Quick Start](#quick-start)
- [Timer Types](#timer-types)
- [Callbacks](#extensible-callbacks)
- [Easing](#easing-integration)
- [Chaining & Groups](#timer-chaining)
- [Presets](#timer-presets)
- [Persistence](#timer-persistence)
- [Metrics](#timer-metrics)
- [Backends](#backends)
- [Multiplayer](#multiplayer-support)
- [API Reference](#api-reference)

---

## Architecture

```mermaid
graph TB
    subgraph "User Code"
        UC[Your Script]
    end

    subgraph "Timer API"
        TF["Timer (Static Facade)"]
        TH["TimerHandle"]
    end

    subgraph "Features"
        TC[TimerChain]
        TG[TimerGroup]
        TP[TimerPresets]
        TPE[TimerPersistence]
        TM[TimerMetrics]
        NS[NetworkTimerSync]
    end

    subgraph "Callbacks"
        TCB[TimerCallbacks]
        CB1[OnComplete]
        CB2[OnTick]
        CB3[OnRepeat]
        CB4["OnPause/Resume/Reset/Cancel"]
    end

    subgraph "Backends"
        IB["ITimerBackend"]
        SB["StandardBackend"]
        BB["BurstBackend"]
    end

    subgraph "Timer Types"
        CT[CountdownTimer]
        ST[StopwatchTimer]
        DT[DelayTimer]
        RT[RepeatingTimer]
        FT[FrequencyTimer]
    end

    UC --> TF
    TF --> TH
    TF --> TCB
    TF --> IB
    
    TC --> TF
    TG --> TF
    TP --> TF
    TPE --> TF
    TM --> TF
    NS --> TF
    
    TCB --> CB1
    TCB --> CB2
    TCB --> CB3
    TCB --> CB4
    
    IB --> SB
    IB --> BB
    
    SB --> CT
    SB --> ST
    SB --> DT
    SB --> RT
    SB --> FT
    
    BB --> CT
    BB --> ST
    BB --> DT
    BB --> RT
    BB --> FT
```

---

## Quick Start

```csharp
using Eraflo.UnityImportPackage.Timers;

// Create timers
var countdown = Timer.Create<CountdownTimer>(5f);
var stopwatch = Timer.Create<StopwatchTimer>(0f);

// Simple delay
Timer.Delay(2f, () => Debug.Log("Done!"));

// Events
Timer.On<OnComplete>(countdown, () => Debug.Log("Finished!"));
Timer.On<OnTick, float>(countdown, (dt) => Debug.Log($"Tick: {dt}"));

// Control
Timer.Pause(countdown);
Timer.Resume(countdown);
Timer.Cancel(countdown);

// Query
float progress = Timer.GetProgress(countdown);
bool done = Timer.IsFinished(countdown);
```

---

## Timer Types

| Type | Description | Callbacks |
|------|-------------|-----------|
| `CountdownTimer` | Counts down to 0 | OnComplete, OnTick, OnPause, OnResume, OnReset, OnCancel |
| `StopwatchTimer` | Counts up indefinitely | OnTick, OnPause, OnResume, OnReset, OnCancel |
| `DelayTimer` | One-shot countdown | OnComplete, OnTick, OnCancel |
| `RepeatingTimer` | Ticks every interval | OnComplete, OnRepeat, OnTick, OnPause, OnResume, OnReset, OnCancel |
| `FrequencyTimer` | N ticks per second | OnTick, OnPause, OnResume, OnReset, OnCancel |

---

## Extensible Callbacks

### Built-in

```csharp
Timer.On<OnComplete>(handle, () => { });           // Finished
Timer.On<OnTick, float>(handle, (dt) => { });      // Every frame
Timer.On<OnRepeat, int>(handle, (count) => { });   // Repeat interval
Timer.On<OnPause>(handle, () => { });
Timer.On<OnResume>(handle, () => { });
Timer.On<OnReset>(handle, () => { });
Timer.On<OnCancel>(handle, () => { });
```

### Custom

```csharp
// 1. Define callback type
public struct OnDamage : ITimerCallback { }

// 2. Implement in timer
public struct DamageTimer : ITimer, ISupportsCallback<OnDamage>
{
    public void CollectCallbacks(ICallbackCollector collector)
    {
        if (_shouldFire)
            collector.Trigger<OnDamage, DamageData>(_data);
    }
}

// 3. Use it
Timer.On<OnDamage, DamageData>(handle, (data) => ApplyDamage(data));
```

---

## Easing Integration

```csharp
using Eraflo.UnityImportPackage.EasingSystem;

// Eased progress
float eased = Timer.GetEasedProgress(handle, EasingType.ElasticOut);

// Lerp (float, Vector2, Vector3, Quaternion, Color)
float value = Timer.Lerp(handle, 0f, 100f, EasingType.QuadInOut);
Vector3 pos = Timer.Lerp(handle, start, end, EasingType.BounceOut);
Color col = Timer.Lerp(handle, Color.red, Color.blue, EasingType.Linear);

// Unclamped (for Elastic/Back overshoots)
float unclamped = Timer.LerpUnclamped(handle, 0f, 10f, EasingType.ElasticOut);
```

---

## Timer Chaining

```csharp
Timer.Chain()
    .Delay(1f)
    .Then(() => Debug.Log("Step 1"))
    .Delay(2f)
    .Then(() => Debug.Log("Step 2"))
    .Loop(3, 0.5f, (i) => Debug.Log($"Loop {i}"))
    .Start();
```

## Timer Groups

```csharp
var group = Timer.CreateGroup("UI Timers");

var h1 = group.Create<CountdownTimer>(5f);
var h2 = group.Delay(3f, () => Debug.Log("Done"));

group.PauseAll();
group.ResumeAll();
group.SetTimeScaleAll(0.5f);
group.CancelAll();
```

---

## Timer Presets

Define reusable configurations:

```csharp
// Define (once at startup)
TimerPresets.Define("UIFade", 0.3f, EasingType.QuadOut);
TimerPresets.Define<RepeatingTimer>("Heartbeat", 1f);

// Use
var handle = Timer.FromPreset("UIFade");
Timer.FromPreset("UIFade", () => OnComplete());
```

---

## Timer Persistence

Save/restore timers with callbacks:

```csharp
// Save
string json = TimerPersistence.SaveAll();
PlayerPrefs.SetString("Timers", json);

// Load (callbacks restored via reflection)
TimerPersistence.LoadAll(PlayerPrefs.GetString("Timers"));
```

> **Important**: Lambdas cannot be persisted. Use method references.

---

## Timer Metrics

```csharp
var m = Timer.Metrics;

m.TotalCreated       // Total timers created
m.ActiveCount        // Currently active
m.TotalCompleted     // Natural completions
m.TotalCancelled     // Cancellations
m.PeakActiveCount    // Max simultaneous
m.AverageDuration    // Average initial duration
m.LastUpdateMs       // Last update time (ms)

Timer.Metrics.Reset();
```

---

## Backends

### StandardBackend (Default)
Thread-safe, queue-based async operations.

### BurstBackend (High Performance)
Uses Unity.Burst + Unity.Collections for parallel updates.

Enable: **PackageSettings** → **Use Burst Timers**

---

## Multiplayer Support

Handlers are auto-registered via `PackageSettings`.

### Creating Networked Timers (Server)

```csharp
// SERVER: Create and register a networked timer
var handle = Timer.Create<CountdownTimer>(10f);
handle.MakeNetworked();  // Extension method
Timer.Start(handle);
```

### Syncing State (Server → Clients)

```csharp
// SERVER: Periodically sync all networked timers to clients
void Update()
{
    if (NetworkManager.IsServer && Time.time > _nextSync)
    {
        TimerNetworkExtensions.BroadcastTimerSync();
        _nextSync = Time.time + 1f;
    }
}
```

### Cleanup

```csharp
// SERVER or CLIENT: Remove networking from a timer
handle.RemoveNetworking();  // Extension method
```

See [Networking.md](Networking.md) for details.

---

## API Reference

### Creation
| Method | Description |
|--------|-------------|
| `Timer.Create<T>(float)` | Create timer |
| `Timer.Delay(float, Action)` | One-shot delay |
| `Timer.FromPreset(string)` | Create from preset |

### Control
| Method | Description |
|--------|-------------|
| `Pause(handle)` | Pause |
| `Resume(handle)` | Resume |
| `Cancel(handle)` | Cancel and remove |
| `Reset(handle)` | Reset to initial |
| `SetTimeScale(handle, float)` | Time multiplier |
| `Clear()` | Remove all |

### Query
| Method | Description |
|--------|-------------|
| `GetProgress(handle)` | Progress 0-1 |
| `GetEasedProgress(handle, EasingType)` | Eased progress |
| `IsFinished(handle)` | Check finished |
| `IsRunning(handle)` | Check running |

### Events
| Method | Description |
|--------|-------------|
| `On<T>(handle, Action)` | Register callback |
| `On<T, TArg>(handle, Action<TArg>)` | With parameter |
| `Off<T>(handle)` | Unregister |

---

## File Structure

```
Runtime/Timers/
├── Core/           Timer.cs, TimerHandle.cs, ITimer.cs, TimerCallbacks.cs
├── Types/          CountdownTimer, StopwatchTimer, DelayTimer, etc.
├── Features/       TimerChain, TimerGroup, TimerPresets, TimerPersistence, TimerMetrics
├── Backends/       StandardBackend, BurstBackend
└── Debugging/      TimerDebugger (F5 overlay)
```
