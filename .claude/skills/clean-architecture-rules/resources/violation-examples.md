# Common Violations and Error Messages

## Violation #1: Domain Referencing Infrastructure

### ❌ The Problem

```csharp
// File: Explore.Domain/Event.cs
namespace Explore.Domain;

using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class Event
{
    [Key]  // ❌ Data annotation from EF Core
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Domain
File: Explore.Domain/Event.cs
Violation: Domain layer cannot reference Microsoft.EntityFrameworkCore

REASON:
Domain must be framework-agnostic. EF Core is an infrastructure concern.

FIX:
1. Remove [Key] attribute
2. Use [ForeignKey] for navigation properties only
3. Configure constraints in Persistence layer using Fluent API
```

### ✅ The Fix

```csharp
// File: Explore.Domain/Event.cs
namespace Explore.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;  // ✅ Only for [ForeignKey]

public class Event
{
    public Guid Id { get; set; }  // ✅ Plain C# property
    public string Title { get; set; } = string.Empty;
    
    // ✅ OK - Specifies relationship metadata
    [ForeignKey("EventType")]
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; }
    
    [ForeignKey("AudienceGender")]
    public int AudienceGenderId { get; set; }
    public AudienceGender AudienceGender { get; set; }
}
```

---

## Violation #2: Application Referencing DbContext Directly

### ❌ The Problem

```csharp
// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
namespace Explore.Application.Features.Events.Handlers.Queries;

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

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
Violation: Application layer cannot reference Explore.Persistence or Microsoft.EntityFrameworkCore

REASON:
Application should not depend on concrete database implementation.
This makes it impossible to:
- Unit test without a database
- Switch database providers
- Mock data for testing

FIX:
1. Create IEventRepository interface in Application layer
2. Use interface instead of concrete DbContext
3. Implement repository in Persistence layer
```

### ✅ The Fix

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
}

// File: Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;  // ✅ Interface in same layer
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
        var events = await _eventRepository.GetEventsWithDetails();  // ✅ Returns entities
        return _mapper.Map<List<EventListDto>>(events);  // ✅ Maps to DTOs
    }
}

// File: Explore.API/Program.cs (DI Registration)
builder.Services.AddScoped<IEventRepository, EventRepository>();
```

---

## Violation #3: Application Using ASP.NET Core Types

### ❌ The Problem

```csharp
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
namespace Explore.Application.Features.Events.Handlers.Commands;

using Microsoft.AspNetCore.Http;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;   // ❌ VIOLATION!

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, ActionResult<Guid>>
{
    public async Task<ActionResult<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // ... create event logic

        return new CreatedAtActionResult("GetById", "Event", new { id = eventId }, eventId);
        // ❌ Returning ASP.NET Core type
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
Violation: Application layer cannot reference Microsoft.AspNetCore.*

REASON:
Application should be framework-agnostic. It should work with:
- REST APIs
- gRPC services
- Console applications
- Background jobs
Returning ActionResult ties it to ASP.NET Core.

FIX:
1. Return BaseCommandResponse<Guid> (or plain Guid)
2. Let Controller map to ActionResult
```

### ✅ The Fix

```csharp
// File: Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Requests.Commands;

using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>  // ✅ Framework-agnostic
{
    public CreateEventDto EventDto { get; set; }
}

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
    private readonly IMapper _mapper;
    // ... other dependencies

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validation...
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;
        @event = await _eventRepository.Create(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event created successfully.";

        return response;  // ✅ Framework-agnostic response
    }
}

// File: Explore.API/Controllers/EventController.cs
namespace Explore.API.Controllers;

using System;
using System.Threading.Tasks;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;  // ✅ OK in API layer

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

        // ✅ Controller handles HTTP-specific concerns
        return Ok(response);
    }
}
```

---

## Violation #4: Domain Using Data Annotations for Validation

### ❌ The Problem

```csharp
// File: Explore.Domain/Event.cs
namespace Explore.Domain;

using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class Event
{
    public Guid Id { get; set; }

    [Required]  // ❌ Presentation concern
    [MaxLength(500)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Validation belongs in Application
    public int? MaxAudienceAttendees { get; set; }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Domain
File: Explore.Domain/Event.cs
Violation: Domain layer should not use System.ComponentModel.DataAnnotations for validation

REASON:
Data annotations mix concerns:
- [Required], [Range] = Validation (belongs in Application with FluentValidation)
- [MaxLength] = Database constraint (belongs in Persistence)

Validation rules can differ by use case:
- Creating an event might require Title
- Updating might allow partial updates
Domain should be pure business entities.

FIX:
1. Remove data annotations (except [ForeignKey])
2. Add FluentValidation in Application layer
```

### ✅ The Fix

```csharp
// File: Explore.Domain/Event.cs
namespace Explore.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;

// ✅ NO validation annotations
public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int TotalViews { get; set; }
    
    // ✅ Only [ForeignKey] is acceptable
    [ForeignKey("EventType")]
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; }

    [ForeignKey("AudienceGender")]
    public int AudienceGenderId { get; set; }
    public AudienceGender AudienceGender { get; set; }
}

// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
namespace Explore.Application.DTOs.Event.Validators;

