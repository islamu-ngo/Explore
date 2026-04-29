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

# EF Core Migration Rules

## Applies To
- `Explore.Persistence/Migrations/**/*.cs`

## Path-Specific Constraints
- **Reversibility**: Always provide a valid `Down` method that correctly reverses the `Up` migration.
- **Named Filters**: Verify that `SoftDelete` and tenant filters remain intact after schema changes.
- **Lookup Sync**: Synchronize `enum` changes with `HasData()` seed data in the same migration.
- **History Integrity**: Never rename or rewrite migrations that have already been merged.
- **Snapshot Accuracy**: Ensure the `ModelSnapshot` is updated and reflects the final intended model state.

## Must Read
- [docs/QUICK_REFERENCE.md#auditing-and-soft-delete](../../docs/QUICK_REFERENCE.md#auditing-and-soft-delete)
- [docs/DOMAIN.md](../../docs/DOMAIN.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`

## Related
- Intents: `add-ef-migration`
- Agents: `backend-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `efcore-persistence.md`, `domain.md`
