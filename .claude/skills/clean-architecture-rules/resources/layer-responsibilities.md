# Layer Responsibilities - What Goes Where

## Decision Tree: Where Does This Code Belong?

```
Is it a business rule or domain concept?
├─ YES → DOMAIN layer
│  └─ Example: Event, Organization, EventStatus enum
│
└─ NO → Is it a use case or application workflow?
   ├─ YES → APPLICATION layer
   │  └─ Example: CreateEventCommand, GetEventListQuery, EventDto
   │
   └─ NO → Is it a technical implementation detail?
      ├─ YES → Does it involve data persistence?
      │  ├─ YES → PERSISTENCE layer
      │  │  └─ Example: ApplicationDbContext, EventRepository
      │  │
      │  └─ NO → INFRASTRUCTURE layer
      │     └─ Example: EmailService, FileStorageService
      │
      └─ NO → Is it a user interface or HTTP endpoint?
         └─ YES → PRESENTATION layer (API or Blazor)
            └─ Example: EventsController, EventsList.razor
```

## 1. Domain Layer (Explore.Domain)

**Purpose**: Pure business logic and domain concepts. The heart of the application.

**Contains**:
- **Entities**: Core business objects with identity (Event, Organization, User)
- **Value Objects**: Immutable objects defined by their attributes (Address, DateRange)
- **Enums**: Domain concepts (EventStatus, Gender, Madhab)
- **Domain Events**: Things that happened in the domain (EventCreatedEvent)
- **Exceptions**: Domain-specific errors (EventCapacityExceededException)

**Does NOT contain**:
- ❌ DTOs (those are in Application)
- ❌ Database configurations (those are in Persistence)
- ❌ API models (those are in API)
- ❌ Any framework dependencies

**File Structure**:
```
Explore.Domain/
├── Entities/
│   ├── Event.cs
│   ├── Organization.cs
│   ├── Participant.cs
│   └── User.cs
├── Enums/
│   ├── EventStatus.cs
│   ├── Gender.cs
│   ├── AgeAudience.cs
│   └── Madhab.cs
├── ValueObjects/
│   ├── Address.cs
│   ├── DateRange.cs
│   └── Geolocation.cs
├── Events/
│   ├── EventCreatedEvent.cs
│   └── EventCancelledEvent.cs
└── Exceptions/
    ├── EventCapacityExceededException.cs
    └── DomainException.cs
```

**Example - Event Entity**:
```csharp
namespace Explore.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public EventStatus Status { get; private set; }
    public int? MaxParticipants { get; private set; }
    public Guid OrganizationId { get; private set; }

    // Navigation properties
    public Organization Organization { get; private set; } = null!;
    public ICollection<Participant> Participants { get; private set; } = new List<Participant>();

    // Business logic methods
    public void Cancel()
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Event is already cancelled");

        if (StartsAt < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot cancel an event that has already started");

        Status = EventStatus.Cancelled;
    }

    public void AddParticipant(Participant participant)
    {
        if (MaxParticipants.HasValue && Participants.Count >= MaxParticipants.Value)
            throw new EventCapacityExceededException(Id, MaxParticipants.Value);

        Participants.Add(participant);
    }
}
```

**Key Principle**: Domain entities contain business rules. They enforce invariants and maintain consistency.

---

## 2. Application Layer (Explore.Application)

**Purpose**: Application business logic, use cases, and orchestration. Defines WHAT the system does.

**Contains**:
- **Commands** (CQRS): Write operations (CreateEventCommand, UpdateEventCommand)
- **Queries** (CQRS): Read operations (GetEventByIdQuery, GetEventListQuery)
- **Handlers**: Process commands and queries using MediatR
- **DTOs**: Data Transfer Objects for API/UI communication
- **Validators**: FluentValidation rules for requests
- **Interfaces**: Contracts that Infrastructure implements (IEventRepository, IEmailService)
- **Mapping**: AutoMapper profiles for Entity ↔ DTO conversion

**Does NOT contain**:
- ❌ Database access logic (use interfaces, implement in Persistence)
- ❌ HTTP concerns (status codes, headers - those are in API)
- ❌ UI logic (those are in Blazor)

