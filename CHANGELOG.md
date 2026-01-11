# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.4.4] - 2026-01-11

### Added
- **Chronos Manager**: New module for per-component time scaling and unified time management.
- **Service Locator**: Robust auto-discovery and lazily initialized `App` facade.
- **Documentation**: New guidelines, naming standards, and module technical docs in `.agent/`.

### Fixed
- **117 Test Failures**: Resolved critical regressions in test runners by robustifying assembly scanning and PlayerLoop injection.
- **TimeScale Latency**: Fixed `ChronosManager` delta time calculation to provide immediate same-frame feedback.

## [v1.4.3] - 2026-01-11

### Changed
- Refactored `ServiceLocator` for high robustness in Unity Test Runner environments.
- Updated `App.cs` to a modern static facade pattern.

## [v1.3.0] - 2026-01-11

### Added
- **Scene Flow**: New module for asynchronous scene loading and loading screen management.

## [v1.2.6] - 2026-01-10

### Added
- **Assets Module**: Abstracted asset loading with provider support (Resources, Addressables).

## [v1.2.5] - 2026-01-10

### Changed
- **Blackboard**: Promoted to a core module for shared state management.

## [v1.2.0] - 2026-01-01

### Added
- **Behaviour Tree**: Initial implementation of the node-based AI framework.

## [v1.1.0] - 2025-12-19

### Added
- **Networking**: Unified network system interface and message routing.

## [v1.0.34] - 2025-12-18

### Added
- **Object Pooling**: High-performance handle-based pooling system.

## [v1.0.13] - 2025-12-18

### Added
- **Timer System**: Handle-based timer service with Burst support.
