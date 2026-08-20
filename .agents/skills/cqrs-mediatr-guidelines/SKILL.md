---
name: cqrs-mediatr-guidelines
description: "Load for MediatR/CQRS commands, queries, handlers, validators, pipeline behaviors, cache invalidation, idempotency, or specification-driven reads in `Explore.Application`; not for controller-only or repository-only edits."
type: pattern
enforcement: suggest
priority: high
---
<!-- ABOUTME: CQRS and MediatR guidance for commands, queries, handlers, validators, caching, and specification usage. -->
<!-- ABOUTME: Keeps Explore.Application handlers thin, entity-first, cancellation-aware, and aligned with repository and response contracts. -->

## Must-Read Docs
- [../../../docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- [../../../docs/API.md](../../../docs/API.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)

## Top 5 Invariants
1. Commands return `BaseCommandResponse<Guid>` for create or update work or `bool` for many deletes, while queries return DTOs or `PaginatedResult<TDto>`.
2. Repositories return entities and handler code performs DTO mapping, so projection boundaries stay in Application.
3. Validators are manually instantiated inside handlers and are never injected as `IValidator<T>` through DI.
4. Handlers pass `CancellationToken` end to end and use HybridCache with `GetOrCreateAsync` for queries and `RemoveAsync` for command invalidation.
5. `IQuerySpecification<T>` and immutable fluent composition via `And(...)` live in Application, while repositories apply the specification to `IQueryable<T>`.

## Top 5 Anti-Patterns
1. Mixing command and query logic in one handler creates blurred responsibilities and makes pipeline behavior harder to reason about.
2. Injecting `IValidator<T>` through DI breaks the project validation standard and weakens handler-local control.
3. Returning domain entities from handlers leaks persistence-shaped objects into API or Blazor consumers.
4. Using `ExploreDbContext` directly in handlers bypasses repository abstractions and couples Application to EF Core.
5. Returning `IQueryable` from repositories leaks EF concerns into Application and lets handlers compose persistence logic ad hoc.

## Minimal Examples
```csharp
public sealed record GetEventByIdQuery(Guid Id);

public sealed class GetEventByIdHandler(IEventRepository repository, HybridCache cache)
{
    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var validator = new GetEventByIdQueryValidator();
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        return await cache.GetOrCreateAsync(
            $"events:{request.Id}",
            async token =>
            {
                Event? entity = await repository.GetByIdAsync(request.Id, token);
                return entity is null ? null : new EventDto(entity.Id, entity.Title);
            },
            cancellationToken: cancellationToken);
    }
}
```

```csharp
IQuerySpecification<Event> spec = EventQuerySpecification.Create()
    .WithPublishedOnly()
    .And(EventQuerySpecification.Create().WithTenant(tenantId))
    .And(EventQuerySpecification.Create().WithSearch(searchTerm));
```

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CqrsPatternTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
