# Command Patterns - ISLAMU Event Conventions

## Key Pattern: BaseCommandResponse + DTO + Manual Validation

Commands in ISLAMU Event use a specific pattern different from standard MediatR CQRS.

## Structure

```
Features/Events/
├── Requests/Commands/
│   ├── CreateEventCommand.cs        # Contains DTO property
│   ├── UpdateEventCommand.cs
│   └── DeleteEventCommand.cs
└── Handlers/Commands/
    ├── CreateEventCommandHandler.cs  # Manual validation inside
    ├── UpdateEventCommandHandler.cs
    └── DeleteEventCommandHandler.cs

DTOs/Event/
├── CreateEventDto.cs
├── UpdateEventDto.cs
└── Validators/
    ├── CreateEventDtoValidator.cs   # FluentValidation validators
    └── UpdateEventDtoValidator.cs
```

## Create Command Pattern

### 1. Command
```csharp
// File: Features/Events/Requests/Commands/CreateEventCommand.cs
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands
{
    public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventDto EventDto { get; set; }
    }
}
```

### 2. DTO
```csharp
// File: DTOs/Event/CreateEventDto.cs
namespace Explore.Application.DTOs.Event
{
    public class CreateEventDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid OrganizationId { get; set; }
        public int AudienceAgeId { get; set; }
        public int AudienceGenderId { get; set; }
        public int EventTypeId { get; set; }
    }
}
```

### 3. Validator
```csharp
// File: DTOs/Event/Validators/CreateEventDtoValidator.cs
using FluentValidation;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.Event.Validators
{
    public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
    {
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IOrganizationRepository _organizationRepository;

        public CreateEventDtoValidator(
            IAudienceAgeRepository audienceAgeRepository,
            IOrganizationRepository organizationRepository)
        {
            _audienceAgeRepository = audienceAgeRepository;
            _organizationRepository = organizationRepository;

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.Now).WithMessage("Event must start in the future");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

            RuleFor(x => x.OrganizationId)
                .MustAsync(async (id, token) =>
                {
                    return await _organizationRepository.Exists(id);
                }).WithMessage("Organization does not exist");

            RuleFor(x => x.AudienceAgeId)
                .MustAsync(async (id, token) =>
                {
                    return await _audienceAgeRepository.Exists(id);
                }).WithMessage("Audience age does not exist");
        }
    }
}
```

### 4. Handler
```csharp
// File: Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
using System.Linq;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IOrganizationRepository organizationRepository,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            // Manual validation
            var validator = new CreateEventDtoValidator(
                _audienceAgeRepository,
                _audienceGenderRepository,
                _eventTypeRepository,
                _organizationRepository);

            var validationResult = await validator.ValidateAsync(request.EventDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            // Map DTO to entity
            var @event = _mapper.Map<Event>(request.EventDto);
            @event.TotalViews = 0;

            // Save
            @event = await _eventRepository.Create(@event);

            // Success response
            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event created successfully.";

            return response;
        }
    }
}
```

## BaseCommandResponse Structure

```csharp
// File: Responses/BaseCommandResponse.cs
namespace Explore.Application.Responses
{
    public class BaseCommandResponse<T>
    {
        public T Id { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public List<string> Errors { get; set; }
    }
}
```

## Update Command Pattern

```csharp
// Command
public class UpdateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateEventDto EventDto { get; set; }
}

// DTO
public class UpdateEventDto
{
    public Guid Id { get; set; }  // Required for updates
    public string Title { get; set; }
    public string Description { get; set; }
    // ... other properties
}

// Handler
public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // Validate
    var validator = new UpdateEventDtoValidator(_repositories...);
    var validationResult = await validator.ValidateAsync(request.EventDto);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Update failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // Get existing
    var @event = await _eventRepository.GetById(request.EventDto.Id);

    if (@event == null)
    {
        response.Success = false;
        response.Message = "Event not found.";
        return response;
    }

    // Update
    _mapper.Map(request.EventDto, @event);
    await _eventRepository.Update(@event);

    response.Success = true;
    response.Id = @event.Id;
    response.Message = "Event updated successfully.";

    return response;
}
```

## Delete Command Pattern

```csharp
// Command
public class DeleteEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }
}

// Handler
public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    var @event = await _eventRepository.GetById(request.Id);

    if (@event == null)
    {
        response.Success = false;
        response.Message = "Event not found.";
        return response;
    }

    await _eventRepository.Delete(@event);

    response.Success = true;
    response.Id = request.Id;
    response.Message = "Event deleted successfully.";

    return response;
}
```

## Controller Usage

```csharp
// API Controller
[HttpPost]
public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateEvent([FromBody] CreateEventDto dto)
{
    var command = new CreateEventCommand { EventDto = dto };
    var result = await _mediator.Send(command);

    if (!result.Success)
        return BadRequest(result);

    return CreatedAtAction(nameof(GetEvent), new { id = result.Id }, result);
}

[HttpPut("{id}")]
public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateEvent(Guid id, [FromBody] UpdateEventDto dto)
{
    dto.Id = id;  // Ensure ID matches route
    var command = new UpdateEventCommand { EventDto = dto };
    var result = await _mediator.Send(command);

    if (!result.Success)
        return BadRequest(result);

    return Ok(result);
}

[HttpDelete("{id}")]
public async Task<ActionResult<BaseCommandResponse<Guid>>> DeleteEvent(Guid id)
{
    var command = new DeleteEventCommand { Id = id };
    var result = await _mediator.Send(command);

    if (!result.Success)
        return NotFound(result);

    return NoContent();
}
```

## Key Differences from Standard CQRS

| Aspect | Standard MediatR CQRS | ISLAMU Event Pattern |
|--------|----------------------|---------------------|
| **Command properties** | Direct on command | Wrapped in DTO |
| **Response** | Direct type (Guid) | BaseCommandResponse<Guid> |
| **Validation** | Pipeline behavior | Manual in handler |
| **Success/Failure** | Exceptions | Success flag + errors list |
| **Validator location** | Application/Behaviors | DTOs/Validators |
| **Validator creation** | DI injection | Manual instantiation |

## Benefits of This Pattern

✅ **Explicit success/failure**: No exceptions for business validation
✅ **Reusable DTOs**: Same DTO for API and commands
✅ **Consistent responses**: All commands return same structure
✅ **Flexible validation**: Validators can have dependencies

---

**Next**: See [query-patterns.md](query-patterns.md) and [handler-patterns.md](handler-patterns.md)
