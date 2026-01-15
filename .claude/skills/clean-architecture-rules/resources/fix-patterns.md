# Fix Patterns - How to Resolve Violations

## Pattern #1: Move Logic to Correct Layer

### Scenario: Business Logic in Controller

**❌ Before** (Logic in wrong layer):
```csharp
// File: Explore.API/Controllers/EventController.cs
[HttpPost]
public async Task<ActionResult> Create(CreateEventDto dto)
{
    // ❌ Business logic in controller
    if (dto.Title == null || dto.Title.Length > 500)
        return BadRequest("Title must be between 1 and 500 characters");

    if (dto.EventTypeId <= 0)
        return BadRequest("Event type is required");

    var @event = new Event
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        EventTypeId = dto.EventTypeId,
        TotalViews = 0
    };

    _dbContext.Events.Add(@event);
    await _dbContext.SaveChangesAsync();

    return Ok(@event.Id);
}
```

**✅ After** (Logic in correct layers):
```csharp
// Step 1: Create Command in Application layer
// File: Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Requests.Commands;

using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
}

// Step 2: Create Validator in Application layer
// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
namespace Explore.Application.DTOs.Event.Validators;

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

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event type is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventTypeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Event type not found");

        RuleFor(x => x.AudienceGenderId)
            .NotEmpty().WithMessage("Audience gender is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceGenderRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience gender not found");

        RuleFor(x => x.AudienceAgeId)
            .NotEmpty().WithMessage("Audience age is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceAgeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience age not found");

        RuleFor(x => x.OrganizationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _organizationRepository.Exists(id.Value);
            })
            .When(x => x.OrganizationId.HasValue)
            .WithMessage("Organization does not exist.");

        RuleFor(x => x.FeaturedImageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _storageObjectRepository.Exists(id.Value);
            })
            .When(x => x.FeaturedImageId.HasValue)
            .WithMessage("FeaturedImageId does not exist.");
    }
}

// Step 3: Create Handler in Application layer
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
namespace Explore.Application.Features.Events.Handlers.Commands;

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

        // ✅ Validate using FluentValidation - instantiated manually with dependencies
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

        // ✅ Map DTO to Entity
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;

        // ✅ Save through repository
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;
    }
}

// Step 4: Thin Controller in API layer
// File: Explore.API/Controllers/EventController.cs
namespace Explore.API.Controllers;

using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
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

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand { EventDto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
```

**Benefits**:
- ✅ Business logic is testable without HTTP
- ✅ Validation runs automatically via FluentValidation
- ✅ Controller is thin (5 lines)
- ✅ Logic can be reused from Blazor, CLI, etc.

---

## Pattern #2: Use Interfaces for Infrastructure Dependencies

### Scenario: Application Needs Email Sending

**❌ Before** (Direct dependency):
```csharp
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
using Explore.Infrastructure.Email;  // ❌ VIOLATION!
using SendGrid;  // ❌ Infrastructure concern

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly SendGridEmailService _emailService;  // ❌ Concrete class

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // ... create event

        await _emailService.SendAsync(email);  // ❌ Tightly coupled

        return response;
    }
}
```

**✅ After** (Dependency Inversion):
```csharp
// Step 1: Define interface in Application layer
// File: Explore.Application/Contracts/Infrastructure/IEmailService.cs
namespace Explore.Application.Contracts.Infrastructure;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEventCreatedNotificationAsync(
        Guid eventId,
        string eventTitle,
        string organizerEmail,
        CancellationToken cancellationToken = default);
}

// Step 2: Use interface in Application layer
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
namespace Explore.Application.Features.Events.Handlers.Commands;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;  // ✅ Same layer interface
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;  // ✅ Interface
    private readonly IMapper _mapper;
    // ... other dependencies

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validation...
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;
        @event = await _eventRepository.Create(@event);

        // ✅ Calls interface, doesn't know about SendGrid
        await _emailService.SendEventCreatedNotificationAsync(
            @event.Id,
            @event.Title,
            request.EventDto.OrganizerEmail,
            cancellationToken);

        return response;
    }
}

// Step 3: Implement in Infrastructure layer
// File: Explore.Infrastructure/Email/SendGridEmailService.cs
namespace Explore.Infrastructure.Email;

using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;  // ✅ Implements interface
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(ISendGridClient client, ILogger<SendGridEmailService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendEventCreatedNotificationAsync(
        Guid eventId,
        string eventTitle,
        string organizerEmail,
        CancellationToken cancellationToken = default)
    {
        var from = new EmailAddress("noreply@islamu.org", "ISLAMU Event");
        var to = new EmailAddress(organizerEmail);
        var subject = $"Event Created: {eventTitle}";
        var htmlContent = $"<p>Your event <strong>{eventTitle}</strong> has been created successfully.</p>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

        try
        {
            var response = await _client.SendEmailAsync(msg, cancellationToken);
            _logger.LogInformation("Email sent to {Email} for event {EventId}", organizerEmail, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for event {EventId}", eventId);
            throw;
        }
    }
}

// Step 4: Register in API (Composition Root)
// File: Explore.API/Program.cs
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
```

