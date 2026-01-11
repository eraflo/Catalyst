# Catalyst Framework Development Guidelines

This document serves as the primary reference for best practices when coding, planning, and maintaining the `Eraflo.Catalyst` framework.

## 🛠️ General Coding Standards
- **Naming**: Use PascalCase for classes, methods, and properties. Use camelCase with an underscore prefix (`_variable`) for private fields.
- **Namespaces**: Must strictly mirror the folder structure (e.g., `Runtime/Core/Chronos/Features/` -> `Eraflo.Catalyst.Core.Chronos.Features`).
- **Asmdefs**: Ensure all new modules have appropriate `.asmdef` files and dependencies are correctly mapped.

## 🏛️ Architectural Patterns
- **Consult Documentation**: Always refer to the files in `Documentation~/` for established architectural patterns before proposing changes.

## 🧪 Testing Best Practices
- **Isolation**: Use `App.Shutdown()` in `[TearDown]` to clear the Service Locator.
- **Networking**: Use `MockNetworkBackend` and `MockBackendFactory` for network tests.
- **Naming**: Tests should follow `{Feature}Tests.cs`.

## 📋 Planning & Execution
- **Strategy**: Always use the `/plan-task` workflow for complex changes.
- **Precision**: Be extremely precise about implementation steps. Avoid vague descriptions.
- **Visualization**: Create Mermaid diagrams for any significant architectural or logic changes (Flow, Data, Class relationships).
- **Atomic Edits**: Prefer multiple small `multi_replace_file_content` calls or sequential tool calls over massive file overwrites.
- **Verification**: Always verify changes by checking for compilation errors and running relevant tests.

## 📝 Documentation Standards
- **Tiered Content**: Provide tutorials and examples for both **Beginner** (getting started) and **Advanced** (deep dives, customization) users.
- **Visuals**: Mandatory inclusion of Mermaid diagrams (Archtecture, Execution Flow, Data Flow) for all core modules and complex features.
- **Consistency**: Any change to `Runtime` logic must be reflected in the corresponding `.md` file in `Documentation~/`.

## 🔄 Automated Workflows
- Refer to `.agent/workflows/` for standard procedures.
