# Handler Patterns - ISLAMU Event Conventions

## Repository Usage in Handlers

**CRITICAL RULE**: Handlers use repositories (not DbContext directly).

**Real Example from Event**:
```csharp
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
namespace Explore.Application.Features.Events.Handlers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;  // ✅ Repository interfaces
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;  // ✅ Repository interface
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    
    // ✅ Inject repositories, not DbContext
    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IStorageObjectRepository storageObjectRepository,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _storageObjectRepository = storageObjectRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validation...
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;

        // ✅ Use repository methods
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;
    }
}
```

**Why Repositories?**
- ✅ Application layer doesn't depend on EF Core
- ✅ Can mock repositories for unit tests
- ✅ Encapsulates complex queries
- ✅ Consistent data access patterns

## AutoMapper Usage

**Real Example from GetEventListRequestHandler**:
```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
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
        // ✅ Repository returns List<Event> (entities)
        var events = await _eventRepository.GetEventsWithDetails();

        // ✅ AutoMapper maps entities → DTOs
        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

**Common AutoMapper Patterns**:
```csharp
// Map DTO → Entity (Create)
var entity = _mapper.Map<Event>(request.EventDto);

// Map DTO → Existing Entity (Update)
_mapper.Map(request.EventDto, existingEntity);

// Map Entity → DTO (Read)
var dto = _mapper.Map<EventDto>(entity);

// Map Entity Collection → DTO Collection (List)
var dtos = _mapper.Map<List<EventListDto>>(entities);
```

## CancellationToken Pattern

**Always include `CancellationToken` parameter in Handle method**:

```csharp
public async Task<BaseCommandResponse<Guid>> Handle(
    CreateEventCommand request,
    CancellationToken cancellationToken)  // ✅ Always include
{
    // Pass cancellationToken to all async operations that accept it (e.g., validator, EF Core queries).
    // Note: some repository methods in this codebase don't currently accept a CancellationToken.
    await _eventRepository.Create(@event);
}
```

**Why?**
- ✅ Enables request cancellation
- ✅ Prevents wasted work when client disconnects
- ✅ Required by MediatR `IRequestHandler<TRequest, TResponse>`

## Manual Validator Instantiation Pattern

**CRITICAL PATTERN**: Validators are instantiated manually with dependencies, NOT injected via DI.

**Real Example from CreateEventCommandHandler**:
```csharp
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // ✅ CRITICAL: Manual validator instantiation with all required repositories
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

    // Continue with business logic...
}
```

**Why Manual Instantiation?**
- ✅ Fine-grained control over dependencies
- ✅ Validators can have different repository dependencies per use case
- ✅ No need to register validators in DI container
- ✅ Clear and explicit

## Command Handler Pattern (Create)

**Full Pattern**:
```csharp
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // 1. Manual validation
    var validator = new CreateEventDtoValidator(/* repositories */);
    var validationResult = await validator.ValidateAsync(request.EventDto);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Event creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 2. Map DTO → Entity
    var @event = _mapper.Map<Event>(request.EventDto);

    // 3. Set default values (not in DTO)
        @event.TotalViews = 0;
        @event.TenantId = _tenantContext.TenantId;

    // 4. Save through repository
    @event = await _eventRepository.Create(@event);

    // 5. Return success response
    response.Success = true;
    response.Id = @event.Id;
    response.Message = "Event created successfully.";

    return response;
}
```

## Command Handler Pattern (Update)

**Full Pattern**:
```csharp
public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // 1. Validate
    var validator = new UpdateEventDtoValidator(/* repositories */);
    var validationResult = await validator.ValidateAsync(request.EventDto);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Event update failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 2. Get existing entity
    var @event = await _eventRepository.GetById(request.EventDto.Id);

    if (@event == null)
    {
        response.Success = false;
        response.Message = "Event not found.";
        return response;
    }

    // 3. Map DTO → Existing Entity
    _mapper.Map(request.EventDto, @event);

    // 4. Update through repository
    await _eventRepository.Update(@event);

    // 5. Return success response
    response.Success = true;
    response.Id = @event.Id;
    response.Message = "Event updated successfully.";

    return response;
}
```

## Command Handler Pattern (Delete)

**Full Pattern**:
```csharp
public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
{
    // 1. Get entity
    var @event = await _eventRepository.GetById(request.Id);

    // 2. Check if exists
    if (@event == null)
        return false;

    // 3. Delete through repository
    await _eventRepository.Delete(@event);

    // 4. Return success
    return true;
}
```

**Note**: Delete returns `bool`, not `BaseCommandResponse<Guid>`.

## Query Handler Pattern (List)

**Full Pattern**:
```csharp
public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
{
    // 1. Get entities from repository
    var events = await _eventRepository.GetEventsWithDetails();

    // 2. Map entities → DTOs
    return _mapper.Map<List<EventListDto>>(events);
}
```

**Key Points**:
- ❌ No validation needed for queries
- ❌ No BaseCommandResponse wrapper
- ✅ Return DTOs directly
- ✅ Repository returns entities, handler maps to DTOs

## Query Handler Pattern (Details)

**Full Pattern**:
```csharp
public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
{
    // 1. Get entity from repository
    var @event = await _eventRepository.GetEventWithDetails(request.Id);

    // 2. Return null if not found (controller handles NotFound)
    if (@event == null)
        return null;

    // 3. Map entity → DTO
    return _mapper.Map<EventDto>(@event);
}
```

**Key Points**:
- ✅ Return `null` for not found (not exception)
- ✅ Controller checks for null and returns `NotFound()`
- ✅ Simple and straightforward

## Error Handling Pattern

**Commands**:
```csharp
// Validation errors
if (!validationResult.IsValid)
{
    response.Success = false;
    response.Message = "Operation failed.";
    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
    return response;
}

// Not found errors
if (entity == null)
{
    response.Success = false;
    response.Message = "Entity not found.";
    return response;
}

// Success
response.Success = true;
response.Id = entity.Id;
response.Message = "Operation successful.";
return response;
```

**Queries**:
```csharp
// Not found
if (entity == null)
    return null;  // Let controller handle NotFound()

// Success
return _mapper.Map<EventDto>(entity);
```

## Dependency Injection Registration

**Handlers are auto-registered by MediatR**:
```csharp
// File: Explore.API/Program.cs
using Explore.Application.Profiles;

// Register MediatR (scans assembly for handlers)
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly));

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

**No need to manually register each handler!**

## Common Patterns Summary

| Pattern | Command | Query |
|---------|---------|-------|
| **Response Type** | `BaseCommandResponse<Guid>` (or `bool` for Delete) | DTO type directly |
| **Validation** | Manual with validator | Not needed |
| **Repository** | CRUD operations | Read with `.Include()` |
| **Mapping** | DTO → Entity | Entity → DTO |
| **Error Handling** | Success flag + errors | Return null |
| **Controller** | Check `Success` flag | Check for null |

---

**See Also**:
- [command-patterns.md](command-patterns.md) - Command request/handler structure
- [query-patterns.md](query-patterns.md) - Query request/handler structure
- [validation-integration.md](validation-integration.md) - FluentValidation patterns
