# Catalyst

## Overview

Eraflo Catalyst provides essential Unity tools to accelerate development: Behaviour Tree, Networking, Security, Event Bus, Pooling, and more.

## Getting Started

### Installation

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter: `https://github.com/eraflo/Catalyst.git`

### Quick Start

After installation, the package will be available under the `Eraflo.Catalyst` namespace.

```csharp
using Eraflo.Catalyst;
using Eraflo.Catalyst.Security;
using Eraflo.Catalyst.Networking;
```

## API Reference

### Core Systems
- [Service Locator](Core/ServiceLocator.md) - The architectural backbone.
- [Blackboard](Core/Blackboard.md) - Hierarchical data sharing and persistence.
- [Settings Manager](Core/SettingsManager.md) - Modular game options and persistent data.
- [Chronos Manager](Core/ChronosManager.md) - Advanced time and slowdown management.
- [Persistence](Core/Persistence.md) - Unified JSON serialization system.

### Modules
- [Security](Modules/Security.md) - Cryptographic operations and providers.
- [Networking](Modules/Networking/README.md) - Network abstraction, lobbies, and discovery.
- [Behaviour Tree](Modules/BehaviourTree.md) - Advanced AI and logic sequencing.
- [Timers](Modules/Timers.md) - Scalable timer and delay system.
- [Pooling](Modules/Pooling.md) - High-performance object reuse.
- [Event Bus](Modules/EventBus.md) - Decoupled messaging.
- [Easing](Modules/Easing.md) - Math utilities for smooth transitions.
- [Scene Flow](Modules/SceneFlow.md) - Complex transition management.
- [Spatial](Modules/Spatial.md) - Spatial partitioning and queries.
- [Asset Management](Modules/AssetManagement.md) - Reference-counted loading.
- [Input System](Modules/InputSystem.md) - Input buffering and combo detection.
- [Command System](Modules/CommandSystem.md) - Undo/Redo, Replay, and Networked actions.
- [HFSM](Modules/HFSM.md) - Hierarchical Finite State Machine.
- [Noise](Modules/Noise.md) - High-performance Simplex and Fractal noise.
- [Utilities](Modules/Utilities.md) - Log exporting and helper tools.

### Infrastructure
- [CI/CD](Infrastructure/CICD.md) - Automated testing and deployment.
- [Package Settings](Infrastructure/PackageSettings.md) - Configuration and project setup.