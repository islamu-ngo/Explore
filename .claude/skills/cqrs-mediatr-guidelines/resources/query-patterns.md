# Query Patterns - ISLAMU Event Conventions

## ISLAMU Event Query Conventions

Queries in this project follow a specific pattern with `Request` suffix (not `Query`) and return DTOs directly.

## File Organization

```
Explore.Application/Features/Events/
├── Requests/Queries/
│   ├── GetEventListRequest.cs
│   ├── GetEventDetailsRequest.cs
│   └── GetMyEventsRequest.cs
└── Handlers/Queries/
    ├── GetEventListRequestHandler.cs
    ├── GetEventDetailsRequestHandler.cs
    └── GetMyEventsRequestHandler.cs

Explore.Application/DTOs/Event/
├── EventListDto.cs          # For list views
├── EventDto.cs              # For detail views
└── (other DTOs)
```

## List Query Pattern

### 1. Query Request

**Real Example from Event**:
```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs
namespace Explore.Application.Features.Events.Requests.Queries;

using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

public class GetEventListRequest : IRequest<List<EventListDto>>
{
}
```

**Key Conventions**:
- ✅ Class name ends with `Request` (not `Query`)
- ✅ Implements `IRequest<List<TDto>>`
- ✅ Returns `List<EventListDto>` directly (no wrapper)
- ✅ Can include filter properties if needed

### 2. Query Handler

**Real Example from Event**:
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

**Key Patterns**:
- ✅ Uses `IEventRepository`, not DbContext
- ✅ Repository method returns `List<Event>` (entities)
- ✅ AutoMapper maps entities to DTOs
- ✅ Returns DTOs directly, no `BaseCommandResponse` wrapper

### 3. List DTO

**Real Example from Event**:
```csharp
// File: Explore.Application/DTOs/Event/EventListDto.cs
namespace Explore.Application.DTOs.Event;

using System;

public class EventListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public string AudienceGenderName { get; set; } = string.Empty;
    public string AudienceAgeName { get; set; } = string.Empty;
}
```

**Conventions**:
- ✅ Minimal properties for list views
- ✅ Include navigation property names (e.g., `EventTypeName`, not just `EventTypeId`)
- ✅ Suffix with `ListDto`

## Details Query Pattern

### 1. Query Request

**Real Example from Event**:
```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetEventDetailsRequest.cs
namespace Explore.Application.Features.Events.Requests.Queries;

using System;
using Explore.Application.DTOs.Event;
using MediatR;

public class GetEventDetailsRequest : IRequest<EventDto>
{
    public Guid Id { get; set; }
}
```

### 2. Query Handler

**Real Example from Event**:
```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventDetailsRequestHandler.cs
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetEventDetailsRequestHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
    {
        // ✅ Repository returns Event? (nullable entity)
        var @event = await _eventRepository.GetEventWithDetails(request.Id);

        // ✅ Return null if not found (controller handles NotFound)
        if (@event == null)
            return null;

        // ✅ Map entity → DTO
        return _mapper.Map<EventDto>(@event);
    }
}
```

### 3. Details DTO

**Real Example from Event**:
```csharp
// File: Explore.Application/DTOs/Event/EventDto.cs
namespace Explore.Application.DTOs.Event;

using System;
using Explore.Application.DTOs.Common;

public class EventDto : IEventDto
{
    public Guid Id { get; set; }
    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AudienceGenderId { get; set; }
    public string AudienceGenderName { get; set; } = string.Empty;
    public int AudienceAgeId { get; set; }
    public string AudienceAgeName { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string ActorDisplayName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid FeaturedImageId { get; set; }
    public string FeaturedImageUri { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    public bool IsRegistrationRequired { get; set; }
    public string EventUrl { get; set; } = string.Empty;
    public int? MadhabId { get; set; }
    public string MadhabName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int VisibilityTypeId { get; set; }
    public string VisibilityTypeName { get; set; } = string.Empty;
    public int? SessionCount { get; set; }
    public int EventStatusId { get; set; }
    public string EventStatusName { get; set; } = string.Empty;
    public string ExternalRegistrationUrl { get; set; } = string.Empty;
    public DateTime? FirstSessionDate { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public int EventFormatId { get; set; }
    public string EventFormatName { get; set; } = string.Empty;
    public Guid? AtprotoRecordId { get; set; }
}
```

