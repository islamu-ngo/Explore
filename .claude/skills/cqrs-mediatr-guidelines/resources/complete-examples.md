# Complete Feature Example

## Full Event Management Feature

### 1. Create Event

**Command**:
```csharp
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}
```

**DTO**:
```csharp
public class CreateEventDto
{
    public string Title { get; set; }
    public DateTime StartDate { get; set; }
    public Guid ActorId { get; set; }
}
```

**Validator**:
```csharp
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator(IActorRepository actorRepo)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ActorId).MustAsync(async (id, _) => await actorRepo.Exists(id));
    }
}
```

**Handler**:
```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventDtoValidator(_actorRepository);
        var validationResult = await validator.ValidateAsync(request.EventDto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var @event = _mapper.Map<Event>(request.EventDto);
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        return response;
    }
}
```

### 2. Get Event List

**Request**:
```csharp
public class GetEventListRequest : IRequest<List<EventListDto>>
{
    public Guid? ActorId { get; set; }
}
```

**Handler**:
```csharp
public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetAll();

        if (request.ActorId.HasValue)
            events = events.Where(e => e.ActorId == request.ActorId.Value).ToList();

        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

### 3. Controller

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var result = await _mediator.Send(new CreateEventCommand { EventDto = dto });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<EventListDto>>> GetAll([FromQuery] Guid? actorId)
    {
        var result = await _mediator.Send(new GetEventListRequest { ActorId = actorId });
        return Ok(result);
    }
}
```

---

This example shows the complete flow following ISLAMU Event conventions.
