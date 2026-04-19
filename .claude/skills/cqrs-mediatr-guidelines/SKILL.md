---
name: cqrs-mediatr-guidelines
description: CQRS (Command Query Responsibility Segregation) patterns with MediatR for .NET Clean Architecture projects. Covers commands, queries, handlers, validation, and pipeline behaviors.
type: domain
enforcement: suggest
priority: high
---

ABOUTME: CQRS + MediatR rules (commands, queries, handlers, validation).
ABOUTME: Read referenced resources before applying.

# CQRS + MediatR Guidelines

> **Project-Agnostic CQRS Guidelines**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Keep commands/queries separate and handlers clean, with manual validation and entity‑first repos.

## When This Skill Activates
- Keywords: command, query, handler, mediatr, cqrs, validation
- File patterns: `**/*Command.cs`, `**/*Query.cs`, `**/*Handler.cs`, `**/*Validator.cs`

## Non‑Inferable Rules (Must Follow)
- Commands **write**, Queries **read**; never mix.
- Handlers are single‑responsibility, controllers are thin.
- **Repositories return entities**; handlers map to DTOs.
- **Manual validator instantiation** (no DI).
- **Atomic writes**: Use `IUnitOfWork.ExecuteInTransactionAsync` for handlers performing multi‑step writes across repositories to ensure transactional integrity.
- Always pass `CancellationToken`.
- **Pipeline behaviors**: `PerformanceBehavior` logs requests >500ms. `AuthorizationBehavior` checks `IAuthorizedRequest` / `[AuthorizeResource]` / `ISecureRequest` and throws `AuthorizationException` on deny.
- **Specification Pattern**: Complex queries use `IQuerySpecification<T>` (immutable fluent builder). `EventQuerySpecification` composes `EventFilter`, `EventSubqueryFilter`, `AspectPresenceFilter`, `IslamicAspectFilter`, `TechAspectFilter`, and `EventCustomPropertyProjectionFilter` via `And(...)` composition. Module‑conditional filters are silently ignored when module disabled.
- **HybridCache in handlers**: Query handlers use `GetOrCreateAsync()` for read‑through caching. Command handlers call `RemoveAsync()` for cache invalidation. `ToCacheKeySuffix()` generates deterministic keys from specification state.
- **Idempotency**: Write endpoints support `Idempotency-Key` for safe retries. Middleware stores/replays mutation responses by `(Key, TenantId)` within a 24-hour window.
- **Response types**: Create/update → `BaseCommandResponse<Guid>`. Delete → `bool`. Queries → DTO or `PaginatedResult<TDto>`.

## Resources (Read Before Applying)
- [command-patterns.md](resources/command-patterns.md)
- [query-patterns.md](resources/query-patterns.md)
- [handler-patterns.md](resources/handler-patterns.md)
- [validation-integration.md](resources/validation-integration.md)
- [api-endpoint-design.md](resources/api-endpoint-design.md)

## Related Documentation
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md)
- [`clean-architecture-rules`](../clean-architecture-rules/SKILL.md)
