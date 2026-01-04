# Common Violations and Error Messages

## Violation #1: Domain Referencing Infrastructure

### ❌ The Problem

```csharp
// File: src/Explore.Domain/Entities/Event.cs
namespace Explore.Domain.Entities;

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
File: src/Explore.Domain/Entities/Event.cs
Violation: Domain layer cannot reference Microsoft.EntityFrameworkCore

REASON:
Domain must be framework-agnostic. EF Core is an infrastructure concern.

FIX:
1. Remove [Key] attribute
2. Configure primary key in Persistence layer using Fluent API
3. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#domain-annotations
```

### ✅ The Fix

```csharp
// File: src/Explore.Domain/Entities/Event.cs
namespace Explore.Domain.Entities;

// ✅ NO framework dependencies
public class Event
{
    public Guid Id { get; private set; }  // ✅ Plain C# property
    public string Title { get; private set; } = string.Empty;

    // Constructor for EF Core (required for materialization)
    private Event() { }

    // Factory method for creating new events
    public static Event Create(string title, Guid organizationId)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// File: src/Explore.Persistence/Configurations/EventConfiguration.cs
namespace Explore.Persistence.Configurations;

using Explore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);  // ✅ Configure key here, not in Domain

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);
    }
}
```

---

## Violation #2: Application Referencing DbContext Directly

### ❌ The Problem

```csharp
// File: src/Explore.Application/Features/Events/Queries/GetEventListHandler.cs
namespace Explore.Application.Features.Events.Queries;

using Explore.Persistence;  // ❌ VIOLATION!
using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class GetEventListHandler : IRequestHandler<GetEventListQuery, List<EventDto>>
{
    private readonly ApplicationDbContext _context;  // ❌ Concrete class

    public async Task<List<EventDto>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
    {
        return await _context.Events  // ❌ Direct DbSet access
            .Where(e => e.Status == EventStatus.Published)
            .ToListAsync(cancellationToken);
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: src/Explore.Application/Features/Events/Queries/GetEventListHandler.cs
Violation: Application layer cannot reference Explore.Persistence or Microsoft.EntityFrameworkCore

REASON:
Application should not depend on concrete database implementation.
This makes it impossible to:
- Unit test without a database
- Switch database providers
- Mock data for testing

FIX:
1. Create IApplicationDbContext interface in Application layer
2. Use interface instead of concrete ApplicationDbContext
3. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#application-dbcontext
```

### ✅ The Fix

```csharp
// File: src/Explore.Application/Interfaces/IApplicationDbContext.cs
namespace Explore.Application.Interfaces;

using Explore.Domain.Entities;
using Microsoft.EntityFrameworkCore;  // ✅ OK - Just for DbSet<T> type

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }
    DbSet<Organization> Organizations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

// File: src/Explore.Persistence/ApplicationDbContext.cs
namespace Explore.Persistence;

using Explore.Application.Interfaces;  // ✅ Implements interface
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

// File: src/Explore.Application/Features/Events/Queries/GetEventListHandler.cs
namespace Explore.Application.Features.Events.Queries;

using Explore.Application.Interfaces;  // ✅ Interface in same layer

public class GetEventListHandler : IRequestHandler<GetEventListQuery, List<EventDto>>
{
    private readonly IApplicationDbContext _context;  // ✅ Abstraction

    public GetEventListHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventDto>> Handle(GetEventListQuery request, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Where(e => e.Status == EventStatus.Published)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title
            })
            .ToListAsync(cancellationToken);
    }
}

// File: src/Explore.API/Program.cs (DI Registration)
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());
```

---

## Violation #3: Application Using ASP.NET Core Types

### ❌ The Problem

