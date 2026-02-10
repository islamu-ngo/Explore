# Layer Responsibilities - What Goes Where

> **Project-Agnostic Layer Guidelines**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Decision Tree: Where Does This Code Belong?

```
Is it a business rule or domain concept?
├─ YES → DOMAIN layer
│  └─ Example: {Entity}, {LookupEntity}, {EntityStatus} enum
│
└─ NO → Is it a use case or application workflow?
   ├─ YES → APPLICATION layer
   │  └─ Example: Create{Entity}Command, Get{Entity}ListRequest, {Entity}Dto
   │
   └─ NO → Is it a technical implementation detail?
      ├─ YES → Does it involve data persistence?
      │  ├─ YES → PERSISTENCE layer
      │  │  └─ Example: {DbContext}, {Entity}Repository
      │  │
      │  └─ NO → INFRASTRUCTURE layer
      │     └─ Example: EmailService, FileStorageService
      │
      └─ NO → Is it a user interface or HTTP endpoint?
         └─ YES → PRESENTATION layer (API or Blazor)
            └─ Example: {Entity}Controller, {Entities}List.razor
```

## 1. Domain Layer ({Project}.Domain)

**Purpose**: Pure business logic and domain concepts. The heart of the application.

**Contains**:
- **Entities**: Core business objects with identity ({Entity}, {RelatedEntity}, User)
- **Value Objects**: Immutable objects defined by their attributes (Address, DateRange)
- **Enums**: Domain concepts ({EntityStatus}, Gender, {LookupEntity})
- **Domain Events**: Things that happened in the domain ({Entity}CreatedEvent)
- **Exceptions**: Domain-specific errors ({Entity}CapacityExceededException)

**Does NOT contain**:
- DTOs (those are in Application)
- Database configurations (those are in Persistence)
- API models (those are in API)
- Any framework dependencies

**File Structure**:
```
{Project}.Domain/
├── Entities/
│   ├── {Entity}.cs
│   ├── {RelatedEntity}.cs
│   ├── {LinkEntity}.cs
│   └── User.cs
├── Enums/
│   ├── {EntityStatus}.cs
│   ├── Gender.cs
│   └── {LookupEntity}Type.cs
├── ValueObjects/
│   ├── Address.cs
│   ├── DateRange.cs
│   └── Geolocation.cs
├── Events/
│   ├── {Entity}CreatedEvent.cs
│   └── {Entity}CancelledEvent.cs
└── Exceptions/
    ├── {Entity}CapacityExceededException.cs
    └── DomainException.cs
```

**Example - Entity**:
```csharp
namespace {Project}.Domain.Entities;

public class {Entity}
{
    public {IdType} Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public {EntityStatus} Status { get; private set; }
    public int? MaxParticipants { get; private set; }
    public {IdType} {RelatedEntity}Id { get; private set; }

    // Navigation properties
    public {RelatedEntity} {RelatedEntity} { get; private set; } = null!;
    public ICollection<{LinkEntity}> {LinkEntities} { get; private set; } = new List<{LinkEntity}>();

    // Business logic methods
    public void Cancel()
    {
        if (Status == {EntityStatus}.Cancelled)
            throw new InvalidOperationException("{Entity} is already cancelled");

        if (StartsAt < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot cancel a {entity} that has already started");

        Status = {EntityStatus}.Cancelled;
    }
}
```

**Key Principle**: Domain entities contain business rules. They enforce invariants and maintain consistency.

---

## 2. Application Layer ({Project}.Application)

**Purpose**: Application business logic, use cases, and orchestration. Defines WHAT the system does.

**Contains**:
- **Commands** (CQRS): Write operations (Create{Entity}Command, Update{Entity}Command)
- **Queries** (CQRS): Read operations (Get{Entity}DetailsRequest, Get{Entity}ListRequest)
- **Handlers**: Process commands and queries using MediatR
- **DTOs**: Data Transfer Objects for API/UI communication
- **Validators**: FluentValidation rules for requests
- **Interfaces**: Contracts that Infrastructure implements (I{Entity}Repository, IEmailService)
- **Mapping**: AutoMapper profiles for Entity ↔ DTO conversion

**Does NOT contain**:
- Database access logic (use interfaces, implement in Persistence)
- HTTP concerns (status codes, headers - those are in API)
- UI logic (those are in Blazor)

