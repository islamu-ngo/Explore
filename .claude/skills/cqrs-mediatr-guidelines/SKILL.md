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
- File patterns: `**/*Command.cs`, `**/*Query.cs`, `**/*Handler.cs`, `**/*Validator.cs`
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
│  ─────────────────────────                                   │
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
8. **Validators Use Manual Instantiation**: Validators are instantiated in handlers, NOT injected via DI
9. **Namespaces**: Prefer file-scoped namespaces, but follow the surrounding file style when working in existing code

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

**Real Example (aligned to current `CreateEventDtoValidator.cs`):**
```csharp
// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
using FluentValidation;
using Explore.Application.Contracts.Persistence;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;

    public CreateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IStorageObjectRepository storageObjectRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _storageObjectRepository = storageObjectRepository;

        // Standard validation rules
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(5000);

        // Foreign key validation with repository
        RuleFor(x => x.AudienceAgeId)
            .NotEmpty().WithMessage("Audience Age is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceAgeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience Age not found");

        RuleFor(x => x.AudienceGenderId)
            .NotEmpty().WithMessage("Audience Gender is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceGenderRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience Gender not found");

        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event Type is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventTypeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Event Type not found");

        // OrganizationId is optional: validate only if provided
        RuleFor(x => x.OrganizationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _organizationRepository.Exists(id.Value);
            })
            .WithMessage("Organization does not exist.");

        // FeaturedImageId is optional: validate only if provided
        RuleFor(x => x.FeaturedImageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _storageObjectRepository.Exists(id.Value);
            })
            .WithMessage("FeaturedImageId does not exist.");
    }
}
```

**Step 3: Command Handler**

**Real Example from Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs:**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;

    public CreateEventCommandHandler(
        IEventRepository eventRepository, 
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IStorageObjectRepository storageObjectRepository, 
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // CRITICAL: Validate using FluentValidation - Validator instantiated manually with dependencies
        var validator = new CreateEventDtoValidator(
            _audienceAgeRepository, 
            _audienceGenderRepository, 
            _eventTypeRepository, 
            _organizationRepository, 
            _storageObjectRepository);
        
        var validationResult = await validator.ValidateAsync(request.EventDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;  // Set non-mapped properties

        // Save through repository
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;
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

**Real Example from Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs:**

```csharp
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

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
```

**Step 6: Controller (Thin)**

**Real Example from Explore.API/Controllers/EventController.cs:**

```csharp
namespace Explore.API.Controllers;

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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventController> _logger;

    public EventController(
        IMediator mediator, 
        IHttpContextAccessor httpContextAccessor, 
        ILogger<EventController> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // GET: api/v1/event
    [HttpGet]
    [EndpointSummary("Get all Events (Conference, Webinar, Workshop ...)")]
    [EndpointDescription("Get A List of all the Events")]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
    {
        var events = await _mediator.Send(new GetEventListRequest());
        return Ok(events);
    }

    // GET: api/v1/event/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Event Details")]
    [EndpointDescription("Get Details of the Event")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id });
        return Ok(@event);
    }

    // POST: api/v1/event
    [HttpPost]
    [EndpointSummary("Create an Event")]
    [EndpointDescription("Create a new event")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event)
    {
        var command = new CreateEventCommand { EventDto = @event };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // PUT: api/v1/event/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update an Event")]
    [EndpointDescription("Update an existing event")]
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
    [EndpointSummary("Delete an Event")]
    [EndpointDescription("Delete an event (only if user owns the organization)")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            var command = new DeleteEventCommand { Id = id, UserId = userId };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Event not found or you don't have permission to delete it" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
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
- ✅ **DO** instantiate validators manually with dependencies (NOT DI inject)
- ✅ **DO** implement `IDisposable` for event cleanup

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
- ❌ **DON'T** inject validators via DI (instantiate manually with dependencies)

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
- `blazor-mudblazor-guidelines` - Blazor component patterns

**Enforcement Level**: 💡 SUGGEST (Provides guidance, doesn't block)