```csharp
// File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
namespace Explore.Application.Features.Events.Commands;

using Microsoft.AspNetCore.Http;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;   // ❌ VIOLATION!

public class CreateEventHandler : IRequestHandler<CreateEventCommand, ActionResult<Guid>>
{
    public async Task<ActionResult<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // ... create event logic

        return new CreatedAtActionResult("GetById", "Events", new { id = eventId }, eventId);
        // ❌ Returning ASP.NET Core type
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
Violation: Application layer cannot reference Microsoft.AspNetCore.*

REASON:
Application should be framework-agnostic. It should work with:
- REST APIs
- gRPC services
- Console applications
- Background jobs
Returning ActionResult ties it to ASP.NET Core.

FIX:
1. Return plain Guid (or Result<Guid> for error handling)
2. Let Controller map to ActionResult
3. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#aspnet-types
```

### ✅ The Fix

```csharp
// File: src/Explore.Application/Features/Events/Commands/CreateEventCommand.cs
namespace Explore.Application.Features.Events.Commands;

using MediatR;

public record CreateEventCommand : IRequest<Guid>  // ✅ Returns plain Guid
{
    public string Title { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public Guid OrganizationId { get; init; }
}

// File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
public class CreateEventHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Event.Create(request.Title, request.OrganizationId);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;  // ✅ Plain Guid
    }
}

// File: src/Explore.API/Controllers/EventsController.cs
namespace Explore.API.Controllers;

using Microsoft.AspNetCore.Mvc;  // ✅ OK in API layer
using MediatR;

[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = await _mediator.Send(command, cancellationToken);

        // ✅ Controller handles HTTP-specific concerns
        return CreatedAtAction(nameof(GetById), new { id = eventId }, eventId);
    }
}
```

---

## Violation #4: Domain Using Data Annotations for Validation

### ❌ The Problem

```csharp
// File: src/Explore.Domain/Entities/Event.cs
namespace Explore.Domain.Entities;

using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class Event
{
    public Guid Id { get; set; }

    [Required]  // ❌ Presentation concern
    [MaxLength(200)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Validation belongs in Application
    public int? MaxParticipants { get; set; }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Domain
File: src/Explore.Domain/Entities/Event.cs
Violation: Domain layer should not use System.ComponentModel.DataAnnotations

REASON:
Data annotations mix concerns:
- [Required], [Range] = Validation (belongs in Application with FluentValidation)
- [MaxLength] = Database constraint (belongs in Persistence with Fluent API)

Validation rules can differ by use case:
- Creating an event might require Title
- Updating might allow partial updates
Domain should enforce INVARIANTS, not validation rules.

FIX:
1. Remove data annotations
2. Add FluentValidation in Application layer
3. Configure constraints in Persistence layer
4. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#domain-validation
```

### ✅ The Fix

```csharp
// File: src/Explore.Domain/Entities/Event.cs
namespace Explore.Domain.Entities;

// ✅ NO annotations
public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int? MaxParticipants { get; private set; }

    // ✅ Domain enforces INVARIANTS (business rules that must ALWAYS be true)
    public void SetMaxParticipants(int maxParticipants)
    {
        if (maxParticipants < 1)
            throw new ArgumentException("Max participants must be at least 1", nameof(maxParticipants));

        if (maxParticipants < Participants.Count)
            throw new InvalidOperationException(
                $"Cannot set max participants to {maxParticipants} when {Participants.Count} are already registered");

        MaxParticipants = maxParticipants;
    }
}

// File: src/Explore.Application/Features/Events/Commands/CreateEventCommandValidator.cs
namespace Explore.Application.Features.Events.Commands;

using FluentValidation;

// ✅ Application layer validates INPUT
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.MaxParticipants)
            .InclusiveBetween(1, 10000).When(x => x.MaxParticipants.HasValue)
            .WithMessage("Max participants must be between 1 and 10000");
    }
}

// File: src/Explore.Persistence/Configurations/EventConfiguration.cs
namespace Explore.Persistence.Configurations;

// ✅ Persistence layer configures DATABASE CONSTRAINTS
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

---

## Violation #5: Infrastructure Referencing API/Blazor

### ❌ The Problem

```csharp
// File: src/Explore.Infrastructure/Email/EmailService.cs
namespace Explore.Infrastructure.Email;