**File Structure**:
```
Explore.Application/
├── Features/
│   └── Events/
│       ├── Commands/
│       │   ├── CreateEvent/
│       │   │   ├── CreateEventCommand.cs
│       │   │   ├── CreateEventCommandHandler.cs
│       │   │   └── CreateEventCommandValidator.cs
│       │   └── UpdateEvent/
│       │       ├── UpdateEventCommand.cs
│       │       └── UpdateEventCommandHandler.cs
│       └── Queries/
│           ├── GetEventById/
│           │   ├── GetEventByIdQuery.cs
│           │   └── GetEventByIdQueryHandler.cs
│           └── GetEventList/
│               ├── GetEventListQuery.cs
│               └── GetEventListQueryHandler.cs
├── DTOs/
│   └── Events/
│       ├── EventDto.cs
│       ├── CreateEventDto.cs
│       └── UpdateEventDto.cs
├── Interfaces/
│   ├── IEventRepository.cs
│   ├── IEmailService.cs
│   └── IApplicationDbContext.cs
├── Mapping/
│   └── EventMappingProfile.cs
└── Common/
    ├── Behaviors/
    │   ├── ValidationBehavior.cs
    │   └── LoggingBehavior.cs
    └── Models/
        └── PagedResult.cs
```

**Example - Create Event Command**:
```csharp
// Command (Request)
namespace Explore.Application.Features.Events.Commands.CreateEvent;

using Explore.Domain.Enums;
using MediatR;

public record CreateEventCommand : IRequest<Guid>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public EventStatus Status { get; init; }
    public Guid OrganizationId { get; init; }
}

// Validator
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Event must start in the future");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue)
            .WithMessage("End date must be after start date");
    }
}

// Handler
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public CreateEventCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Create entity using domain logic
        var entity = new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = request.Status,
            OrganizationId = request.OrganizationId
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Send notification (Infrastructure concern, but called via interface)
        await _emailService.SendEventCreatedNotificationAsync(entity.Id, cancellationToken);

        return entity.Id;
    }
}
```

**Key Principle**: Application orchestrates domain logic and coordinates infrastructure services through interfaces.

---

## 3. Persistence Layer (Explore.Persistence)

**Purpose**: Data access implementation using Entity Framework Core. Defines HOW data is stored.

**Contains**:
- **DbContext**: EF Core database context
- **Entity Configurations**: Fluent API configuration (IEntityTypeConfiguration)
- **Repositories**: Concrete implementations of repository interfaces
- **Migrations**: Database schema changes
- **Seed Data**: Initial data for development/testing

**Does NOT contain**:
- ❌ Business logic (that belongs in Domain/Application)
- ❌ API endpoints (those are in API)

**File Structure**:
```
Explore.Persistence/
├── Configurations/
│   ├── EventConfiguration.cs
│   ├── OrganizationConfiguration.cs
│   └── ParticipantConfiguration.cs
├── Repositories/
│   ├── EventRepository.cs
│   └── OrganizationRepository.cs
├── Migrations/
│   ├── 20250103_InitialCreate.cs
│   └── 20250104_AddEventTags.cs
├── Seeders/
│   └── EventSeeder.cs
└── ApplicationDbContext.cs
```

**Example - DbContext**:
```csharp
namespace Explore.Persistence;

using Explore.Application.Interfaces;
using Explore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Example - Entity Configuration**:
```csharp
namespace Explore.Persistence.Configurations;

using Explore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        builder.Property(e => e.Status)
            .HasConversion<string>()  // Store enum as string
            .HasMaxLength(50);

        // PostGIS spatial column
        builder.Property(e => e.Location)
            .HasColumnType("geography (point)");

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");  // Spatial index

        // Relationships
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Participants)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId);
    }
}
```

**Example - Repository Implementation**:
```csharp
namespace Explore.Persistence.Repositories;

