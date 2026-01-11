---
description: Workflow for analyzing and planning a coding task.
---

# Planning Workflow

1. **Understand Objective**: Read the user request and identify the core problem/feature.
2. **Context Gathering**:
    - List relevant directories using `list_dir`.
    - Search for related symbols using `grep_search`.
    - View specific class definitions with `view_file` or `view_file_outline`.
    - **Consult Documentation**: Check `Documentation~/` for existing architectural context.
3. **Draft Implementation Plan**:
    - **Precise Steps**: List implementation steps with high granularity and precision.
    - **Visualization**: Create Mermaid diagrams for architectural changes, data flow, or complex execution logic.
    - Identify affected files and modules.
    - Check for breaking changes and dependency impacts.
4. **Verification Plan**:
    - Define exact tests (Unit, Runtime, Integration) to run.
    - Specify manual verification steps if applicable.
5. **Approval**: Present the plan to the user via an implementation plan artifact.
