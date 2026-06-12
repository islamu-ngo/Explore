ABOUTME: Minimal EF Core querying rules for this project.
ABOUTME: Focuses on performance and safe querying defaults.

# Querying Patterns (Lean)

## Rules
- Use `AsNoTracking()` for read-only queries.
- Prefer projections (`Select`) when only a subset is needed.
- Use `Include/ThenInclude` to avoid N+1 queries.
- Use `AsSplitQuery()` for large include graphs.

## Related
- [repository-pattern.md](repository-pattern.md)
