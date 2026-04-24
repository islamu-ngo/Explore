---
name: domain
description: Apply when editing Explore.Domain entities, value objects, enums, or domain services.
paths:
  - "Explore.Domain/**/*.cs"
related_skills: [clean-architecture-rules]
related_docs: [docs/DOMAIN.md, docs/ARCHITECTURE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Domain.UnitTests, Event.Architecture.Tests]
related_intents: [add-ef-migration]
---
<!-- ABOUTME: Path-scoped rules for the Domain layer. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# Domain Rules

> **Applies to:** `Explore.Domain/**/*.cs`.
> **Authority:** Domain invariants are canonical in `docs/QUICK_REFERENCE.md` and `docs/DOMAIN.md`.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Keep Domain pure | Stay free of infrastructure, persistence, and presentation dependencies | Reference EF, HTTP, or UI concerns directly |
| 2 | Avoid hidden defaults | Require callers/configuration to set business defaults explicitly | Seed implicit domain property defaults casually |
| 3 | Preserve aggregate typing conventions | Use `Guid` for aggregate roots and `int` for lookup-style ids | Introduce `long` outside size/cursor scenarios |
| 4 | Keep navigation writes controlled | Use readonly navigation patterns and repository-mediated writes | Mutate relationship collections as the write model |
| 5 | Maintain audit/soft-delete contracts | Model entities so persistence can apply auditing and soft-delete rules cleanly | Omit expected lifecycle fields from auditable entities |
| 6 | Keep file structure aligned | Use ABOUTME headers and file-scoped namespaces on new files | Add block-scoped namespaces or undocumented file shapes |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/DOMAIN.md`
- `docs/ARCHITECTURE.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Domain types depending on persistence configuration or controller contracts.
- Backfilling convenience defaults that hide business intent.
- Treating layer-3 custom properties as a replacement for typed layer-2 semantics.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Domain.UnitTests`, `Event.Architecture.Tests`

## Related

- Intents: `add-ef-migration`
- Agents: `.claude/agents/clean-code-architect.md`, `.claude/agents/codebase-verifier.md`
- Rules: `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`
