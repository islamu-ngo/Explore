# Query Patterns

## ISLAMU Event Query Conventions

Queries in this project follow a specific pattern with `Request` suffix and return DTOs directly.

##Structure

### File Organization
```
Explore.Application/
└── Features/
    └── Events/
        ├── Requests/
        │   └── Queries/
        │       ├── GetEventListRequest.cs
        │       ├── GetEventDetailsRequest.cs
        │       └── GetMyEventsRequest.cs
        └── Handlers/
            └── Queries/
                ├── GetEventListRequestHandler.cs
                ├── GetEventDetailsRequestHandler.cs
                └── GetMyEventsRequestHandler.cs
```

### Query Request Pattern

```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries
{
    public class GetEventListRequest : IRequest<List<EventListDto>>
    {
        public Guid? OrganizationId { get; set; }
        public int? StatusTypeId { get; set; }
        public string SearchTerm { get; set; }
    }
}
```

**Key Conventions**:
- ✅ Class name ends with `Request` (not `Query`)
- ✅ Implements `IRequest<TResponse>` where TResponse is DTO type
- ✅ Returns DTOs directly (no wrapper)
- ✅ Use `List<TDto>` for lists
- ✅ Use `TDto` for single item (can be nullable)

### Query Handler Pattern

```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

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
            // Get entities from repository
            var events = await _eventRepository.GetAll();

            // Filter if needed
            if (request.OrganizationId.HasValue)
                events = events.Where(e => e.OrganizationId == request.OrganizationId.Value).ToList();

            if (!string.IsNullOrEmpty(request.SearchTerm))
                events = events.Where(e => e.Title.Contains(request.SearchTerm)).ToList();

            // Map to DTOs
            return _mapper.Map<List<EventListDto>>(events);
        }
    }
}
```

**Key Conventions**:
- ✅ Handler name ends with `RequestHandler`
- ✅ Uses repositories (not DbContext directly)
- ✅ Uses AutoMapper for entity → DTO mapping
- ✅ Returns DTOs directly (no `BaseCommandResponse`)
- ✅ Filtering happens in handler (not in repository)

## Single Item Query

```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetEventDetailsRequest.cs
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries
{
    public class GetEventDetailsRequest : IRequest<EventSpecificDto>
    {
        public Guid Id { get; set; }
    }
}

// File: Explore.Application/Features/Events/Handlers/Queries/GetEventDetailsRequestHandler.cs
public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventSpecificDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<EventSpecificDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetById(request.Id);

        if (@event == null)
            return null;  // Or throw NotFoundException

        return _mapper.Map<EventSpecificDto>(@event);
    }
}
```

## User-Specific Query

```csharp
// File: Explore.Application/Features/Events/Requests/Queries/GetMyEventsRequest.cs
public class GetMyEventsRequest : IRequest<List<EventListDto>>
{
    public Guid UserId { get; set; }
}

// Handler
public class GetMyEventsRequestHandler : IRequestHandler<GetMyEventsRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<List<EventListDto>> Handle(GetMyEventsRequest request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetEventsByUserId(request.UserId);
        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

## DTO Types

### List DTO
```csharp
// File: Explore.Application/DTOs/Event/EventListDto.cs
namespace Explore.Application.DTOs.Event
{
    public class EventListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public string OrganizationName { get; set; }
        public int TotalViews { get; set; }
    }
}
```

### Details DTO
```csharp
// File: Explore.Application/DTOs/Event/EventSpecificDto.cs
namespace Explore.Application.DTOs.Event
{
    public class EventSpecificDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalViews { get; set; }

        // Navigation properties mapped to DTOs
        public OrganizationDto Organization { get; set; }
        public List<ProgramDto> Programs { get; set; }
    }
}
```

## Repository Pattern

Queries use repositories defined in `Contracts/Persistence`:

```csharp
// File: Explore.Application/Contracts/Persistence/IEventRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventRepository : IGenericRepository<Event>
    {
        Task<List<Event>> GetEventsByUserId(Guid userId);
        Task<List<Event>> GetEventsByOrganizationId(Guid organizationId);
        Task<Event> GetEventWithDetails(Guid id);
    }
}
```

## Naming Conventions

| Pattern | Example | Returns |
|---------|---------|---------|
| Get{Entity}ListRequest | GetEventListRequest | `List<EventListDto>` |
| Get{Entity}DetailsRequest | GetEventDetailsRequest | `EventSpecificDto` |
| GetMy{Entities}Request | GetMyEventsRequest | `List<EventListDto>` |
| Check{Condition}Request | CheckUserExistsQuery | `bool` |

**Note**: Some newer queries use `Query` suffix (e.g., `CheckUserExistsQuery`). Both patterns exist in the codebase.

## Controller Usage

```csharp
// File: Explore.API/Controllers/EventsController.cs
[HttpGet]
public async Task<ActionResult<List<EventListDto>>> GetEvents([FromQuery] Guid? organizationId)
{
    var query = new GetEventListRequest { OrganizationId = organizationId };
    var result = await _mediator.Send(query);
    return Ok(result);
}

[HttpGet("{id}")]
public async Task<ActionResult<EventSpecificDto>> GetEvent(Guid id)
{
    var query = new GetEventDetailsRequest { Id = id };
    var result = await _mediator.Send(query);

    if (result == null)
        return NotFound();

    return Ok(result);
}
```

**Key Points**:
- ✅ No try-catch needed (queries don't throw for not found)
- ✅ Return null for not found, handle in controller
- ✅ Simple `return Ok(result)` - no wrapper

---

**Next**: See [handler-patterns.md](handler-patterns.md) for more details on handler implementation.
