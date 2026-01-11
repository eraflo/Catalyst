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
- [Event Bus](Core/EventBus.md) - Decoupled messaging.
- [Timers](Core/Timers.md) - Scalable timer and delay system.
- [Pooling](Core/Pooling.md) - High-performance object reuse.
- [Easing](Core/Easing.md) - Math utilities for smooth transitions.

### Modules
- [Scene Flow](Modules/SceneFlow.md) - Complex transition management.
- [Asset Management](Modules/AssetManagement.md) - Reference-counted loading.
- [Settings Manager](Modules/SettingsManager.md) - Modular game options and persistence.
- [Behaviour Tree](Modules/BehaviourTree.md) - Advanced AI and logic sequencing.
- [Networking](Modules/Networking.md) - Netcode for GameObjects integration.

### Persistence & Data
- [Serializer](Persistence/Serializer.md) - Unified JSON serialization system.

### Infrastructure
- [CI/CD](Infrastructure/CICD.md) - Automated testing and deployment.
- [Package Settings](Infrastructure/PackageSettings.md) - Configuration and project setup.

## Changelog

See [CHANGELOG.md](../CHANGELOG.md) for version history.
