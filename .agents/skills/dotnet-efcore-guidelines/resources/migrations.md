ABOUTME: EF Core migration rules and commands for this project.
ABOUTME: Covers creation, safety constraints, naming, and rollback patterns.

# Migrations

## Commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> \
    --context ExploreDbContext \
    --project src/Explore.Persistence/Explore.Persistence.csproj \
    --startup-project src/Explore.API/Explore.API.csproj \
    --configuration Release

# Apply migrations only through the reviewed rollout target
dotnet ef database update <TargetMigration> \
    --context ExploreDbContext \
    --project src/Explore.Persistence/Explore.Persistence.csproj \
    --startup-project src/Explore.API/Explore.API.csproj \
    --configuration Release

# Generate SQL script for review (recommended before applying)
dotnet ef migrations script \
    --context ExploreDbContext \
    --project src/Explore.Persistence/Explore.Persistence.csproj \
    --startup-project src/Explore.API/Explore.API.csproj \
    --configuration Release \
    --idempotent \
    --output /tmp/explore-migration.sql

# Remove last unapplied migration
dotnet ef migrations remove \
    --context ExploreDbContext \
    --project src/Explore.Persistence/Explore.Persistence.csproj \
    --startup-project src/Explore.API/Explore.API.csproj \
    --configuration Release

# Verify the model has no ungenerated changes
dotnet ef migrations has-pending-model-changes \
    --context ExploreDbContext \
    --project src/Explore.Persistence/Explore.Persistence.csproj \
    --startup-project src/Explore.API/Explore.API.csproj \
    --configuration Release
```

## Safety Rules

| Rule | Rationale |
|------|-----------|
| Never edit applied migrations | Breaks migration history; create corrective migration instead |
| Never remove applied migrations | Other environments depend on the migration chain |
| Keep migrations small and focused | Easier review, safer rollback, faster application |
| Review generated SQL before applying | Catch destructive changes (column drops, type changes) |
| Name migrations descriptively | `AddOutboxMessageTable` not `Migration20260327` |

## Naming Conventions

Use PascalCase verb-noun format describing the change:
- `AddOutboxMessageTable`
- `AddActorAppearanceColumns`
- `AddFooterLinkGroupTable`
- `RenameEventStatusColumn`

## MigrationService

`src/Explore.AppHost/AppHost.cs` registers `src/Event.MigrationService/Event.MigrationService.csproj` as a separate project and makes the API wait for it. Its worker applies every pending migration, applies model-owned PostgreSQL constraints, runs `DatabaseSeeder`, and then exits. Operator-selected staged migrations must therefore use an explicit migration target rather than letting one startup run collapse expand, backfill, and contract stages.

## Seed Data

For new or updated Event Location Privacy lookup families:

1. Give every Domain enum value an explicit stable integer ID and stable uppercase `MasterCode`.
2. Keep entity configuration structural (`ValueGeneratedNever`, unique code, relationships); do not use model `HasData()` while EF Core #36682 applies.
3. Add the required rows to `LookupTableSeeder` through the existing `SeedMissingLookupRowsAsync` pattern so runtime startup repairs rows missing by stable ID.
4. Insert lookup rows with migration-local `InsertData` before any SQL or column backfill that depends on them; runtime seeding happens only after all pending migrations finish.
5. Test enum/ID/code parity and review generated SQL before applying the migration.

The helper repairs absent IDs and does not overwrite an existing row's code or label. Treat parity mismatches as defects. Do not claim every legacy lookup seeder already repairs individual missing rows; this invariant applies to new and updated Event Location Privacy lookup families.

## Verification

```bash
dotnet build Explore.slnx --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Related

- [entity-configuration.md](entity-configuration.md)
