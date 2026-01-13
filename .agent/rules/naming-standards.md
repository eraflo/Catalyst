---
trigger: always_on
---

# Catalyst Naming Standards

Exact naming conventions for the `Eraflo.Catalyst` framework to ensure high readability and consistency.

## 📁 Files & Folders
- **Folders**: PascalCase. Singular for modules (`Timer`, `Networking`), plural for collections of types (`Types`, `Extensions`).
- **Files**: PascalCase. Match the primary class/struct name in the file.

## 💻 Classes & Structs
- **Interfaces**: Start with `I` (e.g., `ITimer`, `INetworkMessage`).
- **Abstracts/Bases**: End with `Base` if used (e.g., `ProviderBase`).
- **Handles**: End with `Handle` for unique IDs (e.g., `TimerHandle`).
- **Backends**: End with `Backend` (e.g., `BurstBackend`).

## ⚙️ Members
- **Methods**: PascalCase. Use verbs (e.g., `CreateTimer`, `Shutdown`).
- **Properties**: PascalCase. Use nouns (e.g., `CurrentTime`, `IsRunning`).
- **Private Fields**: `_camelCase` with undercore prefix (e.g., `_currentTime`).
- **Constants**: PascalCase (e.g., `DefaultChannel`).
- **Events**: Start with `On` (e.g., `OnComplete`).

## 🌐 Networking
- **Messages**: End with `Message` (e.g., `ChronosSyncMessage`).
- **Handlers**: End with `Handler` (e.g., `ChronosNetworkHandler`).

## 🧪 Tests
- **Classes**: Feature name + `Tests` (e.g., `ChronosTests`).
- **Methods**: `Reason_Effect` or clear descriptive name (e.g., `GlobalScale_Updates_UnityTime`).