**Benefits**:
- ✅ Application doesn't know about SendGrid
- ✅ Can easily swap SendGrid for Mailgun, SMTP, or mock for testing
- ✅ No breaking changes to Application when Infrastructure changes

---

## Pattern #3: Repository Pattern for Data Access

### Scenario: Application Needs Database Queries

**❌ Before** (Direct DbContext access):
```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
using Explore.Persistence;  // ❌ VIOLATION!
using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly ExploreDbContext _context;  // ❌ Concrete class

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        return await _context.Events  // ❌ Direct DbSet access
            .Include(e => e.EventType)
            .Where(e => e.EventStatusId == 2)
            .ToListAsync(cancellationToken);
    }
}
```

**✅ After** (Repository pattern):
```csharp
// Step 1: Define repository interface in Application layer
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

// Step 2: Use interface in Application layer
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;  // ✅ Same layer
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;  // ✅ Abstraction
    private readonly IMapper _mapper;

    public GetEventListRequestHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetEventsWithDetails();  // ✅ Returns List<Event>
        return _mapper.Map<List<EventListDto>>(events);  // ✅ Maps to DTOs
    }
}

// Step 3: Implement repository in Persistence layer
// File: Explore.Persistence/Repositories/EventRepository.cs
namespace Explore.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;  // ✅ Implements interface
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
        Guid userGuid;
        var isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userGuid && u.ActorId == e.ActorId) ||
                _dbContext.OrganizationMembers.Any(om =>
                    om.UserId == userGuid &&
                    _dbContext.Organizations.Any(o => o.Id == om.OrganizationId && o.ActorId == e.ActorId)));
        }

        return await query.ToListAsync();
    }
}

// Step 4: Register in API (Composition Root)
// File: Explore.API/Program.cs
builder.Services.AddScoped<IEventRepository, EventRepository>();
```

**Benefits**:
- ✅ Application doesn't know about EF Core
- ✅ Can mock repository for unit tests
- ✅ Complex queries are encapsulated in Persistence
- ✅ Can optimize queries without changing Application

---

## Pattern #4: Domain Invariants vs Application Validation

### Scenario: Ensuring Data Integrity

**Concept**:
- **Domain Invariants**: Rules that must ALWAYS be true (enforced in Domain)
- **Application Validation**: Input validation that can vary by use case (enforced in Application)

**❌ Before** (Validation in wrong place):
```csharp
// File: Explore.Domain/Event.cs
using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class Event
{
    [Required]  // ❌ Presentation concern
    [MaxLength(500)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Arbitrary validation rule
    public int? MaxAudienceAttendees { get; set; }
}
```

**✅ After** (Proper separation):
```csharp
// File: Explore.Domain/Event.cs
namespace Explore.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    
    [ForeignKey("EventType")]
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; }

    [ForeignKey("AudienceGender")]
    public int AudienceGenderId { get; set; }
    public AudienceGender AudienceGender { get; set; }
    
    // ✅ No validation annotations - domain is pure
}

// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
namespace Explore.Application.DTOs.Event.Validators;

using FluentValidation;
using Explore.Application.Contracts.Persistence;

// ✅ INPUT VALIDATION: Can vary by use case
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;

    public CreateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event type is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventTypeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Event type not found");
    }
}

// File: Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs
// ✅ UPDATE validation can be different (e.g., allow partial updates)
public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    public UpdateEventDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        // Maybe title is optional when updating
        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must not exceed 200 characters");

        // Description optional
        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");
    }
}
```

