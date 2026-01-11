# Complete Feature Example - Event Management

This document shows the **complete CQRS cycle** for the Event entity from the ISLAMU Event codebase, demonstrating all patterns and conventions in a real-world implementation.

---

## Complete CQRS Cycle Overview

```
User Request → Controller → MediatR → Handler → Repository → Entity → Database
                                ↓
                           Validation (manual instantiation)
                                ↓
                           AutoMapper (Entity ↔ DTO)
                                ↓
                           Response (BaseCommandResponse<Guid>)
```

---

## 1. Create Event - Full Command Flow

### Domain Entity

**File**: `Explore.Domain/Event.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class Event
{
    public Guid Id { get; set; }
    
    [ForeignKey("EventType")]
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; }
    
    public string Title { get; set; }
    public string? Description { get; set; }
    
    [ForeignKey("AudienceGender")]
    public int AudienceGenderId { get; set; }
    public AudienceGender AudienceGender { get; set; }
    
    [ForeignKey("AudienceAge")]
    public int AudienceAgeId { get; set; }
    public AudienceAge AudienceAge { get; set; }
    
    [ForeignKey("Actor")]
    public Guid ActorId { get; set; }
    public Actor Actor { get; set; }
    
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }
    
    [ForeignKey("FeaturedImage")]
    public Guid FeaturedImageId { get; set; }
    public StorageObject FeaturedImage { get; set; }
    
    public int TotalViews { get; set; }
    public bool IsRegistrationRequired { get; set; }
    public string? EventUrl { get; set; }
    
    [ForeignKey("EventStatus")]
    public int EventStatusId { get; set; }
    public EventStatus EventStatus { get; set; }
    
    [ForeignKey("VisibilityType")]
    public int VisibilityTypeId { get; set; }
    public VisibilityType VisibilityType { get; set; }
    
    [ForeignKey("EventFormat")]
    public int EventFormatId { get; set; }
    public EventFormat EventFormat { get; set; }
    
    [ForeignKey("Madhab")]
    public int? MadhabId { get; set; }
    public Madhab? Madhab { get; set; }
    
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }
    
    public string? Slug { get; set; }
    public int? SessionCount { get; set; }
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }
    public string? ExternalRegistrationUrl { get; set; }
    
    [ForeignKey("AtprotoRecord")]
    public Guid? AtprotoRecordId { get; set; }
    public AtprotoRecord? AtprotoRecord { get; set; }
    
    // Navigation collections
    public virtual ICollection<EventSession> Sessions { get; set; } = new List<EventSession>();
    public virtual ICollection<EventCategories> EventCategories { get; set; } = new List<EventCategories>();
    public virtual ICollection<EventTags> EventTags { get; set; } = new List<EventTags>();
}
```

### Create DTO

**File**: `Explore.Application/DTOs/Event/CreateEventDto.cs`

```csharp
using System;

namespace Explore.Application.DTOs.Event;

public class CreateEventDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Event Type
    public int EventTypeId { get; set; }

    // Audience
    public int AudienceGenderId { get; set; }
    public int AudienceAgeId { get; set; }

    // Actor (Owner - User or Organization)
    public Guid ActorId { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image
    public Guid FeaturedImageId { get; set; }

    // Registration
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }

    // Status & Visibility
    public int EventStatusId { get; set; }
    public int VisibilityTypeId { get; set; }

    // Format
    public int EventFormatId { get; set; }

    // Islamic Context
    public int? MadhabId { get; set; }

    // Session Info (computed, but can be set initially)
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    // Metadata
    public string? EventUrl { get; set; }

    // Tenant (set by system based on context)
    public Guid TenantId { get; set; }
}
```

### Validator (CRITICAL: Manual Instantiation)

**File**: `Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs`

```csharp
using FluentValidation;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.Event.Validators;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;

    public CreateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;

        // Title validation
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

        // Event Type validation with async database check
        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event Type is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventTypeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Event Type not found");

        // Audience Gender validation
        RuleFor(x => x.AudienceGenderId)
            .NotEmpty().WithMessage("Audience Gender is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceGenderRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience Gender not found");

        // Audience Age validation
        RuleFor(x => x.AudienceAgeId)
            .NotEmpty().WithMessage("Audience Age is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceAgeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience Age not found");

        // Actor validation
        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("Actor is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _actorRepository.Exists(id);
                return exists;
            })
            .WithMessage("Actor not found");

        // Featured Image validation
        RuleFor(x => x.FeaturedImageId)
            .NotEmpty().WithMessage("Featured Image is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _storageObjectRepository.Exists(id);
                return exists;
            })
            .WithMessage("Featured Image not found");

        // Price validation
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).When(x => x.Price.HasValue)
            .WithMessage("Price cannot be negative");

        // Currency validation
        RuleFor(x => x.CurrencyCode)
            .MaximumLength(3)
            .When(x => !string.IsNullOrEmpty(x.CurrencyCode))
            .WithMessage("Currency code must be 3 characters (ISO 4217)");
    }
}
```

### Command Request

**File**: `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs`

```csharp
using MediatR;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;

namespace Explore.Application.Features.Events.Requests.Commands;

public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}
```

### Command Handler (CRITICAL: Shows All Patterns)

**File**: `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // CRITICAL: Manual validator instantiation with all dependencies
        var validator = new CreateEventDtoValidator(
            _audienceAgeRepository, 
            _audienceGenderRepository, 
            _eventTypeRepository, 
            _actorRepository, 
            _storageObjectRepository);
        
        var validationResult = await validator.ValidateAsync(request.EventDto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity using AutoMapper
        var @event = _mapper.Map<Event>(request.EventDto);
        
        // Set properties not in DTO (system-generated values)
        @event.TotalViews = 0;

        // Save through repository
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;
    }
}
```