**Conventions**:
- ✅ All entity properties
- ✅ Include both IDs and Names for foreign keys
- ✅ Suffix with `Dto` (not `DetailsDto`)

## User-Specific Query Pattern

**Real Example from Event**:
```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetMyEventsRequest.cs
namespace Explore.Application.Features.Events.Requests.Queries;

using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

public class GetMyEventsRequest : IRequest<List<EventListDto>>
{
    public string UserId { get; set; } = string.Empty;
}

// File: Explore.Application/Features/Events/Handlers/Queries/GetMyEventsRequestHandler.cs
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

public class GetMyEventsRequestHandler : IRequestHandler<GetMyEventsRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetMyEventsRequestHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<List<EventListDto>> Handle(GetMyEventsRequest request, CancellationToken cancellationToken)
    {
        // ✅ Repository handles complex filtering
        var events = await _eventRepository.GetMyEventsWithDetails(request.UserId);

        // ✅ Map to DTOs
        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

## Repository Pattern for Queries

**Real Example from EventRepository**:
```csharp
// File: Explore.Application/Contracts/Persistence/IEventRepository.cs
namespace Explore.Application.Contracts.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Domain;

public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<Event?> GetEventWithDetails(Guid id);
    Task<List<Event>> GetEventsWithDetails();
    Task<List<Event>> GetMyEventsWithDetails(string userId);
}

// File: Explore.Persistence/Repositories/EventRepository.cs
namespace Explore.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
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

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
                .ThenInclude(at => at.Organization)
                .ThenInclude(o => o.Members.Where(m => m.UserId.ToString() == userId))
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .Where(e => e.Actor.Organization.Members.Any(m => m.UserId.ToString() == userId))
            .ToListAsync();
    }
}
```

**Critical Patterns**:
- ✅ Repository returns `List<Event>` (entities), not DTOs
- ✅ Use `.Include()` and `.ThenInclude()` for eager loading
- ✅ Complex filtering happens in repository
- ✅ Handler maps entities → DTOs

## AutoMapper Configuration

**Real Example from MappingProfile**:
```csharp
// File: Explore.Application/Profiles/MappingProfile.cs
namespace Explore.Application.Profiles;

using AutoMapper;
using Explore.Application.DTOs.Event;
using Explore.Domain;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Event → EventListDto (AutoMapper infers navigation properties)
        CreateMap<Event, EventListDto>();

        // Event → EventDto (AutoMapper infers navigation properties)
        CreateMap<Event, EventDto>();

        // CreateEventDto → Event
        CreateMap<CreateEventDto, Event>();

        // UpdateEventDto → Event
        CreateMap<UpdateEventDto, Event>();
    }
}
```

**AutoMapper Magic**:
- ✅ `EventTypeName` automatically mapped from `Event.EventType.FullName`
- ✅ `AudienceGenderName` automatically mapped from `Event.AudienceGender.FullName`
- ✅ No manual `.ForMember()` needed for matching names

## Naming Conventions

| Pattern | Example | Returns |
|---------|---------|---------|
| Get{Entity}ListRequest | GetEventListRequest | `List<EventListDto>` |
| Get{Entity}DetailsRequest | GetEventDetailsRequest | `EventDto` |
| GetMy{Entities}Request | GetMyEventsRequest | `List<EventListDto>` |
| Check{Condition}Query | CheckUserExistsQuery | `bool` |

**Note**: Most queries use `Request` suffix. Some newer ones use `Query` suffix (both patterns exist).

## Controller Usage

**Real Example from EventController**:
```csharp
// File: Explore.API/Controllers/EventController.cs
namespace Explore.API.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EventController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    // ✅ Public read access
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
    {
        var events = await _mediator.Send(new GetEventListRequest());
        return Ok(events);
    }

    // ✅ Public read access for single item
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id });

        if (@event == null)
            return NotFound();

        return Ok(@event);
    }

    // ✅ User-specific query (authenticated)
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<List<EventListDto>>> GetMyEvents()
    {
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

**Key Points**:
- ✅ GET endpoints use `[AllowAnonymous]` for public read
- ✅ Return `null` for not found, handle in controller with `NotFound()`
- ✅ Simple `return Ok(result)` - no wrapper needed
- ✅ Extract userId from JWT claims for user-specific queries

---

**Next**: See [command-patterns.md](command-patterns.md) for write operations and [handler-patterns.md](handler-patterns.md) for common handler patterns.
