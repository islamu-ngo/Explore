---
name: clean-architecture-rules
description: Enforce inward-only dependencies, correct layer ownership, and repository boundaries in Clean Architecture changes.
type: guardrail
enforcement: block
priority: critical
---
<!-- ABOUTME: Clean Architecture dependency guardrail for Explore.Domain, Explore.Application, and layer-boundary work. -->
<!-- ABOUTME: Blocks upward dependencies, misplaced validation, and layer leakage that would violate the repository contract. -->

## Purpose
Use this skill to stop dependency-direction violations before they spread across Explore.Domain, Explore.Application, Explore.Persistence, and API or Blazor composition code. It protects the layer contract more than any single implementation detail.

## When to Load
- Keywords: dependency, reference, architecture, layer, boundary, composition root.
- File patterns: `Explore.Domain/**/*.cs`, `Explore.Application/**/*.cs`, `Explore.Persistence/**/*.cs`, `Explore.API/**/*.cs`, `Explore.Blazor/**/*.cs`.
- Intent IDs: `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `update-repository-query`, `add-ef-migration`.

## When NOT to Load
- Not for trivial property edits that do not change references, ownership, or boundaries.
- Not for pure naming tweaks with no architectural impact.
- Not for UI-only CSS changes where layer direction is irrelevant.

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
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CleanArchitectureTests`
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.NamingConventionTests`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../dotnet-efcore-guidelines/SKILL.md](../dotnet-efcore-guidelines/SKILL.md)
