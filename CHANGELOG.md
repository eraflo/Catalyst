# [1.5.0](https://github.com/eraflo/Catalyst/compare/v1.4.6...v1.5.0) (2026-01-12)


### Bug Fixes

* error in ci/cd pipeline imported package ([b95b350](https://github.com/eraflo/Catalyst/commit/b95b35087b1307d0bc2674209863db83cdd4c891))
* missing dependency in ci/cd ([2493129](https://github.com/eraflo/Catalyst/commit/24931290d82c0886eea02bb8834cbd756f2eb4ea))


### Features

* expand network module ([48ea431](https://github.com/eraflo/Catalyst/commit/48ea4315bf42eb9b5c03a31d54aa8a31c7795929))

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.4.6] - 2026-01-11

### Fixed
- **CI/CD**: Fixed changelog update workflow by migrating to standard `semantic-release`.

## [v1.4.5] - 2026-01-11

### Added
- **CI/CD Optimization**: Automated release notes categorization and CI recursion prevention.
- **Standards**: Added naming standards and development guidelines in `.agent/`.

## [v1.4.4] - 2026-01-11

### Added
- **Chronos Manager**: New module for per-component time scaling and unified time management.
- **Service Locator**: Robust auto-discovery and lazily initialized `App` facade.
- **Documentation**: New module technical docs in `.agent/`.

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