using FluentValidation;
using Explore.Application.Contracts.Persistence;

// ✅ Application layer validates INPUT
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
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 500 characters");

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
    }
}
```

---

## Violation #5: Infrastructure Referencing API/Blazor

### ❌ The Problem

```csharp
// File: Explore.Infrastructure/Email/EmailService.cs
namespace Explore.Infrastructure.Email;

using Explore.API.Controllers;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;  // ❌ VIOLATION!

public class EmailService : IEmailService
{
    public async Task SendEventNotificationAsync(Guid eventId)
    {
        // ❌ Calling controller directly
        var controller = new EventController();
        var result = await controller.GetById(eventId);

        // ... send email
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Infrastructure
File: Explore.Infrastructure/Email/EmailService.cs
Violation: Infrastructure layer cannot reference Explore.API or presentation layers

REASON:
Infrastructure is below Presentation in the architecture.
Controllers should call Infrastructure, not the other way around.

FIX:
1. Pass event data as parameters to EmailService
2. Or use Application layer to orchestrate data retrieval
```

### ✅ The Fix

```csharp
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

// File: Explore.Infrastructure/Email/EmailService.cs
namespace Explore.Infrastructure.Email;

using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;  // ✅ Application interface only
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly ILogger<SendGridEmailService> _logger;

    public async Task SendEventCreatedNotificationAsync(
        Guid eventId,
        string eventTitle,
        string organizerEmail,
        CancellationToken cancellationToken = default)
    {
        // ✅ Uses passed data, doesn't fetch it itself
        var from = new EmailAddress("noreply@islamu.org", "ISLAMU Event");
        var to = new EmailAddress(organizerEmail);
        var subject = $"Event Created: {eventTitle}";
        var htmlContent = $"<p>Your event <strong>{eventTitle}</strong> has been created successfully.</p>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
        await _client.SendEmailAsync(msg, cancellationToken);
        
        _logger.LogInformation("Email sent to {Email} for event {EventId}", organizerEmail, eventId);
    }
}

// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = _mapper.Map<Event>(request.EventDto);
        @event.TotalViews = 0;
        @event = await _eventRepository.Create(@event);

        // ✅ Application orchestrates: gets data and calls Infrastructure
        await _emailService.SendEventCreatedNotificationAsync(
            @event.Id,
            @event.Title,
            request.EventDto.OrganizerEmail,
            cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = @event.Id,
            Message = "Event created successfully."
        };
    }
}
```

---

## Violation #6: Circular References Between Projects

### ❌ The Problem

```xml
<!-- File: Explore.Application/Explore.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
  <ProjectReference Include="..\Explore.Infrastructure\Explore.Infrastructure.csproj" />
  <!-- ❌ VIOLATION! -->
</ItemGroup>

<!-- File: Explore.Infrastructure/Explore.Infrastructure.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
  <!-- ❌ Creates circular reference! -->
</ItemGroup>
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Violation: Circular project reference detected
Path: Application → Infrastructure → Application

REASON:
Circular dependencies make the code impossible to build and test in isolation.

FIX:
1. Infrastructure should implement interfaces defined in Application
2. Remove Application → Infrastructure reference
3. Use dependency injection to wire up concrete implementations
```

### ✅ The Fix

```xml
<!-- File: Explore.Application/Explore.Application.csproj -->
<ItemGroup>
  <!-- ✅ ONLY reference Domain -->
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
</ItemGroup>

<!-- File: Explore.Infrastructure/Explore.Infrastructure.csproj -->
<ItemGroup>
  <!-- ✅ References Application and Domain -->
  <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
</ItemGroup>

<!-- File: Explore.API/Explore.API.csproj -->
<ItemGroup>
  <!-- ✅ API references all (Composition Root) -->
  <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
  <ProjectReference Include="..\Explore.Infrastructure\Explore.Infrastructure.csproj" />
  <ProjectReference Include="..\Explore.Persistence\Explore.Persistence.csproj" />
</ItemGroup>
```

---

## Quick Violation Detection Commands

### Search for Domain Violations
```bash
# Find prohibited using statements in Domain
rg "using (Microsoft\.(EntityFrameworkCore|AspNetCore)|Explore\.(Application|Infrastructure|API|Blazor))" Explore.Domain/

# Find validation annotations in Domain (except [ForeignKey])
rg "\[Required\]|\[MaxLength\]|\[Range\]|\[StringLength\]" Explore.Domain/
```

### Search for Application Violations
```bash
# Find prohibited using statements in Application
rg "using (Microsoft\.EntityFrameworkCore|Explore\.(Infrastructure|Persistence|API|Blazor))" Explore.Application/

# Find direct DbContext usage
rg "ExploreDbContext" Explore.Application/

# Find ASP.NET Core types in Application
rg "ActionResult|IActionResult|HttpContext" Explore.Application/
```

### Verify Project References
```bash
# Domain should have NO project references
dotnet list Explore.Domain/Explore.Domain.csproj reference

# Application should ONLY reference Domain
dotnet list Explore.Application/Explore.Application.csproj reference

# Check for circular references
dotnet build --no-incremental
```

---

**Next**: See [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.
