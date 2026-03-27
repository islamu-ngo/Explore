ABOUTME: Common Clean Architecture violations with before/after code examples.
ABOUTME: Use with dependency-rules.md and fix-patterns.md for remediation.

# Common Violations

## 1. Domain References EF Core or Application

```csharp
// WRONG — Domain depends on Infrastructure
using Microsoft.EntityFrameworkCore;
namespace Event.Domain.Entities;
public class Event
{
    [Column("title")] // EF attribute in Domain
    public string Title { get; set; }
}

// CORRECT — Domain is dependency-free
namespace Event.Domain.Entities;
public class Event
{
    public string Title { get; set; }
}
// Column mapping goes in Persistence/Configurations/EventConfiguration.cs
```

## 2. Application Uses DbContext Directly

```csharp
// WRONG — Application depends on Infrastructure
public class GetEventsHandler(ExploreDbContext context) { }

// CORRECT — Application depends on interface
public class GetEventsHandler(IEventRepository repository) { }
// Repository implementation lives in Persistence layer
```

## 3. Application Returns ASP.NET Types

```csharp
// WRONG — Application knows about HTTP
using Microsoft.AspNetCore.Mvc;
public class CreateEventHandler
{
    public async Task<IActionResult> Handle(...) { }
}

// CORRECT — Application returns domain response
public class CreateEventHandler
{
    public async Task<BaseCommandResponse<Guid>> Handle(...) { }
}
// Controller maps to IActionResult
```

## 4. Domain Uses DataAnnotations for Validation

```csharp
// WRONG — Validation framework in Domain
using System.ComponentModel.DataAnnotations;
public class Event
{
    [Required, MaxLength(200)]
    public string Title { get; set; }
}

// CORRECT — Use FluentValidation in Application
public class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
```

## 5. Infrastructure References API/Blazor

```csharp
// WRONG — Infrastructure depends on Presentation
using Event.API.Controllers;

// CORRECT — Infrastructure only references Application and Domain
// Pass data through interfaces defined in Application
```

## 6. Repository Returns DTOs

```csharp
// WRONG — Persistence creates Application-layer types
public async Task<EventDto> GetByIdAsync(Guid id) { }

// CORRECT — Repository returns entities; handler maps to DTOs
public async Task<Event?> GetByIdAsync(Guid id) { }
```

## Detection

Architecture tests (`Event.Architecture.Tests`) enforce these rules automatically:
- `DomainDependencyTests` — Domain has zero external refs
- `ApplicationDependencyTests` — Application only refs Domain
- Naming convention tests verify correct layer placement

## Related

- [dependency-rules.md](dependency-rules.md)
- [fix-patterns.md](fix-patterns.md)
