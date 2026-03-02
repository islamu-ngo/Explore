---
name: dotnet-efcore-guidelines
description: Entity Framework Core best practices for Clean Architecture projects. Covers DbContext, entity configurations, repository pattern, migrations, and PostgreSQL-specific features.
type: domain
enforcement: suggest
priority: high
---

ABOUTME: EF Core rules aligned with Clean Architecture.
ABOUTME: Read referenced resources before applying.

# .NET + Entity Framework Core Guidelines

> **Project-Agnostic EF Core Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
EF Core conventions aligned with Clean Architecture + PostgreSQL.

## When This Skill Activates
- Keywords: ef core, dbcontext, repository, migration, postgres
- File patterns: `**/Persistence/**/*.cs`, `**/*DbContext.cs`, `**/Configurations/**/*.cs`

## Non‑Inferable Rules (Must Follow)
- Repositories return **entities**, not DTOs.
- Default values **not** in domain entities (use handlers or EF config).
- Lookup IDs use `int`, main entities use `Guid`.
- Link table nav props are **readonly**; writes via repository.
- EF Core **named query filters** for soft delete (`SoftDelete`) and tenancy (`Tenant`). Use `IgnoreQueryFilter("SoftDelete")` to show deleted records while still respecting tenant isolation.
- **Pooled DbContext factory**: `ExploreDbContext` uses pooling. Scoped services (`TenantContext`, `CurrentUserService`) are set via **property injection** after creation — not constructor injection. Both can be `null` during migrations/seeding.
- **Snake case naming**: PostgreSQL convention. Configured via Npgsql naming conventions.
- **Specification Pattern**: Complex queries use `IQuerySpecification<T>`. Repository applies specification filters to `IQueryable<T>`. Includes JSONB filtering via PostgreSQL `@>` (JsonContains) and `?` (JsonKeyExists) operators.
- **Npgsql resilience**: Retry 3 attempts with 5s delay, 30s command timeout, split query behavior.

## Resources (Read Before Applying)
- [dbcontext-patterns.md](resources/dbcontext-patterns.md)
- [entity-configuration.md](resources/entity-configuration.md)
- [repository-pattern.md](resources/repository-pattern.md)
- [querying-patterns.md](resources/querying-patterns.md)
- [migrations.md](resources/migrations.md)
- [named-query-filters.md](resources/named-query-filters.md)

## Related Documentation
- [`docs/DOMAIN.md`](../../../docs/DOMAIN.md)
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md)
- [`clean-architecture-rules`](../clean-architecture-rules/SKILL.md)
