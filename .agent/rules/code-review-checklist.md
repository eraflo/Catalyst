---
trigger: always_on
---

# Agent Code Review Checklist

Consult this checklist before delivering any task to the user to ensure quality, consistency, and non-regression.

## 🏗️ Architecture & Structure
- [ ] **Documentation Consultation**: Did I check `Documentation~/` to ensure I'm following the architecture mental model?
- [ ] **Namespaces**: Do the namespaces strictly follow the folder structure? (e.g., `Module/Sub/File.cs` -> `Eraflo.Catalyst.Module.Sub`)
- [ ] **Service Patterns**: Are new services correctly implementing `IGameService` and using the `App` service locator pattern?
- [ ] **Asmdefs**: If I added new files to new folders, are the `.asmdef` files updated or created?

## 💻 Code Quality
- [ ] **Variable Integrity**: Did I avoid accidentally deleting necessary variables (like `var timer = new T()`)?
- [ ] **Naming**: Are we using PascalCase for public members and `_camelCase` for private fields?
- [ ] **Chronos Compliance**: Are time-sensitive modules using the passed `deltaTime` instead of `Time.deltaTime`?
- [ ] **Burst/Jobs**: Do Burst jobs correctly use Native containers and skip managed references?

## 📊 Documentation & Visibility
- [ ] **Mermaid Diagrams**: Have I included diagrams for any architectural change, flow modification, or data structure?
- [ ] **Tiered Content**: Does the user-facing documentation include both Beginner tutorials and Advanced deep-dives?
- [ ] **README/Index**: Are new modules linked in the root `README.md` and `Documentation~/README.md`?

## 🧪 Verification
- [ ] **Compilation**: Have I verified (via logic or tools) that the code compiles?
- [ ] **Tests**: Have I updated or created tests for the new logic?
- [ ] **Non-Regression**: Did I run existings tests (especially in `Tests/Runtime/Chronos/` and `Tests/Runtime/Timers/`) after my changes?