**Decision Matrix**:

| Question | Domain Invariant | Application Validation |
|----------|------------------|----------------------|
| Can it vary by use case? | ❌ No | ✅ Yes |
| Can entity exist without it? | ❌ No | ✅ Yes |
| Is it a business rule? | ✅ Yes | ⚠️ Maybe |
| Example | "Event must have actor" | "Title required for CREATE, optional for UPDATE" |

---

## Pattern #5: Sharing Code Between Layers (DTOs)

### Scenario: Sharing DTOs Between API and Blazor

**❌ Before** (DTOs in wrong layer):
```csharp
// File: Explore.API/Models/EventDto.cs
namespace Explore.API.Models;  // ❌ API-specific

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// File: Explore.Blazor.Client/Models/EventDto.cs
namespace Explore.Blazor.Client.Models;  // ❌ Duplicated!

public class EventDto  // ❌ Same DTO copied
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

**✅ After** (DTOs in Application layer):
```csharp
// File: Explore.Application/DTOs/Event/EventListDto.cs
namespace Explore.Application.DTOs.Event;

using System;

// ✅ Shared DTO in Application layer
public class EventListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public string AudienceGenderName { get; set; } = string.Empty;
    public string AudienceAgeName { get; set; } = string.Empty;
}

// File: Explore.API/Controllers/EventController.cs
namespace Explore.API.Controllers;

using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.Event;  // ✅ References Application DTOs
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventListDto>>> GetAll()
    {
        var events = await _mediator.Send(new GetEventListRequest());
        return Ok(events);
    }
}

// File: Explore.Blazor.Client/Pages/EventsList.razor
@using Explore.Application.DTOs.Event  @* ✅ References same DTOs *@
@inject HttpClient Http

@code {
    private List<EventListDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events = await Http.GetFromJsonAsync<List<EventListDto>>("/api/v1/event");
    }
}
```

**Benefits**:
- ✅ Single source of truth for DTOs
- ✅ No duplication between API and Blazor
- ✅ Changes to DTOs are reflected everywhere
- ✅ Can be shared across multiple UIs (Blazor, CLI, etc.)

---

## Pattern #6: Composition Root (DI Registration)

### Scenario: Wiring Up All Dependencies

**Location**: Always in **API or Blazor Program.cs** (Presentation layer)

**Real Example from Explore.API/Program.cs**:
```csharp
// File: Explore.API/Program.cs
using Explore.Application;
using Explore.Infrastructure;
using Explore.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Application layer services
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly));
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ✅ Register Persistence layer services (DbContext, Repositories)
builder.Services.AddDbContext<ExploreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IActorRepository, ActorRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

// ✅ Register Infrastructure layer services
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();

var app = builder.Build();
app.Run();
```

**Benefits**:
- ✅ All dependencies registered in one place
- ✅ Clear composition root
- ✅ Each layer provides services
- ✅ Testable with mock implementations

---

## Quick Reference: Fix Decision Tree

```
Violation detected. What should I do?

1. Is it a business rule or domain concept?
   YES → Move to Domain entity method
   NO → Continue

2. Is it a use case/workflow?
   YES → Create Command/Query in Application
   NO → Continue

3. Is it database access?
   YES → Create interface in Application, implement in Persistence
   NO → Continue

4. Is it external service (email, file storage)?
   YES → Create interface in Application, implement in Infrastructure
   NO → Continue

5. Is it HTTP-specific (status codes, headers)?
   YES → Keep in API Controller
   NO → Continue

6. Is it UI-specific (rendering, user interaction)?
   YES → Keep in Blazor component
   NO → Re-evaluate (might be in wrong layer)
```

---

**Summary**: When in doubt, dependencies flow INWARD. High-level policy (Domain, Application) does not depend on low-level details (Infrastructure, Persistence, API).