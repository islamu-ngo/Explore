ABOUTME: Entity configuration rules for EF Core in this project.
ABOUTME: Highlights TPT mapping and uuidv7 defaults.

# Entity Configuration (Lean)

## Rules
- Use `IEntityTypeConfiguration<T>` per entity.
- Use TPT mapping strategy (project standard).
- Use `uuidv7()` as default key generation.
- DB defaults allowed in config (not in domain entities).
- Use `DeleteBehavior.Restrict` for most FK relationships.

## Related
- [named-query-filters.md](named-query-filters.md)
