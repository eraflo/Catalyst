## [1.8.2](https://github.com/eraflo/Catalyst/compare/v1.8.1...v1.8.2) (2026-01-15)


### Bug Fixes

* netcode backend not spawning properly ([97ee4bc](https://github.com/eraflo/Catalyst/commit/97ee4bc613f63ed3af03e4d84fe14002ad52b3b9))

## [1.8.1](https://github.com/eraflo/Catalyst/compare/v1.8.0...v1.8.1) (2026-01-14)


### Bug Fixes

* change ci/cd to not have crash on first try of ci/cd ([a9ac2b6](https://github.com/eraflo/Catalyst/commit/a9ac2b6806907dd19aefee8d63befb94c7f94925))
* created rules for agent in right location ([c20e244](https://github.com/eraflo/Catalyst/commit/c20e244a38a9187030d2b115cbe45f73d01dba23))

# [1.8.0](https://github.com/eraflo/Catalyst/compare/v1.7.0...v1.8.0) (2026-01-13)


### Features

* **networking:** major architecture refactor and comprehensive documentation ([5aa28e9](https://github.com/eraflo/Catalyst/commit/5aa28e93d632b92fca2a6a32e4189f2fa6b8df47))

# [1.7.0](https://github.com/eraflo/Catalyst/compare/v1.6.0...v1.7.0) (2026-01-13)


### Bug Fixes

* network ownership missing hook ([ecf8973](https://github.com/eraflo/Catalyst/commit/ecf89736f3b472aaab57cf51f9da3ec7e455c357))


### Features

* new HFSM module ([570b8a4](https://github.com/eraflo/Catalyst/commit/570b8a4446d86fd69e4b20af607dc7f4fc2c579a))

# [1.6.0](https://github.com/eraflo/Catalyst/compare/v1.5.0...v1.6.0) (2026-01-12)


### Features

* added command system module ([ba7c623](https://github.com/eraflo/Catalyst/commit/ba7c6238089c3954e35d01058e068b4abcc54081))

# [1.5.0](https://github.com/eraflo/Catalyst/compare/v1.4.6...v1.5.0) (2026-01-12)


### Bug Fixes

* error in ci/cd pipeline imported package ([b95b350](https://github.com/eraflo/Catalyst/commit/b95b35087b1307d0bc2674209863db83cdd4c891))
* missing dependency in ci/cd ([2493129](https://github.com/eraflo/Catalyst/commit/24931290d82c0886eea02bb8834cbd756f2eb4ea))


### Features

* expand network module ([48ea431](https://github.com/eraflo/Catalyst/commit/48ea4315bf42eb9b5c03a31d54aa8a31c7795929))


# [1.4.6](https://github.com/eraflo/Catalyst/compare/v1.4.5...v1.4.6) (2026-01-11)


### Bug Fixes

* fixed changelog update workflow by migrating to standard semantic-release


# [1.4.5](https://github.com/eraflo/Catalyst/compare/v1.4.4...v1.4.5) (2026-01-11)


### Features

* automated release notes categorization and CI recursion prevention
* added naming standards and development guidelines


# [1.4.4](https://github.com/eraflo/Catalyst/compare/v1.4.3...v1.4.4) (2026-01-11)


### Bug Fixes

* resolved critical regressions in test runners by robustifying assembly scanning
* fixed ChronosManager delta time calculation for immediate feedback


### Features

* added Chronos Manager module for per-component time scaling
* added Service Locator with auto-discovery
* added technical documentation for modules


# [1.4.3](https://github.com/eraflo/Catalyst/compare/v1.3.0...v1.4.3) (2026-01-11)


### Bug Fixes

* refactored ServiceLocator for high robustness in Unity Test Runner
* updated App.cs to a modern static facade pattern


# [1.3.0](https://github.com/eraflo/Catalyst/compare/v1.2.6...v1.3.0) (2026-01-11)


### Features

* added Scene Flow module for async loading and loading screens


# [1.2.6](https://github.com/eraflo/Catalyst/compare/v1.2.5...v1.2.6) (2026-01-10)


### Features

* added Assets Module with provider support


# [1.2.5](https://github.com/eraflo/Catalyst/compare/v1.2.0...v1.2.5) (2026-01-10)


### Features

* promoted Blackboard to a core module for shared state management


# [1.2.0](https://github.com/eraflo/Catalyst/compare/v1.1.0...v1.2.0) (2026-01-01)


### Features

* initial implementation of the node-based Behaviour Tree framework


# [1.1.0](https://github.com/eraflo/Catalyst/compare/v1.0.34...v1.1.0) (2025-12-19)


### Features

* added unified network system interface and message routing


# [1.0.34](https://github.com/eraflo/Catalyst/compare/v1.0.13...v1.0.34) (2025-12-18)


### Features

* added high-performance handle-based object pooling system


# [1.0.13](https://github.com/eraflo/Catalyst/compare/v1.0.0...v1.0.13) (2025-12-18)


### Features

* expand network module ([48ea431](https://github.com/eraflo/Catalyst/commit/48ea4315bf42eb9b5c03a31d54aa8a31c7795929))

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
