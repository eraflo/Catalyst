# Catalyst

## Overview

Eraflo Catalyst provides essential Unity tools to accelerate development: Behaviour Tree, Networking, Event Bus, Pooling, and more.

## Getting Started

### Installation

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter: `https://github.com/eraflo/Catalyst.git`

### Quick Start

After installation, the package will be available under the `Eraflo.Catalyst` namespace.

```csharp
using Eraflo.Catalyst;
```

## API Reference

### Core Systems
- [Service Locator](Core/ServiceLocator.md) - The architectural backbone.
- [Blackboard](Core/Blackboard.md) - Hierarchical data sharing and persistence.
- [Settings Manager](Core/SettingsManager.md) - Modular game options and persistent data.
- [Chronos Manager](Core/ChronosManager.md) - Advanced time and slowdown management.
- [Persistence](Core/Persistence.md) - Unified JSON serialization system.

### Modules
- [Behaviour Tree](Modules/BehaviourTree.md) - Advanced AI and logic sequencing.
- [Networking](Modules/Networking.md) - Network abstraction and synchronization.
- [Timers](Modules/Timers.md) - Scalable timer and delay system.
- [Pooling](Modules/Pooling.md) - High-performance object reuse.
- [Event Bus](Modules/EventBus.md) - Decoupled messaging.
- [Easing](Modules/Easing.md) - Math utilities for smooth transitions.
- [Scene Flow](Modules/SceneFlow.md) - Complex transition management.
- [Asset Management](Modules/AssetManagement.md) - Reference-counted loading.
- [Input System](Modules/InputSystem.md) - Input buffering and combo detection.
- [Command System](Modules/CommandSystem.md) - Undo/Redo, Replay, and Networked actions.

### Infrastructure
- [CI/CD](Infrastructure/CICD.md) - Automated testing and deployment.
- [Package Settings](Infrastructure/PackageSettings.md) - Configuration and project setup.