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
- EF Core **named query filters** for soft delete/tenancy.

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
