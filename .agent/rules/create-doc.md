---
trigger: model_decision
description: Use this rule when creating or reviewing module documentation files in `Documentation~/`.
---

## Document Structure

All module documentation MUST follow this structure:
  1. **Title** - `# Module Name`
  2. **Brief description** - 1-2 sentences explaining the module purpose
  3. **Table of Contents** - Numbered list linking to all sections
  4. **Sections** - Follow the standard template below

## Standard Sections Template

1. Features - Bullet list of key features
2. Architecture - 1-2 Mermaid diagrams (flowchart, sequence, or class diagram)
3. Quick Start - Complete, copy-paste ready examples with all imports
4-N. Feature Sections - One section per major feature, each with complete code examples
N. API Reference - Tables listing all public members (methods, properties, events)

## Code Example Requirements

**Every code example MUST be:**
- Complete with all `using` statements
- Self-contained (no undefined variables)
- Copy-paste ready into a Unity project
- Inside a proper class/struct declaration

## Mermaid Diagram Rules
- **No quotes inside node labels**: Use `[World Channel]` not `["World" Channel]`
- **No spaces before pipe labels**: Use `-->|label|` not `--> |label|`
- **Quote labels with special chars**: Use `id["Label (info)"]` in class diagrams
- **Max 10-15 nodes** per diagram for readability

## API Reference Tables

Use markdown tables for API references with Method and Description columns.

## Verification Checklist
Before completing documentation:
- [ ] All code examples compile
- [ ] All APIs verified against actual codebase
- [ ] No placeholder text or TODO comments
- [ ] Namespace in examples matches actual namespace
- [ ] All diagrams render correctly
- [ ] Cross-links to related modules work