using Explore.API.Controllers;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;  // ❌ VIOLATION!

public class EmailService : IEmailService
{
    public async Task SendEventNotificationAsync(Guid eventId)
    {
        // ❌ Calling controller directly
        var controller = new EventsController();
        var result = await controller.GetById(eventId);

        // ... send email
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Infrastructure
File: src/Explore.Infrastructure/Email/EmailService.cs
Violation: Infrastructure layer cannot reference Explore.API or presentation layers

REASON:
Infrastructure is below Presentation in the architecture.
Controllers should call Infrastructure, not the other way around.

FIX:
1. Pass event data as parameters to EmailService
2. Or inject IMediator to fetch data using queries
3. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#infrastructure-data
```

### ✅ The Fix

```csharp
// File: src/Explore.Application/Interfaces/IEmailService.cs
namespace Explore.Application.Interfaces;

public interface IEmailService
{
    Task SendEventNotificationAsync(string eventTitle, string organizationName, DateTime startsAt);
}

// File: src/Explore.Infrastructure/Email/EmailService.cs
namespace Explore.Infrastructure.Email;

using Explore.Application.Interfaces;  // ✅ Application interface only

public class EmailService : IEmailService
{
    private readonly ISendGridClient _client;

    public async Task SendEventNotificationAsync(string eventTitle, string organizationName, DateTime startsAt)
    {
        // ✅ Uses passed data, doesn't fetch it itself
        var body = $"Event '{eventTitle}' by {organizationName} starts at {startsAt:f}";
        await _client.SendEmailAsync(body);
    }
}

// File: src/Explore.Application/Features/Events/Commands/CreateEventHandler.cs
public class CreateEventHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Event.Create(request.Title, request.OrganizationId);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // ✅ Application orchestrates: gets data and calls Infrastructure
        var organization = await _context.Organizations.FindAsync(entity.OrganizationId);
        await _emailService.SendEventNotificationAsync(
            entity.Title,
            organization!.Name,
            entity.StartsAt);

        return entity.Id;
    }
}
```

---

## Violation #6: Circular References Between Projects

### ❌ The Problem

```xml
<!-- File: src/Explore.Application/Explore.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
  <ProjectReference Include="..\Explore.Infrastructure\Explore.Infrastructure.csproj" />
  <!-- ❌ VIOLATION! -->
</ItemGroup>

<!-- File: src/Explore.Infrastructure/Explore.Infrastructure.csproj -->
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
4. See: .claude/skills/clean-architecture-rules/resources/fix-patterns.md#circular-refs
```

### ✅ The Fix

```xml
<!-- File: src/Explore.Application/Explore.Application.csproj -->
<ItemGroup>
  <!-- ✅ ONLY reference Domain -->
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
</ItemGroup>

<!-- File: src/Explore.Infrastructure/Explore.Infrastructure.csproj -->
<ItemGroup>
  <!-- ✅ References Application and Domain -->
  <ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />
  <ProjectReference Include="..\Explore.Domain\Explore.Domain.csproj" />
</ItemGroup>

<!-- File: src/Explore.API/Explore.API.csproj -->
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
rg "using (Microsoft\.(EntityFrameworkCore|AspNetCore)|Explore\.(Application|Infrastructure|API|Blazor))" src/Explore.Domain/

# Find data annotations in Domain
rg "\[Required\]|\[MaxLength\]|\[Range\]" src/Explore.Domain/
```

### Search for Application Violations
```bash
# Find prohibited using statements in Application
rg "using (Microsoft\.(EntityFrameworkCore|AspNetCore\.Mvc)|Explore\.(Infrastructure|Persistence|API|Blazor))" src/Explore.Application/

# Find direct DbContext usage
rg "ApplicationDbContext" src/Explore.Application/
```

### Verify Project References
```bash
# Domain should have NO project references
dotnet list src/Explore.Domain/Explore.Domain.csproj reference

# Application should ONLY reference Domain
dotnet list src/Explore.Application/Explore.Application.csproj reference
```

---

**Next**: See [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.
