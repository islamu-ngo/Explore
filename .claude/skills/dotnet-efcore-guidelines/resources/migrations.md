ABOUTME: EF Core migration rules and commands for this project.
ABOUTME: Covers creation, safety constraints, naming, and rollback patterns.

# Migrations

## Commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> \
    --project Event.Persistence \
    --startup-project Explore.AppHost

# Apply pending migrations
dotnet ef database update \
    --project Event.Persistence \
    --startup-project Explore.AppHost

# Generate SQL script for review (recommended before applying)
dotnet ef migrations script \
    --project Event.Persistence \
    --startup-project Explore.AppHost \
    --idempotent

# Remove last unapplied migration
dotnet ef migrations remove \
    --project Event.Persistence \
    --startup-project Explore.AppHost
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

The project uses `MigrationService` (registered as a hosted service via Aspire) to apply pending migrations at startup in development. In production, migrations are applied via the CLI commands above.

## Seed Data

Lookup tables use `HasData()` in entity configurations. When adding a new lookup:
1. Create the enum in Domain
2. Add the entity configuration with `HasData()` seed
3. Create the migration
4. Verify the seed SQL in the generated migration

## Related

- [entity-configuration.md](entity-configuration.md)
