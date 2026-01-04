# Fix Patterns - How to Resolve Violations

## Pattern #1: Move Logic to Correct Layer

### Scenario: Business Logic in Controller

**❌ Before** (Logic in wrong layer):
```csharp
// File: src/Explore.API/Controllers/EventsController.cs
[HttpPost]
public async Task<ActionResult> Create(CreateEventDto dto)
{
    // ❌ Business logic in controller
    if (dto.StartsAt < DateTime.UtcNow)
        return BadRequest("Event must start in the future");

    if (dto.EndsAt.HasValue && dto.EndsAt < dto.StartsAt)
        return BadRequest("End date must be after start date");

    var entity = new Event
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        StartsAt = dto.StartsAt,
        EndsAt = dto.EndsAt
    };

    _context.Events.Add(entity);
    await _context.SaveChangesAsync();

    return Ok(entity.Id);
}
```

**✅ After** (Logic in correct layers):
```csharp
// Step 1: Create Command in Application layer
// File: src/Explore.Application/Features/Events/Commands/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Commands;

public record CreateEventCommand : IRequest<Guid>
{
    public string Title { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
}

// Step 2: Create Validator in Application layer
// File: src/Explore.Application/Features/Events/Commands/CreateEventCommandValidator.cs
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Event must start in the future");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue)
            .WithMessage("End date must be after start date");
    }
}

// Step 3: Create Handler in Application layer
// File: src/Explore.Application/Features/Events/Commands/CreateEventCommandHandler.cs
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Event.Create(request.Title, request.OrganizationId);
        entity.SetSchedule(request.StartsAt, request.EndsAt);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

// Step 4: Thin Controller in API layer
// File: src/Explore.API/Controllers/EventsController.cs
[HttpPost]
public async Task<ActionResult<Guid>> Create(CreateEventCommand command, CancellationToken cancellationToken)
{
    var eventId = await _mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = eventId }, eventId);
}
```

**Benefits**:
- ✅ Business logic is testable without HTTP
- ✅ Validation runs automatically via MediatR pipeline
- ✅ Controller is thin (5 lines)
- ✅ Logic can be reused from Blazor, CLI, etc.

---

## Pattern #2: Use Interfaces for Infrastructure Dependencies

### Scenario: Application Needs Email Sending

**❌ Before** (Direct dependency):
```csharp
// File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
using Explore.Infrastructure.Email;  // ❌ VIOLATION!
using SendGrid;  // ❌ Infrastructure concern

public class CreateEventHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly SendGridEmailService _emailService;  // ❌ Concrete class

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // ... create event

        await _emailService.SendAsync(email);  // ❌ Tightly coupled

        return entity.Id;
    }
}
```

**✅ After** (Dependency Inversion):
```csharp
// Step 1: Define interface in Application layer
// File: src/Explore.Application/Interfaces/IEmailService.cs
namespace Explore.Application.Interfaces;

public interface IEmailService
{
    Task SendEventCreatedNotificationAsync(
        Guid eventId,
        string eventTitle,
        string organizerEmail,
        CancellationToken cancellationToken = default);
}

// Step 2: Use interface in Application layer
// File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
namespace Explore.Application.Features.Events.Commands;

using Explore.Application.Interfaces;  // ✅ Same layer

public class CreateEventHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;  // ✅ Interface

    public CreateEventHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Event.Create(request.Title, request.OrganizationId);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // ✅ Calls interface, doesn't know about SendGrid
        await _emailService.SendEventCreatedNotificationAsync(
            entity.Id,
            entity.Title,
            request.OrganizerEmail,
            cancellationToken);

        return entity.Id;
    }
}

// Step 3: Implement in Infrastructure layer
// File: src/Explore.Infrastructure/Email/SendGridEmailService.cs
namespace Explore.Infrastructure.Email;

using Explore.Application.Interfaces;  // ✅ Implements interface
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

// Step 4: Register in API/Blazor (Composition Root)
// File: src/Explore.API/Program.cs
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
// File: src/Explore.Application/Features/Events/Queries/GetEventListHandler.cs
using Explore.Persistence;  // ❌ VIOLATION!
using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class GetEventListHandler : IRequestHandler<GetEventListQuery, List<EventDto>>
{
    private readonly ApplicationDbContext _context;  // ❌ Concrete class

    public async Task<List<EventDto>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
    {
        // ❌ Complex EF Core query in Application layer
        return await _context.Events
            .Include(e => e.Organization)
            .Include(e => e.Participants)
            .Where(e => e.Status == EventStatus.Published)
            .Where(e => e.StartsAt >= DateTime.UtcNow)
            .OrderBy(e => e.StartsAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                OrganizationName = e.Organization.Name,
                ParticipantCount = e.Participants.Count
            })
            .ToListAsync(cancellationToken);
    }
}
```

