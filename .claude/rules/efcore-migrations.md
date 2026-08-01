---
name: efcore-migrations
description: Apply when editing Explore.Persistence migration files or model snapshots.
paths:
  - "src/Explore.Persistence/Migrations/**/*.cs"
related_skills: [dotnet-efcore-guidelines]
related_docs: [docs/CODEBASE_INSIGHTS.md, docs/QUICK_REFERENCE.md, docs/DOMAIN.md]
minimum_tests: [Event.Persistence.IntegrationTests, Event.Architecture.Tests]
related_intents: [add-ef-migration]
---

# EF Core Migration Rules

## Applies To
- `src/Explore.Persistence/Migrations/**/*.cs`

## Path-Specific Constraints
- **Generated Only**: Migration and model-snapshot files are immutable generated output. Never create or edit them by hand.
- **Fix the Source**: Correct entities, `IEntityTypeConfiguration<T>` mappings, `DbContext` setup, lookup seeding, or the repository's migration-generation extension before regenerating.
- **Development Regeneration**: Delete an unapplied development migration with `dotnet ef migrations remove` (or regenerate the reset init when explicitly authorized), then run `dotnet ef migrations add`; inspect and verify the output without patching it.
- **Reversibility**: Always provide a valid `Down` method that correctly reverses the `Up` migration.
- **Named Filters**: Verify that `SoftDelete` and tenant filters remain intact after schema changes.
- **Lookup Parity**: Keep lookup enum IDs and stable `MasterCode` values synchronized with idempotent missing-row repair in `LookupTableSeeder`.
- **Seed Ordering**: Use migration-local `InsertData` before any dependent backfill because runtime seeding runs only after all migrations finish.
- **HasData Guard**: Do not add model `HasData()` while EF Core #36682 applies; existing migration-owned seed history remains immutable.
- **History Integrity**: Never delete, rename, or rewrite migrations that have already been applied or merged; fix the model and generate a corrective migration.
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
