---
description: Workflow for isolating and fixing bugs in the Catalyst package.
---

# Bug Fixing Workflow

Follow these steps to ensure bugs are fixed permanently and don't introduce regressions.

1. **Reproduction**:
    - Create a new test case in the appropriate `Tests/` directory that reproduces the reported issue.
    - If it's a compilation error, analyze the error message and the target file.
2. **Isolation**:
    - Identify the specific method, class, or dependency causing the issue.
    - Check for recent changes in `Runtime` that might have caused the regression using `grep_search` or `view_file`.
3. **Fixing**:
    - Implement the fix using precise, atomic edits.
    - **Consult Memory**: Check `.agent/project-memory.md` to see if similar bugs occurred.
4. **Verification**:
    - Run the reproduction test to verify the fix.
    - Run **all** relevant tests in the module to check for side effects.
5. **Memory Update**:
    - Document the bug and the fix in `.agent/project-memory.md`.
    - Update `.agent/code-review-checklist.md` if the bug was caused by an avoidable pattern.
