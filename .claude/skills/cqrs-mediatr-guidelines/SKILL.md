---
name: cqrs-mediatr-guidelines
description: CQRS (Command Query Responsibility Segregation) patterns with MediatR for ISLAMU Event. Covers commands, queries, handlers, validation, and pipeline behaviors.
type: domain
enforcement: suggest
priority: high
---

# CQRS + MediatR Guidelines

## 🎯 Purpose

Provides best practices for implementing **CQRS** (Command Query Responsibility Segregation) using **MediatR** in the ISLAMU Event project. Ensures consistent, testable, and maintainable application logic.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "command", "query", "handler", "mediatr", "cqrs", "validation", "validator"
- Intent patterns: "create feature", "add endpoint", "implement use case"
- File patterns: **/*Command.cs, **/*Query.cs, **/*Handler.cs, **/*Validator.cs
- Content patterns: `IRequest`, `IRequestHandler`, `AbstractValidator`

## 📐 CQRS Pattern Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    CQRS with MediatR                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  WRITE OPERATIONS (Commands)                                │
│  ────────────────────────────                               │
│  ┌─────────────┐   ┌─────────────────┐   ┌──────────────┐  │
│  │  Controller │──▶│ CreateEvent     │──▶│  Event       │  │
│  │  or Page    │   │ Command         │   │  Created ✓   │  │
│  └─────────────┘   │                 │   └──────────────┘  │
│                    │ • Mutates state │                     │
│                    │ • Returns ID    │                     │
│                    │ • Validated     │                     │
│                    └─────────────────┘                     │
│                                                             │
│  READ OPERATIONS (Queries)                                  │
│  ─────────────────────────                                  │
│  ┌─────────────┐   ┌─────────────────┐   ┌──────────────┐  │
│  │  Controller │──▶│ GetEventList    │──▶│  EventDto[]  │  │
│  │  or Page    │   │ Query           │   │  (Read-only) │  │
│  └─────────────┘   │                 │   └──────────────┘  │
│                    │ • Read-only     │                     │
│                    │ • Returns DTOs  │                     │
│                    │ • No mutations  │                     │
│                    └─────────────────┘                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 🔑 Key Principles

1. **Separation**: Commands (write) and Queries (read) are separate
2. **Single Responsibility**: One handler per request
3. **Immutable Requests**: Commands/Queries are records (immutable)
4. **Validation**: FluentValidation in pipeline behavior
5. **Thin Controllers**: Controllers just send requests to MediatR
6. **CancellationToken**: Always pass to async methods

## 📚 Resources

| Resource | Description |
|----------|-------------|
| [command-patterns.md](resources/command-patterns.md) | Command structure, naming, handlers |
| [query-patterns.md](resources/query-patterns.md) | Query structure, pagination, projections |
| [handler-patterns.md](resources/handler-patterns.md) | Handler implementation, DI, error handling |
| [validation-integration.md](resources/validation-integration.md) | FluentValidation pipeline integration |
| [complete-examples.md](resources/complete-examples.md) | End-to-end feature examples |

## ⚡ Quick Reference

### Create a New Feature (Command + Query)

**Step 1: Command (Write Operation)**
```csharp
// File: Explore.Application/Features/Events/Commands/CreateEvent/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Commands.CreateEvent;

using MediatR;

public record CreateEventCommand : IRequest<Guid>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public Guid OrganizationId { get; init; }
}
```

**Step 2: Command Validator**
```csharp
// File: Explore.Application/Features/Events/Commands/CreateEvent/CreateEventCommandValidator.cs
using FluentValidation;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Event must start in the future");
    }
}
```

**Step 3: Command Handler**
```csharp
// File: Explore.Application/Features/Events/Commands/CreateEvent/CreateEventCommandHandler.cs
using MediatR;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Event.Create(request.Title, request.OrganizationId);
        entity.SetSchedule(request.StartsAt, request.EndsAt);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

**Step 4: Query (Read Operation)**
```csharp
// File: Explore.Application/Features/Events/Queries/GetEventById/GetEventByIdQuery.cs
public record GetEventByIdQuery(Guid Id) : IRequest<EventDto?>;

// File: Explore.Application/Features/Events/Queries/GetEventById/GetEventByIdQueryHandler.cs
public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events
            .AsNoTracking()  // Read-only optimization
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        return entity is not null ? _mapper.Map<EventDto>(entity) : null;
    }
}
```

**Step 5: Controller (Thin)**
```csharp
// File: Explore.API/Controllers/EventsController.cs
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = eventId }, eventId);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}
```

## ✅ Do's

- ✅ **DO** use `record` for immutable Commands/Queries
- ✅ **DO** suffix with `Command` or `Query`
- ✅ **DO** suffix handlers with `Handler`
- ✅ **DO** pass `CancellationToken` to all async methods
- ✅ **DO** use `AsNoTracking()` for read-only queries
- ✅ **DO** return DTOs from queries (not entities)
- ✅ **DO** return IDs or simple types from commands
- ✅ **DO** validate inputs with FluentValidation
- ✅ **DO** keep handlers focused (Single Responsibility)
- ✅ **DO** use AutoMapper for entity → DTO mapping

## ❌ Don'ts

- ❌ **DON'T** return entities from queries (use DTOs)
- ❌ **DON'T** put business logic in controllers
- ❌ **DON'T** use `IRequest` without a response type
- ❌ **DON'T** forget `CancellationToken`
- ❌ **DON'T** use `.Result` or `.Wait()` (use `await`)
- ❌ **DON'T** query in commands (use repositories)
- ❌ **DON'T** mutate state in queries
- ❌ **DON'T** throw exceptions for validation (use FluentValidation)

## 🔄 MediatR Pipeline

```
Request
   │
   ▼
[Pre-Processors]     ← Audit logging
   │
   ▼
[Pipeline Behaviors] ← Validation (FluentValidation)
   │                  ← Logging
   ▼                  ← Performance monitoring
[Handler]            ← Your business logic
   │
   ▼
[Post-Processors]    ← Caching
   │
   ▼
Response
```

**Registered in Program.cs**:
```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

    // Pipeline behaviors execute in order
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});
```

## 📖 Deep Dive

For comprehensive guidance:
- **Command Patterns**: [command-patterns.md](resources/command-patterns.md)
- **Query Patterns**: [query-patterns.md](resources/query-patterns.md)
- **Handler Patterns**: [handler-patterns.md](resources/handler-patterns.md)
- **Validation**: [validation-integration.md](resources/validation-integration.md)
- **Complete Examples**: [complete-examples.md](resources/complete-examples.md)

---

**Related Skills**:
- `clean-architecture-rules` - Ensures handlers are in correct layer
- `dotnet-efcore-guidelines` - Database access patterns for handlers
- `backend-dev-guidelines` - Overall backend architecture

**Enforcement Level**: 💡 SUGGEST (Provides guidance, doesn't block)