**File Structure**:
```
{Project}.Application/
├── Features/
│   └── {Entities}/
│       ├── Requests/
│       │   ├── Commands/
│       │   │   ├── Create{Entity}Command.cs
│       │   │   ├── Update{Entity}Command.cs
│       │   │   └── Delete{Entity}Command.cs
│       │   └── Queries/
│       │       ├── Get{Entity}ListRequest.cs
│       │       ├── Get{Entity}DetailsRequest.cs
│       │       └── Get{Entities}By{RelatedEntity}Request.cs
│       └── Handlers/
│           ├── Commands/
│           │   ├── Create{Entity}CommandHandler.cs
│           │   ├── Update{Entity}CommandHandler.cs
│           │   └── Delete{Entity}CommandHandler.cs
│           └── Queries/
│               ├── Get{Entity}ListRequestHandler.cs
│               ├── Get{Entity}DetailsRequestHandler.cs
│               └── Get{Entities}By{RelatedEntity}RequestHandler.cs
├── DTOs/
│   └── {Entity}/
│       ├── {Entity}Dto.cs
│       ├── {Entity}ListDto.cs
│       ├── Create{Entity}Dto.cs
│       ├── Update{Entity}Dto.cs
│       └── Validators/
│           ├── Create{Entity}DtoValidator.cs
│           └── Update{Entity}DtoValidator.cs
├── Contracts/
│   ├── Persistence/
│   │   ├── I{Entity}Repository.cs
│   │   ├── IGenericRepository.cs
│   │   └── I{RelatedEntity}Repository.cs
│   └── Infrastructure/
│       ├── IEmailService.cs
│       └── IFileStorageService.cs
├── Profiles/
│   └── MappingProfile.cs
└── Responses/
    └── BaseCommandResponse.cs
```

**Example - Create Command Handler**:
```csharp
// Command (Request)
namespace {Project}.Application.Features.{Entities}.Requests.Commands;

using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;
using MediatR;

public record Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; } = null!;
}

// Handler
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly I{RelatedEntity}Repository _{relatedEntity}Repository;
    private readonly IMapper _mapper;

    public Create{Entity}CommandHandler(
        I{Entity}Repository {entity}Repository,
        I{RelatedEntity}Repository {relatedEntity}Repository,
        IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _{relatedEntity}Repository = {relatedEntity}Repository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // Validator instantiated manually with dependencies
        var validator = new Create{Entity}DtoValidator(_{relatedEntity}Repository);
        var validationResult = await validator.ValidateAsync(request.{Entity}Dto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "{Entity} creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Create entity using domain logic
        var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);
        entity = await _{entity}Repository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "{Entity} created successfully.";

        return response;
    }
}
```

**Key Principle**: Application orchestrates domain logic and coordinates infrastructure services through interfaces.

---

## 3. Persistence Layer ({Project}.Persistence)

**Purpose**: Data access implementation using Entity Framework Core. Defines HOW data is stored.

**Contains**:
- **DbContext**: EF Core database context
- **Entity Configurations**: Fluent API configuration (IEntityTypeConfiguration)
- **Repositories**: Concrete implementations of repository interfaces
- **Migrations**: Database schema changes
- **Seed Data**: Initial data for development/testing

**Does NOT contain**:
- Business logic (that belongs in Domain/Application)
- API endpoints (those are in API)

**File Structure**:
```
{Project}.Persistence/
├── Configurations/
│   ├── {Entity}Configuration.cs
│   ├── {RelatedEntity}Configuration.cs
│   └── {LinkEntity}Configuration.cs
├── Repositories/
│   ├── GenericRepository.cs
│   ├── {Entity}Repository.cs
│   └── {RelatedEntity}Repository.cs
├── Migrations/
│   ├── 20250103_InitialCreate.cs
│   └── 20250104_Add{Entity}Tags.cs
├── Seeders/
│   └── {LookupEntity}Seeder.cs
└── {DbContext}.cs
```

**Example - DbContext**:
```csharp
namespace {Project}.Persistence;

using {Project}.Domain;
using Microsoft.EntityFrameworkCore;

public class {DbContext} : DbContext
{
    public {DbContext}(DbContextOptions<{DbContext}> options)
        : base(options)
    {
    }

    public DbSet<{Entity}> {Entities} => Set<{Entity}>();
    public DbSet<{RelatedEntity}> {RelatedEntities} => Set<{RelatedEntity}>();
    public DbSet<{LinkEntity}> {LinkEntities} => Set<{LinkEntity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({DbContext}).Assembly);
    }
}
```

**Example - Entity Configuration**:
```csharp
namespace {Project}.Persistence.Configurations;

using {Project}.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{entities}");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        // Relationships
        builder.HasOne(e => e.{RelatedEntity})
            .WithMany()
            .HasForeignKey(e => e.{RelatedEntity}Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.{LinkEntities})
            .WithOne(l => l.{Entity})
            .HasForeignKey(l => l.{Entity}Id);
    }
}
```

**Example - Repository Implementation**:
```csharp
namespace {Project}.Persistence.Repositories;

using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;
using Microsoft.EntityFrameworkCore;

public class {Entity}Repository : GenericRepository<{Entity}, {IdType}>, I{Entity}Repository
{
    private readonly {DbContext} _dbContext;

    public {Entity}Repository({DbContext} dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity})
            .ToListAsync();
    }

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity})
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
```

**Key Principle**: Persistence knows about databases, SQL, EF Core. Application doesn't.

---

## 4. Infrastructure Layer ({Project}.Infrastructure)

**Purpose**: External services and integrations (email, file storage, external APIs).

**Contains**:
- **Email Services**: SendGrid, SMTP implementations
- **File Storage**: Azure Blob Storage, AWS S3, local file system
- **External APIs**: Federation (planned), third-party integrations
- **Time Services**: System clock abstraction
- **Caching**: Redis, in-memory cache

