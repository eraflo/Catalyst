# Catalyst Technical Stack & Patterns

This document defines the preferred technologies and patterns for the `Eraflo.Catalyst` framework.

## 📦 Core Libraries
- **Serialization**: `Newtonsoft.Json` (Json.NET) for complex persistence. `BinaryWriter`/`BinaryReader` for networking.
- **Unity Performance**: Use `Unity.Burst`, `Unity.Collections`, and `Unity.Jobs` for high-performance modules (like Timers).
- **Easing**: Internal Evaluate system in `Eraflo.Catalyst.EasingSystem`.

## 🏛️ Patterns
- **Service Management**: Always use the `App` / `ServiceLocator` singleton-like pattern. Services must implement `IGameService`.
- **Timer Backend Pattern**: Separate interface (`ITimerBackend`) from implementation to allow swapping between the Managed (`StandardBackend`) and Burst (`BurstBackend`) versions.
- **Provider Pattern**: Used in Asset Management and Networking (Factory pattern) to allow user-extensibility.

## ⚙️ Project Structure
- **Runtime/**: Core code.
- **Tests/**: NUnit (Editor/Runtime) tests.
- **Documentation~/**: Doxygen/Markdown documentation (hidden from Unity project view via `~`).
- **Samples~/**: Example usage (hidden from Unity project view via `~`).

## 🧪 Testing Tools
- **Mocking**: Use `MockNetworkBackend` for networking. Avoid external mocking libraries to keep dependencies minimal.
- **Verification**: Use `Assert` from NUnit and `yield return null` or `yield return new WaitForSeconds` for async tests.