### Controller (Complete with Authorization)

**File**: `Explore.API/Controllers/EventController.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Explore.API.Controllers;

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

    // POST: api/v1/event
    [HttpPost]
    [Authorize]  // CRITICAL: Write operations require authentication
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand { EventDto = dto };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // GET: api/v1/event
    [HttpGet]
    [AllowAnonymous]  // CRITICAL: Read operations are public
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
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id });
        
        if (@event == null)
        {
            return NotFound(new { error = "Event not found" });
        }

        return Ok(@event);
    }

    // GET: api/v1/event/my
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<List<EventListDto>>> GetMyEvents()
    {
        // CRITICAL: UserId extraction with fallback pattern
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var events = await _mediator.Send(new GetMyEventsRequest { UserId = userId });
        return Ok(events);
    }
}
```

---

## 2. Get Event List - Full Query Flow

### List DTO (Optimized for Display)

**File**: `Explore.Application/DTOs/Event/EventListDto.cs`

```csharp
using System;

namespace Explore.Application.DTOs.Event;

public class EventListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Event Type
    public int EventTypeId { get; set; }
    public string EventTypeFullName { get; set; }

    // Audience
    public int AudienceGenderId { get; set; }
    public string AudienceGenderFullName { get; set; }
    public int AudienceAgeId { get; set; }
    public string AudienceAgeFullName { get; set; }
    public int? AudienceAgeMinAge { get; set; }
    public int? AudienceAgeMaxAge { get; set; }

    // Actor (Owner)
    public Guid ActorId { get; set; }
    public string ActorDisplayName { get; set; }
    public int ActorTypeId { get; set; }
    public string ActorTypeFullName { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image
    public Guid FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    // Registration
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }

    // Status & Visibility
    public int EventStatusId { get; set; }
    public string EventStatusFullName { get; set; }
    public int VisibilityTypeId { get; set; }
    public string VisibilityTypeFullName { get; set; }

    // Format
    public int EventFormatId { get; set; }
    public string EventFormatFullName { get; set; }

    // Islamic Context
    public int? MadhabId { get; set; }
    public string? MadhabFullName { get; set; }

    // Session Info
    public int? SessionCount { get; set; }
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    // Metadata
    public int TotalViews { get; set; }
    public string? EventUrl { get; set; }

    // Tenant
    public Guid TenantId { get; set; }
}
```

### Query Request

**File**: `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`

```csharp
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public class GetEventListRequest : IRequest<List<EventListDto>>
{
    // No parameters - returns all events
}
```

### Query Handler (Shows Repository → DTO Mapping)

**File**: `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries;

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
        // CRITICAL: Repository returns ENTITIES (not DTOs)
        var events = await _eventRepository.GetEventsWithDetails();

        // CRITICAL: AutoMapper maps entities to DTOs
        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

### Repository Interface

**File**: `Explore.Application/Contracts/Persistence/IEventRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<Event?> GetEventWithDetails(Guid id);
    Task<List<Event>> GetEventsWithDetails();
    Task<List<Event>> GetMyEventsWithDetails(string userId);
}
```

### Repository Implementation (Complex Include Chains)

**File**: `Explore.Persistence/Repositories/EventRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Event>> GetEventsWithDetails()
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithDetails(Guid id)
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a.Organization)
                    .ThenInclude(o => o.Members)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .Where(e => e.Actor.Organization != null && 
                        e.Actor.Organization.Members.Any(m => m.UserId.ToString() == userId))
            .ToListAsync();
    }
}
```

### AutoMapper Profile

**File**: `Explore.Application/Profiles/MappingProfile.cs` (excerpt)

```csharp
using AutoMapper;
using Explore.Application.DTOs.Event;
using Explore.Domain;

namespace Explore.Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Event mappings
        CreateMap<Event, EventDto>()
            .ReverseMap();

        CreateMap<Event, EventListDto>();

        CreateMap<CreateEventDto, Event>();

        CreateMap<UpdateEventDto, Event>();
        
        // ... other entity mappings
    }
}
```

---

## Key Patterns Demonstrated

### 1. **Manual Validator Instantiation** ⚠️
```csharp
// CRITICAL: Validators are instantiated with dependencies in handlers
var validator = new CreateEventDtoValidator(
    _audienceAgeRepository, 
    _audienceGenderRepository, 
    _eventTypeRepository, 
    _actorRepository, 
    _storageObjectRepository);
```

### 2. **Repository Returns Entities, Not DTOs** ⚠️
```csharp
// Repository returns entities
var events = await _eventRepository.GetEventsWithDetails();

// Handler maps to DTOs using AutoMapper
return _mapper.Map<List<EventListDto>>(events);
```

### 3. **BaseCommandResponse<Guid> for Commands** ⚠️
```csharp
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}
```

### 4. **Authorization Pattern** ⚠️
```csharp
[HttpGet]
[AllowAnonymous]  // Public read access

[HttpPost]
[Authorize]  // Authenticated write access
```

### 5. **UserId Extraction with Fallback** ⚠️
```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
```

### 6. **Complex EF Core Include Chains** ⚠️
```csharp
return await _dbContext.Events
    .Include(e => e.Actor)
        .ThenInclude(a => a.ActorType)
    .Include(e => e.Actor)
        .ThenInclude(a => a.Organization)
            .ThenInclude(o => o.Members)
    .Include(e => e.FeaturedImage)
    .ToListAsync();
```

---

This complete example shows **every layer** of the CQRS cycle in the ISLAMU Event codebase, from domain entity to API controller, with all critical patterns highlighted.
