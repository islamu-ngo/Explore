---
name: cqrs-mediatr-guidelines
description: CQRS (Command Query Responsibility Segregation) patterns with MediatR for ISLAMU Event. Covers commands, queries, handlers, validation, and pipeline behaviors.
type: domain
enforcement: suggest
priority: high
---

# CQRS + MediatR Guidelines

## 🎯 Purpose

Provides best practices for implementing **CQRS** (Command Query Responsibility Segregation) using **MediatR** in ISLAMU Event project. Ensures consistent, testable, and maintainable application logic.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "command", "query", "handler", "mediatr", "cqrs", "validation", "validator"
- Intent patterns: "create feature", "add endpoint", "implement use case"
- File patterns: **/*Command.cs, **/*Query.cs, **/*Handler.cs, **/*Validator.cs
- Content patterns: `IRequest`, `IRequestHandler`, `AbstractValidator`

## 📐 CQRS Pattern Overview

```
┌─────────────────────────────────────────────────────┐
│                    CQRS with MediatR                        │
├─────────────────────────────────────────────────────┤
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
└─────────────────────────────────────────────────────┘
```

## 🔑 Key Principles

1. **Separation**: Commands (write) and Queries (read) are separate
2. **Single Responsibility**: One handler per request
3. **Class Requests**: Commands/Queries are classes (not records)
4. **Validation**: FluentValidation at Application boundary
5. **Thin Controllers**: Controllers just send requests to MediatR
6. **CancellationToken**: Always pass to async methods
7. **Repository Returns Entities**: Handlers map entities to DTOs

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
// File: Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Requests.Commands;

using MediatR;
using Explore.Application.DTOs.Event;

public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}
```

**Step 2: Command Validator**
```csharp
// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
using FluentValidation;
using Explore.Application.Contracts.Persistence;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    // ... other repos for FK checks

    public CreateEventDtoValidator(
        IEventRepository eventRepository,
        ILocationRepository locationRepository)
    {
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
    }

    public CreateEventDtoValidator()
    {
        // Standard validation rules
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500);

        // Date/time validation
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required")
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time");

        // Foreign key validation with repository
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event is required")
            .MustAsync(EventExists)
            .WithMessage("Event not found");

        RuleFor(x => x.LocationId)
            .MustAsync(LocationExists)
            .WithMessage("Location not found");
    }

    private async Task<bool> EventExists(Guid eventId, CancellationToken cancellationToken)
    {
        return await _eventRepository.Exists(eventId);
    }

    private async Task<bool> LocationExists(Guid? locationId, CancellationToken cancellationToken)
    {
        if (!locationId.HasValue) return true;
        return await _locationRepository.Exists(locationId.Value);
    }
}
```

**Step 3: Command Handler**
```csharp
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            // Validate using FluentValidation
            var validator = new CreateEventDtoValidator(_eventRepository, _locationRepository);
            var validationResult = await validator.ValidateAsync(request.EventDto);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            // Map DTO to Entity
            var @event = _mapper.Map<Event>(request.EventDto);

            // Save through repository
            @event = await _eventRepository.Create(@event);

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event created successfully.";

            return response;
        }
    }
}
```

**Step 4: Query (Read Operation)**
```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs
namespace Explore.Application.Features.Events.Requests.Queries;

using MediatR;

public class GetEventListRequest : IRequest<List<EventListDto>>
{
}
```

**Step 5: Query Handler**
```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;

namespace Explore.Application.Features.Events.Handlers.Queries
{
    public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetEventListRequestHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
        {
            // Repository returns ENTITIES
            var events = await _eventRepository.GetEventsWithDetails();

            // AutoMapper maps ENTITIES to DTOs
            return _mapper.Map<List<EventListDto>>(events);
        }
    }
}
```

**Step 6: Controller (Thin)**
```csharp
// File: Explore.API/Controllers/EventController.cs
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventController(IMediator mediator) => _mediator = mediator;

    // GET: api/v1/event
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
    {
        var events = await _mediator.Send(new GetEventListRequest());
        return Ok(events);
    }

    // GET: api/v1/event/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetEventDetailsRequest { Id = id });
        return Ok(result);
    }

    // POST: api/v1/event
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event)
    {
        var command = new CreateEventCommand { EventDto = @event };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // PUT: api/v1/event/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDto @event)
    {
        if (id != @event.Id)
        {
            return BadRequest(new { error = "Event ID mismatch" });
        }

        var command = new UpdateEventCommand { EventDto = @event };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/event/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        var command = new DeleteEventCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new { error = "Event not found or you don't have permission to delete it" });
        }

        return NoContent();
    }
}
```

## ✅ Do's

- ✅ **DO** use classes (not records) for Commands/Queries
- ✅ **DO** suffix with `Command` or `Query`
- ✅ **DO** suffix handlers with `Handler`
- ✅ **DO** pass `CancellationToken` to all async methods
- ✅ **DO** use repositories that return entities (not DTOs)
- ✅ **DO** validate inputs with FluentValidation
- ✅ **DO** keep handlers focused (Single Responsibility)
- ✅ **DO** use AutoMapper for entity → DTO mapping
- ✅ **DO** use `[AllowAnonymous]` for GET endpoints
- ✅ **DO** use `[Authorize]` for POST/PUT/DELETE

## ❌ Don'ts

- ❌ **DON'T** use records (use classes instead)
- ❌ **DON'T** return entities from queries (use DTOs)
- ❌ **DON'T** put business logic in controllers
- ❌ **DON'T** use `IRequest` without a response type
- ❌ **DON'T** forget `CancellationToken`
- ❌ **DON'T** use `.Result` or `.Wait()` (use `await`)
- ❌ **DON'T** query in commands (use repositories)
- ❌ **DON'T** mutate state in queries
- ❌ **DON'T** throw exceptions for validation (use FluentValidation)
- ❌ **DON'T** extract userId without fallback pattern (sub → nameidentifier → sid)

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
