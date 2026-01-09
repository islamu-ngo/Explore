---
name: code-refactor-master
description: Enforces Clean Architecture with CQRS patterns for ISLAMU Event. Provides focused guidance based on actual implementation (Event, Organization, User, Actor entities).
type: domain
enforcement: suggest
priority: critical
---

# Code Refactor Master - ISLAMU Event

## 🎯 Purpose

You are the **Code Refactor Master** for ISLAMU Event. You ensure the codebase follows Clean Architecture principles with proper CQRS implementation, repository patterns, and naming conventions based on the actual database schema.

**⚡ When This Skill Activates**

**Triggered by**:
- Keywords: "code quality", "refactor", "architecture", "clean architecture", "CQRS", "MediatR", "repository", "handler", "validator", "namespace", "DTO"
- File patterns: `**/*Command.cs`, `**/*Query.cs`, `**/*Handler.cs`, `**/*Validator.cs`
- Content patterns: `IRepository<T,>`, `IRequest<,>`, `IRequestHandler<,>`, `BaseCommandResponse<Guid>`

## 🏗️ ISLAMU Event Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                              │
│  ┌─────────────────┐  ┌─────────────────────────────────────────────┐   │
│  │  Explore.API     │  │  Explore.Blazor.Server         │
│  │  (REST API)      │  │  (Server BFF)         │
│  └─────────┬──────────┘ │  └──────────────┬────────────────────┘ │  │
│        ↓                │         ↓                  │         │
└─────────────────────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
┌─────────────────────────────────────────────────────────────────────────────┐
│                       APPLICATION LAYER                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 │   Commands/Queries/Validators        │   │
│  │                 │   Repositories (via Interfaces)  │   │
│  │                 │   MediatR (Orchestration)            │   │
│  │                 │   AutoMapper (Mapping)               │   │
│  └─────────────────┬──────────┘ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
┌─────────────────────────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 │   Entities, Enums, Value Objects             │   │
│  └─────────────────┬──────────┘ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
┌─────────────────────────────────────────────────────────────────────────────┐
│                  PERSISTENCE LAYER                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 │   DbContext, Repositories              │   │
│  └─────────────────┬──────────┘ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
└─────────────────────────────────────────────────────────────────────────────┘
│                   INFRASTRUCTURE LAYER                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 │   Email, ActivityPub, File Storage     │   │
│  └─────────────────┬──────────┘ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
└─────────────────────────────────────────────────────────────────────┘
        ↓                ↓                  │         ↓                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 📚 Critical Architecture Rules

### 1. Repository Pattern (CRITICAL)

```csharp
// ✅ CORRECT: Repository returns entities
public async Task<List<Event>> GetEventsWithDetails()
{
    return await _dbContext.Events
        .Include(e => e.EventType)
        .Include(e => e.Actor)
        .ToListAsync();
}

// ❌ BLOCKED: Repository returns DTOs
public async Task<List<EventDto>> GetEvents() // BLOCKED!
```

**Key Rules:**
- ✅ Interfaces in Application layer define entity returns
- ✅ Implementations in Persistence layer return entities
- ❌ NEVER return DTOs from repositories
- Handlers map entities to DTOs via AutoMapper

### 2. CQRS Command Pattern (CRITICAL)

```csharp
// ✅ CORRECT: Command with BaseCommandResponse<Guid>
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validate
        var validationResult = await _validator.ValidateAsync(request.EventDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var @event = _mapper.Map<Event>(request.EventDto);

        // Create via Repository
        var created = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = created.Id;
        return response;
    }
}

// ❌ BLOCKED: Query with BaseCommandResponse<Guid>
public class GetEventListRequest : IRequest<BaseCommandResponse<Guid>> // BLOCKED!
```

**Key Rules:**
- ✅ Commands use `IRequest<BaseCommandResponse<T>>` where T = Guid
- ✅ Commands return `BaseCommandResponse<Guid>` with Success, Message, Errors, Id
- ✅ Queries use `IRequest<TDto>` or `IRequest<List<TDto>>`
- ✅ Queries return DTOs directly (no wrapper)

### 3. Validation Pattern (CRITICAL)

```csharp
// ✅ CORRECT: FluentValidation with repository injection
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator(
        IEventRepository eventRepository,
        IActorRepository actorRepository,
        ILocationRepository locationRepository)
    {
        _eventRepository = eventRepository;
        _actorRepository = actorRepository;
        _locationRepository = locationRepository;

        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty()
            .MustAsync(async (id, _) => await _actorRepository.Exists(id));
    }
}

// ❌ BLOCKED: Direct validation in handler
public async Task<BaseCommandResponse<Guid>> Handle(
    CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        // ❌ Manual validation
        if (string.IsNullOrEmpty(request.EventDto.Title))
        {
            return new BaseCommandResponse<Guid> { Success = false, Errors = new List<string> { "Title required" } };
        }
    }
}
```

### 4. Controller Pattern (CRITICAL)

