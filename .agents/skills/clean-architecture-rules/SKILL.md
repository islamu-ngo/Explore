---
name: clean-architecture-rules
description: "Load for cross-layer refactors, new services/interfaces/repositories, project references, dependency-direction failures, or deciding whether code belongs in Domain, Application, Persistence/Infrastructure, API, or Blazor; not for a local edit wholly inside an established layer pattern."
type: guardrail
enforcement: block
priority: critical
---
<!-- ABOUTME: Clean Architecture dependency guardrail for Explore.Domain, Explore.Application, and layer-boundary work. -->
<!-- ABOUTME: Blocks upward dependencies, misplaced validation, and layer leakage that would violate the repository contract. -->

## Must-Read Docs
- [../../../docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- [../../../docs/CODEBASE_STRUCTURE.md](../../../docs/CODEBASE_STRUCTURE.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)

## Top 5 Invariants
1. Domain has zero external dependencies, so entities and value objects cannot reference EF Core, MediatR, ASP.NET Core, or AutoMapper.
2. Application references Domain only, while Persistence and Infrastructure may reference Application plus Domain but never the other way around.
3. API and Blazor are the composition root and may reference all lower layers because HTTP, routing, rendering, and DI wiring belong there.
4. Validators are manually instantiated inside handlers or services, so `IValidator<T>` must not be injected through DI.
5. The Specification Pattern lives in Application and HATEOAS policies or assemblers live in API because query composition and HTTP affordances have different layer ownership.

## Top 5 Anti-Patterns
1. Domain references EF Core, MediatR, or ASP.NET Core, which breaks inward-only dependency flow and fails architecture checks.
2. Application uses `ExploreDbContext` directly, which bypasses repository abstractions and couples handlers to persistence concerns.
3. A repository returns a DTO, which leaks application mapping concerns into the persistence layer.
4. Domain models use `DataAnnotations` validation, which moves request validation into the wrong layer and undermines handler control.
5. Infrastructure or Domain references API or Blazor code, which creates an upward dependency that violates the composition-root rule.

## Minimal Examples
```csharp
namespace Explore.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;

    public Event(Guid id, string title)
    {
        Id = id;
        Title = title;
    }
}
```

```csharp
namespace Explore.Application.Features.Events.Queries;

public sealed class GetEventByIdHandler(IEventRepository repository)
{
    public async Task<EventDto?> Handle(Guid id, CancellationToken cancellationToken)
    {
        Event? entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null
            ? null
            : new EventDto(entity.Id, entity.Title);
    }
}
```

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CleanArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/NamingConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
