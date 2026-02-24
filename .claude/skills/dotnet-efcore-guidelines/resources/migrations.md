ABOUTME: Migration rules for EF Core in this project.
ABOUTME: Keeps workflow and safety constraints minimal.

# Migrations (Lean)

## Rules
- Use `dotnet ef migrations add <Name> --project {Project}.Persistence`.
- Never edit applied migrations; create a new migration instead.
- Keep migrations small and review generated SQL.
- Do not remove applied migrations.

## Related
- [entity-configuration.md](entity-configuration.md)