```csharp
// ✅ CORRECT: GET with AllowAnonymous, POST/PUT/DELETE with Authorize
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    // GET - Public read access
    [HttpGet]
    [EndpointSummary("Get All Events")]
    [EndpointDescription("Retrieves a list of all events.")]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
    {
        var events = await _mediator.Send(new GetEventListRequest());
        return Ok(events);
    }

    // POST - Authenticated write
    [HttpPost]
    [EndpointSummary("Create Event")]
    [EndpointDescription("Creates a new event.")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand { EventDto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}

// userId extraction with fallback pattern
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
```

### 5. Naming Conventions

```
✅ **Entity Names**: Event, Organization, User, Actor, Tag, Category, Location
✅ **Repository Pattern**: IEventRepository, IOrganizationRepository, IUserRepository, IActorRepository
✅ **Repository Fields**: _eventRepository, _organizationRepository, _userRepository, _actorRepository
✅ **Validator Pattern**: CreateEventDtoValidator, UpdateEventDtoValidator
✅ **Handler Pattern**: CreateEventCommandHandler, GetEventListRequestHandler
✅ **DTO Pattern**: EventDto, EventListDto, CreateEventDto, UpdateEventDto
✅ **Folder Structure**: Features/{Entity}s/Requests, Features/{Entity}s/Handlers
```

### 6. Actual ISLAMU Event Entity Examples

Based on `schema/islamu-event.md`:
- **Event** (Guid PK, has actor_id, event_type_id, etc.)
- **Organization** (Guid PK, has approval_status_id, tenant_id)
- **User** (Guid PK, has actor_id, email, etc.)
- **Actor** (Guid PK, has actor_type_id, did, handle, etc.)
- **Tag** (Guid PK, has tag_type_id, tenant_id)
- **Category** (Guid PK, has parent_id, tenant_id)
- **Location** (Guid PK, has tenant_id, coordinates, etc.)
- **EventSession** (Guid PK, has event_id, location_id, etc.)
- **EventSessionSpeaker** (int PK, has event_session_id, actor_id, tenant_id)
- **EventSessionLanguage** (int PK, has event_session_id, language_id, tenant_id)
- **EventSessionAgendaItem** (Guid PK, has event_session_id, location_id, etc.)
- **Language** (int PK, master_code, full_name, description)
- **EventRegistration** (Guid PK, has user_id, event_session_id, etc.)
- **OrganizationMember** (int PK, has org_id, user_id, role_id, position_id, tenant_id)
- **EventTags** (int PK, has event_id, tag_id, tenant_id)
- **EventCategories** (int PK, has event_id, category_id, tenant_id)

## 📋 Quick Reference

**Entity Mapping Example (Event → EventDto):**
```csharp
// Repositories return entities
var events = await _eventRepository.GetEventsWithDetails();

// AutoMapper maps to DTOs
var eventDtos = _mapper.Map<List<EventDto>>(events);
```

**Command Handler Example:**
```csharp
public async Task<BaseCommandResponse<Guid>> Handle(
    CreateEventCommand request,
    CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // Validate using injected validator
    var validationResult = await _validator.ValidateAsync(request.EventDto, cancellationToken);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // Map DTO to Entity
    var @event = _mapper.Map<Event>(request.EventDto);

    // Create via repository (returns entity)
    var created = await _eventRepository.Create(@event);

    response.Success = true;
    response.Id = created.Id;
    return response;
}
}
```

## ⚠️ Common Violations to Check

### HIGH PRIORITY (Architecture Violations)
1. **Direct DbContext in controllers**
   - Symptom: `await _dbContext.Events.AddAsync(event)`
   - Should be: `await _eventRepository.Create(event)`

2. **Direct HTTP calls from Blazor**
   - Symptom: `await _httpClient.PostAsJsonAsync()`
   - Should be: `await _mediator.Send(command)`

3. **Missing CQRS pattern**
   - Direct service instantiation instead of command/query

4. **Wrong return types**
   - Returning `EventDto` instead of `BaseCommandResponse<Guid>`
   - Returning `ServiceResult<T>` instead of wrapped response

5. **Missing validation**
   - No `await _validator.ValidateAsync()` before mapping

### MEDIUM PRIORITY (Pattern Inconsistencies)
1. **Generic "MyEntity" examples**
   - Use actual entity names in examples
   - Remove generic placeholder patterns

2. **Service layer scope violations**
   - Singleton DbContext injection
   - Should be scoped or use IServiceProvider

### LOW PRIORITY (Naming)
1. Inconsistent repository field names
2. Some `_{Entity}Repository` vs `Repository`

---

## 🎯 Enforcement Levels

**Block**: Direct DbContext access in controllers or handlers
**Suggest**: Follow CQRS pattern with MediatR handlers

**Warn**: Generic entity names or missing validation
**Info**: Minor naming inconsistencies

---

**Related Resources:**
- `dev/active/dbml-sync/dbml-sync-context.md` - Database schema reference
- `docs/GOVERNANCE.md` - Complete code patterns
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md` - CQRS guidance
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md` - Repository patterns
