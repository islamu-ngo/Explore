---
name: clean-architecture-rules
description: Enforces Clean Architecture dependency rules (Domain → Application → Infrastructure → API/Blazor). Blocks violations to maintain architectural integrity.
type: guardrail
enforcement: block
priority: critical
---

ABOUTME: Clean Architecture dependency guardrails.
ABOUTME: Read referenced resources before applying.

# Clean Architecture Dependency Rules

> **Project-Agnostic Clean Architecture Guidelines**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
**Critical guardrail**: dependencies flow inward only. Violations are **BLOCKED**.

## When This Skill Activates
- Keywords: dependency, reference, architecture, layer
- File patterns: `Domain/**/*.cs`, `Application/**/*.cs`

## Non‑Inferable Rules (Must Follow)
- Domain has **no dependencies** (pure C#).
- Application references **Domain only**.
- Persistence/Infrastructure reference **Application + Domain**.
- API/Blazor is the composition root (can reference all).
- **Manual validator instantiation** (no DI).

## Resources (Read Before Applying)
- [dependency-rules.md](resources/dependency-rules.md)
- [layer-responsibilities.md](resources/layer-responsibilities.md)
- [violation-examples.md](resources/violation-examples.md)
- [fix-patterns.md](resources/fix-patterns.md)

**Enforcement Level**: BLOCK
**Override**: Add `@skip-architecture-check` comment in file (use sparingly)
