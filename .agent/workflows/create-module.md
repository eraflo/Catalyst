---
description: Workflow for creating a new module in the Catalyst framework.
---

# Create Module Workflow

Follow these steps when tasked with creating a new feature or system module.

1. **Analysis & Design**:
    - Identify the core responsibility and service name.
    - Check `Documentation~/` for overlapping features.
    - Create a Mermaid diagram for the module architecture.

2. **Folder Structure**:
    - Create `Runtime/<ModuleName>/`
    - Subfolders: `Core/`, `Interfaces/`, `Backends/`, `Registries/` (as needed).
    - Create `Tests/Runtime/<ModuleName>/` and `Tests/Editor/<ModuleName>/`.

3. **Core Implementation**:
    - Create the primary service class implementing `IGameService`.
    - Register it in `App` via the `[Service]` attribute or manual registration.
    - Use the namespace `Eraflo.Catalyst.<ModuleName>`.

4. **Assembly Definitions**:
    - Create `<ModuleName>.asmdef` in the module root.
    - Ensure dependencies on `Eraflo.Catalyst.Core` are set.
    - For tests, create a `.asmdef` file with the `Test Assemblies` flag.

5. **Initial Documentation**:
    - Create `Documentation~/Modules/<ModuleName>.md`.
    - Include a **Beginner** "Getting Started" section.
    - Include an **Advanced** "Deep Dive" section.
    - Link the new doc in `Documentation~/README.md`.

6. **Verification**:
    - Create a basic "Smoke Test" to verify service registration.
    - Run the `/fix-bug` workflow if issues occur during creation.