**✅ After** (Repository pattern):
```csharp
// Step 1: Define repository interface in Application layer
// File: src/Explore.Application/Interfaces/IEventRepository.cs
namespace Explore.Application.Interfaces;

using Explore.Domain.Entities;
using Explore.Domain.Enums;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Event>> GetPublishedUpcomingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<Event>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(
        EventStatus? status = null,
        CancellationToken cancellationToken = default);

    void Add(Event entity);
    void Update(Event entity);
    void Remove(Event entity);
}

// Step 2: Use interface in Application layer
// File: src/Explore.Application/Features/Events/Queries/GetEventListHandler.cs
namespace Explore.Application.Features.Events.Queries;

using Explore.Application.Interfaces;  // ✅ Same layer

public class GetEventListHandler : IRequestHandler<GetEventListQuery, PagedResult<EventDto>>
{
    private readonly IEventRepository _repository;  // ✅ Interface
    private readonly IMapper _mapper;

    public GetEventListHandler(IEventRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<EventDto>> Handle(
        GetEventListQuery request,
        CancellationToken cancellationToken)
    {
        var events = await _repository.GetPublishedUpcomingAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalCount = await _repository.GetTotalCountAsync(
            EventStatus.Published,
            cancellationToken);

        var dtos = _mapper.Map<List<EventDto>>(events);

        return new PagedResult<EventDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}

// Step 3: Implement repository in Persistence layer
// File: src/Explore.Persistence/Repositories/EventRepository.cs
namespace Explore.Persistence.Repositories;

using Explore.Application.Interfaces;  // ✅ Implements interface
using Explore.Domain.Entities;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Organization)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Event>> GetPublishedUpcomingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()  // Performance: read-only query
            .Include(e => e.Organization)
            .Include(e => e.Participants)
            .Where(e => e.Status == EventStatus.Published)
            .Where(e => e.StartsAt >= DateTime.UtcNow)
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        EventStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return await query.CountAsync(cancellationToken);
    }

    public void Add(Event entity) => _context.Events.Add(entity);
    public void Update(Event entity) => _context.Events.Update(entity);
    public void Remove(Event entity) => _context.Events.Remove(entity);
}

// Step 4: Register in API/Blazor
// File: src/Explore.API/Program.cs
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
// File: src/Explore.Domain/Entities/Event.cs
using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class Event
{
    [Required]  // ❌ This is validation, not an invariant
    [MaxLength(200)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Arbitrary validation rule
    public int? MaxParticipants { get; set; }
}
```

**✅ After** (Proper separation):
```csharp
// File: src/Explore.Domain/Entities/Event.cs
namespace Explore.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int? MaxParticipants { get; private set; }

    private readonly List<Participant> _participants = new();
    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();

    // ✅ INVARIANT: Title cannot be empty (business rule)
    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title.Trim();
    }

    // ✅ INVARIANT: Cannot exceed max participants (business rule)
    public void AddParticipant(Participant participant)
    {
        if (MaxParticipants.HasValue && _participants.Count >= MaxParticipants.Value)
            throw new EventCapacityExceededException(Id, MaxParticipants.Value);

        _participants.Add(participant);
    }

    // ✅ INVARIANT: Max participants must be >= current participants
    public void SetMaxParticipants(int maxParticipants)
    {
        if (maxParticipants < _participants.Count)
            throw new InvalidOperationException(
                $"Cannot set max participants to {maxParticipants} when {_participants.Count} are already registered");

        MaxParticipants = maxParticipants;
    }
}

// File: src/Explore.Application/Features/Events/Commands/CreateEventCommandValidator.cs
namespace Explore.Application.Features.Events.Commands;

using FluentValidation;

// ✅ INPUT VALIDATION: Can vary by use case
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-]+$").WithMessage("Title contains invalid characters");

        RuleFor(x => x.MaxParticipants)
            .InclusiveBetween(1, 10000).When(x => x.MaxParticipants.HasValue)
            .WithMessage("Max participants must be between 1 and 10000");
    }
}

// File: src/Explore.Application/Features/Events/Commands/UpdateEventCommandValidator.cs
// ✅ UPDATE validation can be different (e.g., allow partial updates)
public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        // Maybe title is optional when updating
        RuleFor(x => x.Title)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must not exceed 200 characters");
    }
}

// File: src/Explore.Persistence/Configurations/EventConfiguration.cs
namespace Explore.Persistence.Configurations;

// ✅ DATABASE CONSTRAINTS
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(e => e.Title)
            .IsRequired()        // Database constraint
            .HasMaxLength(200);  // Database constraint
    }
}
```

