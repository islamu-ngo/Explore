ABOUTME: DbContext setup rules for EF Core in this project.
ABOUTME: Focuses on configuration discovery and scoped lifetime.

# DbContext Patterns (Lean)

## Rules
- Use `ApplyConfigurationsFromAssembly` in `OnModelCreating`.
- Keep DbContext lifetime scoped (per request).
- Avoid DbContext usage in Application layer; use repositories.

## Related
- [entity-configuration.md](entity-configuration.md)