**File Structure**:
```
{Project}.Infrastructure/
├── Email/
│   └── SendGridEmailService.cs
├── Storage/
│   └── AzureBlobStorageService.cs
├── External/
│   └── (third-party integrations)
├── Time/
│   └── SystemTimeProvider.cs
└── DependencyInjection.cs
```

**Example - Email Service**:
```csharp
namespace {Project}.Infrastructure.Email;

using {Project}.Application.Contracts.Infrastructure;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;

    public SendGridEmailService(ISendGridClient client)
    {
        _client = client;
    }

    public async Task Send{Entity}CreatedNotificationAsync({IdType} {entity}Id, CancellationToken cancellationToken)
    {
        var from = new EmailAddress("noreply@example.org", "{Project}");
        var to = new EmailAddress("admin@example.org");
        var subject = $"New {Entity} Created: {{entity}Id}";
        var plainTextContent = $"A new {entity} has been created with ID: {{entity}Id}";
        var htmlContent = $"<strong>A new {entity} has been created with ID: {{entity}Id}</strong>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        await _client.SendEmailAsync(msg, cancellationToken);
    }
}
```

---

## 5. Presentation Layer ({Project}.API)

**Purpose**: HTTP API endpoints using ASP.NET Core controllers.

**Contains**:
- **Controllers**: REST API endpoints
- **Middleware**: Error handling, authentication, logging
- **DTOs/Models**: Request/Response models (if not shared with Application)
- **Filters**: Authorization, validation filters
- **Program.cs**: DI registration (Composition Root)

**File Structure**:
```
{Project}.API/
├── Controllers/
│   ├── {Entity}Controller.cs
│   ├── {RelatedEntity}Controller.cs
│   └── {LookupEntity}Controller.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Filters/
│   └── ApiKeyAuthorizationFilter.cs
└── Program.cs
```

**Example - Controller**:
```csharp
namespace {Project}.API.Controllers;

using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Features.{Entities}.Requests.Queries;
using {Project}.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Entity}Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EndpointSummary("Get {Entity} Details")]
    [ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id, CancellationToken cancellationToken)
    {
        var request = new Get{Entity}DetailsRequest { Id = id };
        var result = await _mediator.Send(request, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    [Authorize]
    [EndpointSummary("Create {Entity}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create(
        [FromBody] Create{Entity}Dto dto,
        CancellationToken cancellationToken)
    {
        var command = new Create{Entity}Command { {Entity}Dto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}
```

**Key Principle**: Controllers are thin. They receive requests, delegate to MediatR handlers, and return responses.

---

## 6. Presentation Layer ({Project}.Blazor)

**Purpose**: Interactive web UI using Blazor Server + WebAssembly.

**Contains**:
- **Pages**: Routable Blazor components (@page directive)
- **Components**: Reusable UI components
- **Services**: UI-specific services (state management, navigation)
- **Program.cs**: DI registration

**File Structure**:
```
{Project}.Blazor/
├── Pages/
│   ├── {Entities}/
│   │   ├── {Entities}List.razor
│   │   ├── {Entity}Details.razor
│   │   └── Create{Entity}.razor
│   └── Index.razor
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Shared/
│       ├── {Entity}Card.razor
│       └── LoadingSpinner.razor
└── Program.cs
```

**Example - Blazor Page**:
```razor
@page "/{entities}"
@using {Project}.Application.DTOs.{Entity}
@using {Project}.Application.Features.{Entities}.Requests.Queries
@using MediatR
@inject IMediator Mediator

<PageTitle>{Entities}</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4" Class="mb-4">All {Entities}</MudText>

    @if (_{entities} is null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudGrid>
            @foreach (var item in _{entities})
            {
                <MudItem xs="12" md="6">
                    <{Entity}Card {Entity}="@item" />
                </MudItem>
            }
        </MudGrid>
    }
</MudContainer>

@code {
    private List<{Entity}ListDto>? _{entities};

    protected override async Task OnInitializedAsync()
    {
        var request = new Get{Entity}ListRequest();
        _{entities} = await Mediator.Send(request);
    }
}
```

---

## Common Scenarios: Where Does This Go?

| Scenario | Layer | Why |
|----------|-------|-----|
| {Entity} capacity validation | **Domain** | Business rule invariant |
| Creating a {entity} via API | **Application** (Command/Handler) | Use case orchestration |
| Saving {entity} to database | **Persistence** (DbContext) | Data persistence implementation |
| Sending {entity} notification email | **Infrastructure** (EmailService) | External service integration |
| {Entity} list API endpoint | **API** (Controller) | HTTP entry point |
| {Entity} list UI page | **Blazor** (Razor page) | User interface |
| {Entity}Dto for API response | **Application** (DTOs folder) | Application-level data transfer |
| I{Entity}Repository interface | **Application** (Contracts) | Abstraction for persistence |
| {Entity}Repository implementation | **Persistence** | Concrete implementation |

---

**Next**: See [violation-examples.md](violation-examples.md) for common mistakes and [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.