**Decision Matrix**:

| Question | Domain Invariant | Application Validation | Persistence Constraint |
|----------|------------------|----------------------|----------------------|
| Can it vary by use case? | ❌ No | ✅ Yes | ❌ No |
| Can entity exist without it? | ❌ No | ✅ Yes | ✅ Yes |
| Is it a business rule? | ✅ Yes | ⚠️ Maybe | ❌ No |
| Example | "Capacity cannot be less than current participants" | "Title required for CREATE, optional for UPDATE" | "Title max 200 chars in DB" |

---

## Pattern #5: Sharing Code Between Layers (DTOs)

### Scenario: Sharing DTOs Between API and Blazor

**❌ Before** (DTOs in wrong layer):
```csharp
// File: src/Explore.API/Models/EventDto.cs
namespace Explore.API.Models;  // ❌ API-specific

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// File: src/Explore.Blazor.Client/Models/EventDto.cs
namespace Explore.Blazor.Client.Models;  // ❌ Duplicated!

public class EventDto  // ❌ Same DTO copied
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

**✅ After** (DTOs in Application layer):
```csharp
// File: src/Explore.Application/DTOs/Events/EventDto.cs
namespace Explore.Application.DTOs.Events;

// ✅ Shared DTO in Application layer
public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
}

// File: src/Explore.API/Controllers/EventsController.cs
using Explore.Application.DTOs.Events;  // ✅ References Application DTOs

[HttpGet("{id:guid}")]
public async Task<ActionResult<EventDto>> GetById(Guid id)
{
    var query = new GetEventByIdQuery(id);
    var result = await _mediator.Send(query);
    return result is not null ? Ok(result) : NotFound();
}

// File: src/Explore.Blazor.Client/Pages/EventsList.razor
@using Explore.Application.DTOs.Events  @* ✅ References same DTOs *@
@inject HttpClient Http

@code {
    private List<EventDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events = await Http.GetFromJsonAsync<List<EventDto>>("/api/v1/events");
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

```csharp
// File: src/Explore.API/Program.cs
using Explore.Application;
using Explore.Infrastructure;
using Explore.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Application layer services
builder.Services.AddApplication();  // Extension method from Application

// ✅ Register Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);  // Extension method from Infrastructure

// ✅ Register Persistence layer services
builder.Services.AddPersistence(builder.Configuration);  // Extension method from Persistence

var app = builder.Build();
app.Run();

// File: src/Explore.Application/DependencyInjection.cs
namespace Explore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        return services;
    }
}

// File: src/Explore.Persistence/DependencyInjection.cs
namespace Explore.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.UseNetTopologySuite()));  // PostGIS support

        // Register interface → implementation mapping
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Register repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();

        return services;
    }
}

// File: src/Explore.Infrastructure/DependencyInjection.cs
namespace Explore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register email service
        services.AddScoped<ISendGridClient>(provider =>
            new SendGridClient(configuration["SendGrid:ApiKey"]));

        services.AddScoped<IEmailService, SendGridEmailService>();

        // Register file storage
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        // Register time provider
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
```

**Benefits**:
- ✅ All layers register their own dependencies
- ✅ API/Blazor just calls extension methods
- ✅ Each layer is responsible for its own DI setup
- ✅ Clear separation of concerns

---

## Quick Reference: Fix Decision Tree

```
Violation detected. What should I do?

1. Is it a business rule?
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