using Explore.Application.Interfaces;
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

    public async Task<List<Event>> GetByStatusAsync(EventStatus status, CancellationToken cancellationToken)
    {
        return await _context.Events
            .AsNoTracking()  // Performance optimization for read-only
            .Include(e => e.Organization)
            .Where(e => e.Status == status)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Include(e => e.Organization)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
```

**Key Principle**: Persistence knows about databases, SQL, EF Core. Application doesn't.

---

## 4. Infrastructure Layer (Explore.Infrastructure)

**Purpose**: External services and integrations (email, file storage, external APIs).

**Contains**:
- **Email Services**: SendGrid, SMTP implementations
- **File Storage**: Azure Blob Storage, AWS S3, local file system
- **External APIs**: Federation (planned), Keycloak integration
- **Time Services**: System clock abstraction
- **Caching**: Redis, in-memory cache

**File Structure**:
```
Explore.Infrastructure/
├── Email/
│   └── SendGridEmailService.cs
├── Storage/
│   └── AzureBlobStorageService.cs
├── Federation/ (planned)
│   └── (future federation integrations)
├── Time/
│   └── SystemTimeProvider.cs
└── DependencyInjection.cs
```

**Example - Email Service**:
```csharp
namespace Explore.Infrastructure.Email;

using Explore.Application.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;

    public SendGridEmailService(ISendGridClient client)
    {
        _client = client;
    }

    public async Task SendEventCreatedNotificationAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var from = new EmailAddress("noreply@islamu.org", "ISLAMU Event");
        var to = new EmailAddress("admin@islamu.org");
        var subject = $"New Event Created: {eventId}";
        var plainTextContent = $"A new event has been created with ID: {eventId}";
        var htmlContent = $"<strong>A new event has been created with ID: {eventId}</strong>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        await _client.SendEmailAsync(msg, cancellationToken);
    }
}
```

---

## 5. Presentation Layer (Explore.API)

**Purpose**: HTTP API endpoints using ASP.NET Core controllers.

**Contains**:
- **Controllers**: REST API endpoints
- **Middleware**: Error handling, authentication, logging
- **DTOs/Models**: Request/Response models (if not shared with Application)
- **Filters**: Authorization, validation filters
- **Program.cs**: DI registration (Composition Root)

**File Structure**:
```
Explore.API/
├── Controllers/
│   ├── EventsController.cs
│   ├── OrganizationsController.cs
│   └── ParticipantsController.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Filters/
│   └── ApiKeyAuthorizationFilter.cs
└── Program.cs
```

**Example - Controller**:
```csharp
namespace Explore.API.Controllers;

using Explore.Application.Features.Events.Commands.CreateEvent;
using Explore.Application.Features.Events.Queries.GetEventById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // Keycloak authentication
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEventByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = eventId }, eventId);
    }
}
```

**Key Principle**: Controllers are thin. They receive requests, delegate to MediatR handlers, and return responses.

---

## 6. Presentation Layer (Explore.Blazor)

**Purpose**: Interactive web UI using Blazor Server + WebAssembly.

**Contains**:
- **Pages**: Routable Blazor components (@page directive)
- **Components**: Reusable UI components
- **Services**: UI-specific services (state management, navigation)
- **Program.cs**: DI registration

**File Structure**:
```
Explore.Blazor/
├── Pages/
│   ├── Events/
│   │   ├── EventsList.razor
│   │   ├── EventDetails.razor
│   │   └── CreateEvent.razor
│   └── Index.razor
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Shared/
│       ├── EventCard.razor
│       └── LoadingSpinner.razor
└── Program.cs
```

**Example - Blazor Page**:
```razor
@page "/events"
@using Explore.Application.Features.Events.Queries.GetEventList
@using MediatR
@inject IMediator Mediator

<PageTitle>Events</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4" Class="mb-4">Upcoming Events</MudText>

    @if (_events is null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudGrid>
            @foreach (var evt in _events)
            {
                <MudItem xs="12" md="6">
                    <EventCard Event="@evt" />
                </MudItem>
            }
        </MudGrid>
    }
</MudContainer>

@code {
    private List<EventDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        var query = new GetEventListQuery();
        _events = await Mediator.Send(query);
    }
}
```

---

## Common Scenarios: Where Does This Go?

| Scenario | Layer | Why |
|----------|-------|-----|
| Event capacity validation | **Domain** | Business rule invariant |
| Creating an event via API | **Application** (Command/Handler) | Use case orchestration |
| Saving event to database | **Persistence** (DbContext) | Data persistence implementation |
| Sending event notification email | **Infrastructure** (EmailService) | External service integration |
| Event list API endpoint | **API** (Controller) | HTTP entry point |
| Event list UI page | **Blazor** (Razor page) | User interface |
| EventDto for API response | **Application** (DTOs folder) | Application-level data transfer |
| IEventRepository interface | **Application** (Interfaces) | Abstraction for persistence |
| EventRepository implementation | **Persistence** | Concrete implementation |

---

**Next**: See [violation-examples.md](violation-examples.md) for common mistakes and [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.