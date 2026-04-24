---
name: efcore-migrations
description: Apply when editing Explore.Persistence migration files or model snapshots.
paths:
  - "Explore.Persistence/Migrations/**/*.cs"
related_skills: [dotnet-efcore-guidelines]
related_docs: [docs/CODEBASE_INSIGHTS.md, docs/QUICK_REFERENCE.md, docs/DOMAIN.md]
minimum_tests: [Event.Persistence.IntegrationTests, Event.Architecture.Tests]
related_intents: [add-ef-migration]
---
<!-- ABOUTME: Path-scoped rules for EF Core migration files. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# EF Core Migration Rules

> **Applies to:** `Explore.Persistence/Migrations/**/*.cs`.
> **Authority:** Migration safety rules are derived from canonical docs; do not restate them wholesale here.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Keep migrations auditable and reversible | Provide disciplined `Up`/`Down` logic with clear intent | Leave destructive or one-way schema edits unexplained |
| 2 | Preserve named soft-delete filtering support | Ensure model changes keep the `SoftDelete` filter model intact | Erode filter naming or bypass support accidentally |
| 3 | Keep lookup dual-track in sync | Update enum-backed seed data and `HasData()` together | Change one side of the lookup contract only |
| 4 | Respect id conventions | Keep aggregate roots on `Guid`/UUIDv7 and lookups on `int` | Introduce inconsistent key strategies in migrations |
| 5 | Treat migration names as history | Add new migrations for follow-up fixes after merge | Rename or rewrite merged migration history |
| 6 | Keep snapshots honest | Let the snapshot reflect the actual model change set | Hand-edit snapshots to hide drift |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/CODEBASE_INSIGHTS.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Post-merge migration renames.
- Seed drift between enum values and `HasData()` rows.
- Data-destructive `Down()` paths without explicit approval.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`

## Related

- Intents: `add-ef-migration`
- Agents: `.claude/agents/code-architecture-reviewer.md`, `.claude/agents/codebase-verifier.md`
- Rules: `efcore-persistence.md`, `domain.md`
