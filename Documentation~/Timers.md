# Timer System

> Inspired by [GitAmend's Improved Unity Timers](https://github.com/adammyhre/Unity-Improved-Timers) by Adam Myhre.

A high-performance, extensible timer system that integrates directly into Unity's Player Loop.

## Features

- **Player Loop Integration**: Timers run independently of MonoBehaviours
- **Self-Managing**: Timers auto-register/unregister with the TimerManager
- **Multiple Timer Types**: Countdown, Stopwatch, and Frequency timers
- **Scaled/Unscaled Time**: Support for both Time.deltaTime and Time.unscaledDeltaTime
- **Event-Based**: Subscribe to timer events (OnTimerStart, OnTimerStop, OnTick)

## Quick Start

```csharp
using Eraflo.UnityImportPackage.Timers;

// Countdown Timer - counts down to zero
var countdown = new CountdownTimer(5f);
countdown.OnTimerStart += () => Debug.Log("Timer started!");
countdown.OnTimerStop += () => Debug.Log("Timer finished!");
countdown.Start();

// Stopwatch Timer - counts up from zero
var stopwatch = new StopwatchTimer();
stopwatch.Start();
Debug.Log($"Elapsed: {stopwatch.ElapsedTime}");

// Frequency Timer - ticks N times per second
var frequency = new FrequencyTimer(10); // 10 ticks per second
frequency.OnTick += () => Debug.Log("Tick!");
frequency.Start();
```

## Timer Types

### CountdownTimer

Counts down from a duration to zero.

```csharp
var timer = new CountdownTimer(10f); // 10 seconds

timer.OnTimerStart += () => Debug.Log("Started");
timer.OnTimerStop += () => Debug.Log("Complete");

timer.Start();

// In Update or elsewhere
Debug.Log(timer.CurrentTime);  // Remaining time
Debug.Log(timer.Progress);      // 0.0 to 1.0
Debug.Log(timer.IsFinished);    // true when CurrentTime <= 0
```

### StopwatchTimer

Counts up from zero indefinitely.

```csharp
var stopwatch = new StopwatchTimer();
stopwatch.Start();

// Later...
Debug.Log($"Elapsed: {stopwatch.ElapsedTime}s");

stopwatch.Pause();  // Pause the stopwatch
stopwatch.Resume(); // Continue counting
stopwatch.Reset();  // Back to zero
```

### FrequencyTimer

Fires an event N times per second.

```csharp
var ticker = new FrequencyTimer(60); // 60 ticks per second
ticker.OnTick += () => {
    // Called 60 times per second
    ProcessGameLogic();
};
ticker.Start();

Debug.Log(ticker.TickCount); // Total ticks since start
```

## Timer Lifecycle

All timers share these common methods:

| Method | Description |
|--------|-------------|
| `Start()` | Starts or resumes the timer |
| `Stop()` | Stops the timer and fires OnTimerStop |
| `Pause()` | Pauses without firing events |
| `Resume()` | Same as Start() |
| `Reset()` | Resets to initial time |
| `Reset(float)` | Resets with a new duration |
| `Dispose()` | Cleans up the timer |

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentTime` | float | Current time value |
| `IsRunning` | bool | Whether the timer is active |
| `IsFinished` | bool | Whether the timer has completed |
| `Progress` | float | 0.0 to 1.0 progress ratio |
| `UseUnscaledTime` | bool | Use real-time instead of scaled time |

## Events

| Event | Description |
|-------|-------------|
| `OnTimerStart` | Fired when Start() is called |
| `OnTimerStop` | Fired when Stop() is called or timer finishes |
| `OnTick` | (FrequencyTimer only) Fired at specified frequency |

## Using Unscaled Time

For timers that should ignore `Time.timeScale` (e.g., pause menus):

```csharp
var pauseTimer = new CountdownTimer(3f);
pauseTimer.UseUnscaledTime = true; // Ignores Time.timeScale
pauseTimer.Start();
```

## Creating Custom Timers

Extend the `Timer` class to create specialized timers:

```csharp
public class PingPongTimer : Timer
{
    private bool _countingDown = true;
    
    public PingPongTimer(float duration) : base(duration) { }
    
    public override bool IsFinished => false; // Never auto-stops
    
    public override void Tick(float deltaTime)
    {
        if (_countingDown)
        {
            CurrentTime -= deltaTime;
            if (CurrentTime <= 0)
            {
                CurrentTime = 0;
                _countingDown = false;
            }
        }
        else
        {
            CurrentTime += deltaTime;
            if (CurrentTime >= initialTime)
            {
                CurrentTime = initialTime;
                _countingDown = true;
            }
        }
    }
}
```

## Memory Management

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
    
    void OnDestroy()
    {
        _spawnTimer?.Dispose(); // Clean up!
    }
}
```

## How It Works

The timer system uses Unity's Player Loop API to inject an update step after `Update.ScriptRunBehaviourUpdate`. This means:

1. **No MonoBehaviour Required**: Timers work in pure C# classes
2. **Centralized Updates**: All timers are updated in one pass
3. **Consistent Timing**: Timers update at the same point in the frame as Update()

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

## See Also

- [EventBus](EventBus.md) - Event system for decoupled communication
