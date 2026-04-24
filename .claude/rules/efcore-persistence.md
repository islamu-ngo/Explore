---
name: efcore-persistence
description: Apply when editing Explore.Persistence repositories, DbContext, filters, or entity configurations outside migrations.
paths:
  - "Explore.Persistence/**/*.cs"
  - "!Explore.Persistence/Migrations/**/*.cs"
related_skills: [dotnet-efcore-guidelines, clean-architecture-rules]
related_docs: [docs/MULTI_TENANCY.md, docs/CODEBASE_INSIGHTS.md, docs/ARCHITECTURE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Persistence.IntegrationTests, Event.Architecture.Tests]
related_intents: [update-repository-query, add-ef-migration, add-cqrs-handler]
---
<!-- ABOUTME: Path-scoped rules for EF Core persistence code outside migrations. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# EF Core Persistence Rules

> **Applies to:** `Explore.Persistence/**/*.cs` except `Explore.Persistence/Migrations/**/*.cs`.
> **Authority:** Canonical persistence invariants live in `docs/QUICK_REFERENCE.md` and the EF/multi-tenancy docs.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Keep repositories entity-first | Return entities and let Application map them | Shape DTOs inside repositories |
| 2 | Preserve named filters | Keep `SoftDelete` and tenant filters independently controllable | Replace them with blunt `IgnoreQueryFilters()` use |
| 3 | Respect pooled DbContext mechanics | Use property injection patterns for scoped context data | Add scoped ctor dependencies to `ExploreDbContext` |
| 4 | Favor configuration classes | Put EF mapping in `IEntityTypeConfiguration<T>` and related config files | Smear mapping rules across repositories |
| 5 | Track query intent explicitly | Use `AsNoTracking()` for read-only flows and tracked queries only for mutation paths | Leave tracking on by accident everywhere |
| 6 | Keep tenant isolation central | Let middleware + DbContext filters enforce tenant scope | Hand-roll tenant predicates inconsistently |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/MULTI_TENANCY.md`
- `docs/CODEBASE_INSIGHTS.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- `IgnoreQueryFilters()` without a narrowly justified safety case.
- DbContext constructor changes that break pooling/property-injection assumptions.
- Repository code bypassing the specification pattern without a documented reason.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`

## Related

- Intents: `update-repository-query`, `add-ef-migration`, `add-cqrs-handler`
- Agents: `.claude/agents/code-architecture-reviewer.md`, `.claude/agents/codebase-verifier.md`
- Rules: `application-layer.md`, `domain.md`, `efcore-migrations.md`
