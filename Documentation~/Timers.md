# Timer System

> Inspired by [GitAmend's Improved Unity Timers](https://github.com/adammyhre/Unity-Improved-Timers) by Adam Myhre.

A high-performance, extensible timer system that integrates directly into Unity's Player Loop.

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Timer Types](#timer-types)
  - [CountdownTimer](#countdowntimer)
  - [StopwatchTimer](#stopwatchtimer)
  - [FrequencyTimer](#frequencytimer)
  - [DelayTimer](#delaytimer)
  - [RepeatingTimer](#repeatingtimer)
- [API Reference](#api-reference)
- [Timer Pooling](#timer-pooling)
- [Timer Chaining](#timer-chaining)
- [Timer Groups](#timer-groups)
- [Thread Safety](#thread-safety)
- [Network Synchronization](#network-synchronization)
- [Advanced Usage](#advanced-usage)
- [How It Works](#how-it-works)

---

## Features

| Feature | Description |
|---------|-------------|
| **Player Loop Integration** | Timers run independently of MonoBehaviours |
| **Self-Managing** | Timers auto-register/unregister with the TimerManager |
| **Multiple Timer Types** | Countdown, Stopwatch, Frequency, Delay, Repeating |
| **One-Liner Delays** | `TimerManager.Delay(2f, action)` helper |
| **Timer Pooling** | `TimerPool.Get<T>()` with reflection support |
| **Timer Chaining** | Sequential execution with `TimerChain` |
| **Scaled/Unscaled Time** | Support for `Time.deltaTime` and `Time.unscaledDeltaTime` |
| **Individual TimeScale** | Per-timer speed multiplier |
| **Thread-Safe Mode** | Optional concurrent access from any thread |
| **Network Sync** | NetworkedTimer variants for multiplayer |

### Folder Structure

```
Runtime/Timers/
├── Core/       Timer, TimerManager, TimerBootstrapper, TimerPool
├── Types/      CountdownTimer, StopwatchTimer, FrequencyTimer, RepeatingTimer, DelayTimer
├── Chaining/   TimerChain, ChainSteps (IChainStep implementations)
└── Network/    NetworkedTimer
```

---

## Quick Start

```csharp
using Eraflo.UnityImportPackage.Timers;

// One-liner delay (auto-disposes)
TimerManager.Delay(2f, () => Debug.Log("2 seconds passed!"));

// Countdown Timer - counts down to zero
var countdown = new CountdownTimer(5f);
countdown.OnTimerStop += () => Debug.Log("Timer finished!");
countdown.Start();

// Stopwatch Timer - counts up from zero
var stopwatch = new StopwatchTimer();
stopwatch.Start();

// Repeating Timer - fires every interval
var repeater = new RepeatingTimer(1f, 5); // 5 times, every 1s
repeater.OnTick += () => Debug.Log($"Tick {repeater.CurrentRepeat}");
repeater.Start();

// Frequency Timer - ticks N times per second
var frequency = new FrequencyTimer(60);
frequency.OnTick += () => ProcessGameLogic();
frequency.Start();

// Don't forget to dispose!
countdown.Dispose();
```

---

## Timer Types

### CountdownTimer

Counts down from a duration to zero.

```csharp
var timer = new CountdownTimer(10f);

timer.OnTimerStop += () => Debug.Log("Complete");
timer.Start();

Debug.Log(timer.CurrentTime);  // Remaining time
Debug.Log(timer.Progress);      // 1.0 → 0.0
Debug.Log(timer.IsFinished);    // true when CurrentTime <= 0
```

### StopwatchTimer

Counts up from zero indefinitely.

```csharp
var stopwatch = new StopwatchTimer();
stopwatch.Start();

Debug.Log($"Elapsed: {stopwatch.ElapsedTime}s");

stopwatch.Pause();   // Pause
stopwatch.Resume();  // Continue
stopwatch.Reset();   // Back to zero
```

### FrequencyTimer

Fires an event N times per second.

```csharp
var ticker = new FrequencyTimer(60); // 60 ticks per second
ticker.OnTick += () => ProcessGameLogic();
ticker.Start();

Debug.Log(ticker.TickCount); // Total ticks since start
```

### DelayTimer

One-liner for delayed actions. Auto-disposes after completion.

```csharp
// Simple delay
TimerManager.Delay(2f, () => Debug.Log("Done!"));

// With unscaled time (works during pause)
TimerManager.Delay(1f, ShowNotification, useUnscaledTime: true);

// Keep reference to cancel
var delay = TimerManager.Delay(5f, () => SpawnBoss());
delay.Cancel(); // Cancels without invoking callback
```

### RepeatingTimer

Fires an event at regular intervals, finite or infinite times.

```csharp
// Repeat 5 times, every 2 seconds
var timer = new RepeatingTimer(2f, 5);
timer.OnTick += () => Debug.Log($"Tick {timer.CurrentRepeat}/{timer.RepeatCount}");
timer.OnComplete += () => Debug.Log("All repeats done!");
timer.Start();

// Infinite repeating (repeatCount = 0)
var infinite = new RepeatingTimer(0.5f, 0);
infinite.OnTick += SpawnEnemy;
infinite.Start();

// Properties
Debug.Log(timer.Interval);        // 2.0
Debug.Log(timer.CurrentRepeat);   // Current repeat (1-based)
Debug.Log(timer.RemainingRepeats);// Repeats left
Debug.Log(timer.IsInfinite);      // true if repeatCount = 0
```

---

## API Reference

### Methods

| Method | Description |
|--------|-------------|
| `Start()` | Starts or resumes the timer |
| `Stop()` | Stops the timer and fires `OnTimerStop` |
| `Pause()` | Pauses without firing events |
| `Resume()` | Same as `Start()` |
| `Reset()` | Resets to initial time |
| `Reset(float)` | Resets with a new duration |
| `Dispose()` | Unregisters and cleans up the timer |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentTime` | `float` | Current time value |
| `IsRunning` | `bool` | Whether the timer is active |
| `IsFinished` | `bool` | Whether the timer has completed |
| `Progress` | `float` | 0.0 to 1.0 progress ratio |
| `UseUnscaledTime` | `bool` | Use real-time (ignores `Time.timeScale`) |
| `TimeScale` | `float` | Individual speed multiplier (default: 1.0) |

### Events

| Event | Description |
|-------|-------------|
| `OnTimerStart` | Fired when `Start()` is called |
| `OnTimerStop` | Fired when `Stop()` is called or timer finishes |
| `OnTick` | *(FrequencyTimer, RepeatingTimer)* Fired at interval |
| `OnComplete` | *(RepeatingTimer)* Fired when all repeats complete |

---

## Timer Pooling

Reduce garbage collection by reusing timer instances.

### Configuration

Configure pooling in the `PackageSettings` asset:

| Setting | Default | Description |
|---------|---------|-------------|
| Enable Timer Pooling | `true` | Enable/disable pooling |
| Default Capacity | `10` | Initial pool size per type |
| Max Capacity | `50` | Maximum pooled timers per type |
| Prewarm Count | `0` | Prewarm on startup |

### Basic Usage

```csharp
// Get from pool (instead of new)
var timer = TimerPool.GetCountdown(5f);
timer.OnTimerStop += () => Debug.Log("Done!");
timer.Start();

// Return to pool when done (instead of Dispose)
TimerPool.Release(timer);
```

### Available Methods

```csharp
// Generic method - works with any timer type
var countdown = TimerPool.Get<CountdownTimer>(5f);  // 5 second duration
var stopwatch = TimerPool.Get<StopwatchTimer>();
var repeater = TimerPool.Get<RepeatingTimer>(1f);   // 1 second interval
var frequency = TimerPool.Get<FrequencyTimer>(60);  // 60 ticks/sec

// Also works with custom timers
var myTimer = TimerPool.Get<MyCustomTimer>(2f);
```

### Manual Prewarming

```csharp
// Prewarm at any time (uses reflection)
TimerPool.Prewarm<CountdownTimer>(20);
TimerPool.Prewarm<MyCustomTimer>(10);
```

> [!IMPORTANT]
> Use `TimerPool.Release(timer)` instead of `timer.Dispose()` to return timers to the pool.

---

## Timer Chaining

Chain multiple steps for sequential execution.

### Basic Usage

```csharp
TimerChain.Start(2f)
    .Then(() => Debug.Log("Step 1"))
    .Then(1.5f)
    .Then(() => Debug.Log("Step 2"))
    .OnComplete(() => Debug.Log("Done!"))
    .Run();
```

### Available Steps

| Method | Description |
|--------|-------------|
| `.Then(float)` | Delay in seconds |
| `.Then(Action)` | Execute action |
| `.Then(Timer, duration)` | Any timer type |
| `.ThenRepeat(interval, count, onTick)` | Repeating action |
| `.WaitUntil(() => condition)` | Wait until true |
| `.WaitWhile(() => condition)` | Wait while true |
| `.Parallel(step1, step2, ...)` | Run steps in parallel |

### Examples

```csharp
// Wait for condition
TimerChain.Start(1f)
    .WaitUntil(() => player.IsReady)
    .Then(() => StartGame())
    .Run();

// Parallel execution
TimerChain.Start(0f)
    .Parallel(
        new DelayStep(2f),
        new RepeatStep(0.5f, 4, () => Flash())
    )
    .Then(() => Debug.Log("Both complete!"))
    .Run();

// Custom steps
public class MyStep : IChainStep
{
    public float Duration => 1f;
    public void Execute(Action onComplete) { /* ... */ onComplete(); }
    public void Pause() { }
    public void Resume() { }
    public void Dispose() { }
}

TimerChain.Start(new MyStep()).Run();
```

### Control

```csharp
var chain = TimerChain.Start(5f).Run();
chain.Pause();
chain.Resume();
chain.Stop();
chain.Dispose();
```

---

## Timer Groups

Group multiple timers for collective control.

### Basic Usage

```csharp
// Create a group and add timers
var group = new TimerGroup("Enemies");
group.Add(timer1).Add(timer2).Add(timer3);

// Control all timers at once
group.PauseAll();
group.ResumeAll();
group.StopAll();
group.ResetAll();
group.SetTimeScale(0.5f);
```

### Named Groups with Registry

```csharp
// Get or create by name (global registry)
var uiGroup = TimerGroups.GetOrCreate("UI");
uiGroup.Add(fadeTimer, animTimer);

// Control groups by name from anywhere
TimerGroups.Pause("UI");
TimerGroups.Resume("UI");
TimerGroups.Stop("Enemies");

// Pause/Resume ALL groups
TimerGroups.PauseAll();
TimerGroups.ResumeAll();
```

### Cleanup

```csharp
group.Clear();              // Remove all timers from group
group.Dispose();            // Dispose group only
group.Dispose(true);        // Dispose group AND all timers in it
```

---

## Thread Safety

The timer system supports two threading modes:

### SingleThread Mode *(Default)*

Optimized for maximum performance on the main Unity thread.

```csharp
TimerManager.ThreadMode = TimerThreadMode.SingleThread;
```

| Pros | Cons |
|------|------|
| ✅ Best performance | ⚠️ Main thread only |
| ✅ Zero allocations during updates | |

### ThreadSafe Mode

Safe for async operations and multi-threaded access.

```csharp
TimerManager.ThreadMode = TimerThreadMode.ThreadSafe;
```

| Pros | Cons |
|------|------|
| ✅ Safe from any thread | ⚠️ Small performance overhead |
| ✅ Works with async/await | |

> [!NOTE]
> Set `ThreadMode` before creating any timers for best results.

### Thread-Safe Integration Example

```csharp
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.UnityImportPackage.Timers;

public class AsyncDataLoader : MonoBehaviour
{
    private CountdownTimer _timeoutTimer;
    private bool _dataLoaded;

    async void Start()
    {
        // Enable thread-safe mode
        TimerManager.ThreadMode = TimerThreadMode.ThreadSafe;

        // Create a timeout timer on the main thread
        _timeoutTimer = new CountdownTimer(10f);
        _timeoutTimer.OnTimerStop += OnTimeout;
        _timeoutTimer.Start();

        // Load data on a background thread
        await Task.Run(async () =>
        {
            await LoadDataFromServer();
            
            // Safe to dispose from background thread!
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;
        });

        _dataLoaded = true;
        Debug.Log("Data loaded successfully!");
    }

    private async Task LoadDataFromServer()
    {
        // Simulate network delay
        await Task.Delay(3000);
    }

    private void OnTimeout()
    {
        if (!_dataLoaded)
        {
            Debug.LogError("Data loading timed out!");
        }
    }

    void OnDestroy()
    {
        _timeoutTimer?.Dispose();
    }
}
```

### Checking Thread Context

```csharp
if (TimerManager.IsMainThread)
{
    // Direct timer operations
}
else
{
    // Consider using ThreadSafe mode
}
```

---

## Network Synchronization

For multiplayer games, use the networked timer variants.

### Network Modes

| Mode | Description |
|------|-------------|
| `Local` | No network sync *(default)* |
| `Owner` | This client controls the timer and broadcasts state |
| `Remote` | Receives state from the network, cannot control directly |

### Basic Usage

```csharp
// Host/Owner
var timer = new NetworkedCountdownTimer(60f, "match-timer", TimerNetworkMode.Owner);
timer.OnNetworkSync += SendToOtherClients;
timer.Start();

// Remote Client
var remoteTimer = new NetworkedCountdownTimer(60f, "match-timer", TimerNetworkMode.Remote);
```

### Receiving Network State

```csharp
void OnTimerStateReceived(TimerNetworkState state, float latency)
{
    if (remoteTimer.TimerId == state.TimerId)
    {
        remoteTimer.ApplyNetworkState(state, latency);
    }
}
```

### Lag Compensation

```csharp
// Automatically compensates:
// - Countdown: subtracts latency from remaining time
// - Stopwatch: adds latency to elapsed time
timer.ApplyNetworkState(state, networkLatency: 0.05f);
```

### Sync Interval

```csharp
timer.SyncInterval = 0.5f; // Sync every 500ms (default)
timer.SyncInterval = 0.1f; // More responsive
```

### Network Integration Example

```csharp
using Eraflo.UnityImportPackage.Timers;

public class MatchTimer : MonoBehaviour
{
    private NetworkedCountdownTimer _timer;

    void Start()
    {
        bool isHost = /* your network check */;

        _timer = new NetworkedCountdownTimer(
            duration: 300f,
            timerId: "match",
            networkMode: isHost ? TimerNetworkMode.Owner : TimerNetworkMode.Remote
        );

        if (isHost)
        {
            _timer.OnNetworkSync += state => {
                // Send via your network layer (Netcode, Photon, Mirror, etc.)
                MyNetworkManager.Send("timer-sync", state);
            };
            _timer.Start();
        }
    }

    public void OnNetworkMessage(TimerNetworkState state)
    {
        _timer.ApplyNetworkState(state, GetNetworkLatency());
    }

    void OnDestroy() => _timer?.Dispose();
}
```

---

## Advanced Usage

### Using Unscaled Time

For timers that should ignore `Time.timeScale` (pause menus, UI):

```csharp
var pauseTimer = new CountdownTimer(3f);
pauseTimer.UseUnscaledTime = true;
pauseTimer.Start();
```

### Creating Custom Timers

Extend the `Timer` class:

```csharp
public class PingPongTimer : Timer
{
    private bool _countingDown = true;

    public PingPongTimer(float duration) : base(duration) { }

    public override bool IsFinished => false;

    public override void Tick(float deltaTime)
    {
        if (_countingDown)
        {
            CurrentTime -= deltaTime;
            if (CurrentTime <= 0) { CurrentTime = 0; _countingDown = false; }
        }
        else
        {
            CurrentTime += deltaTime;
            if (CurrentTime >= initialTime) { CurrentTime = initialTime; _countingDown = true; }
        }
    }
}
```

### Memory Management

> [!IMPORTANT]
> Always call `Dispose()` when you're done with a timer to prevent memory leaks.

```csharp
public class EnemySpawner : MonoBehaviour
{
    private FrequencyTimer _spawnTimer;

    void Start()
    {
        _spawnTimer = new FrequencyTimer(1);
        _spawnTimer.OnTick += SpawnEnemy;
        _spawnTimer.Start();
    }

    void OnDestroy() => _spawnTimer?.Dispose();
}
```

---

## How It Works

The timer system uses Unity's Player Loop API to inject an update step after `Update.ScriptRunBehaviourUpdate`:

```
Player Loop Order:
├── Initialization
├── EarlyUpdate
├── FixedUpdate
├── PreUpdate
├── Update
│   ├── ScriptRunBehaviourUpdate  ← Your Update() methods
│   └── TimerUpdate               ← Timer system updates here
├── PreLateUpdate
├── PostLateUpdate
└── ...
```

**Benefits:**
1. No MonoBehaviour required
2. Centralized updates in one pass
3. Consistent timing with `Update()`

---

## See Also

- [EventBus](EventBus.md) - Event system for decoupled communication
