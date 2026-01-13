---
trigger: model_decision
description: This document stores key technical decisions, "lessons learned", and context that might be lost across chat sessions.
---

# Agent Project Memory

## 🔑 Key Architectural Decisions
- **Service Locator Usage**: We use `App.Get<T>()` and `App.Register(instance)`. The system is being transitioned from static calls to instance-based calls to improve testability.
- **Chronos-Timer Integration**: Timers are scale-aware. The `ITimer` interface was expanded with a `Channel` property. Backends (`Standard` & `Burst`) apply the scaling factor from `ChronosManager` to the `deltaTime` before calling `timer.Tick(dt)`.
- **Burst Support**: `BurstBackend` uses a `NativeList<float> _channelScales` to pass localized time scales down to parallel update jobs.

## 🐛 Notable Bugs & Fixes
- **Timer Compilation Error (CS0535)**: Occurred because `ITimer` added `string Channel { get; set; }` but the concrete structs (`DelayTimer`, etc.) hadn't implemented it.
    - *Fix*: Added `_channel` field and public `Channel` property to all timer types.
- **BurstBackend Regression**: During a refactor, `var timer = new T()` was accidentally deleted in `Create<T>`, causing CS0103.
    - *Lesson*: Always verify variable declarations after complex `multi_replace_file_content` calls.
- **Service Locator Renaming**: `App.Clear()` was renamed to `App.Shutdown()` during the refactor.
- **Namespace Mismatch**: `ChronosNetworkHandler` was originally in the wrong namespace relative to its folder (`Features`).
    - *Fix*: Aligned namespace to `Eraflo.Catalyst.Core.Chronos.Features`.
- **ServiceLocator Test Regression**: 117 tests failed due to brittle assembly scanning and PlayerLoop access in test runners.
    - *Fix*: Complete clean rewrite of `ServiceLocator` with robust scanning (using `GetName().Name`), resilient filtering, and safe `Initialize()` triggering.
    - *Lesson*: Static initialization in Unity must handle restricted/headless test environments gracefully.
- **Chronos Static Test Failure**: `UIChannel_Remains_Unscaled_During_Pause` failed due to Unity's `timeScale` latency.
    - *Fix*: Re-implemented internal `_globalScale` in `ChronosManager` to guarantee immediate, same-frame delta time updates for synchronous tests.

## 🛠️ Convention & Style
- **Callbacks**: Timers use `TimerCallbacks.Register<T>` and `ICallbackCollector` for a generic, high-performance callback system.
- **Networking**: Mock backends for tests should always be `MockNetworkBackend` and registered via `MockBackendFactory`.
- **MockNetworkBackend tracking**: Updated to include `SentMessages` tracking and `TriggerReceive` alias to support existing `ChronosNetworkTests`.
- **Test Message Verification**: In tests, use `mock.SentMessages.Any(m => m.Type == id)` and `NetworkSerializer.Deserialize<T>(data)` to verify sent messages, as the backend operates at the byte level.
