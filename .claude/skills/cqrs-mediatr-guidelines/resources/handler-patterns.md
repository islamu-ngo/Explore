# Handler Patterns

## Repository Usage

Handlers use repositories (not DbContext directly):

```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    
    // Inject repositories, not DbContext
    public CreateEventCommandHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }
}
```

## AutoMapper Usage

```csharp
// Map DTO to Entity
var entity = _mapper.Map<Event>(request.EventDto);

// Map Entity to DTO
var dto = _mapper.Map<EventDto>(entity);

// Map collection
var dtos = _mapper.Map<List<EventListDto>>(entities);
```

## CancellationToken

Always include `CancellationToken` parameter:

```csharp
public async Task<BaseCommandResponse<Guid>> Handle(
    CreateEventCommand request,
    CancellationToken cancellationToken)  // ✅ Always include
{
    // Pass to async methods
    await _eventRepository.Create(@event);  // Repository handles it
}
```

---

See [command-patterns.md](command-patterns.md) and [query-patterns.md](query-patterns.md) for complete examples.
