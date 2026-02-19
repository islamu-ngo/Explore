# Fix Patterns - How to Resolve Violations

> **Project-Agnostic Fix Patterns Reference**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Pattern #1: Move Logic to Correct Layer

### Scenario: Business Logic in Controller

**❌ Before** (Logic in wrong layer):
```csharp
// File: {Project}.API/Controllers/{Entity}Controller.cs
[HttpPost]
public async Task<ActionResult> Create(Create{Entity}Dto dto)
{
    // ❌ Business logic in controller
    if (dto.Title == null || dto.Title.Length > 500)
        return BadRequest("Title must be between 1 and 500 characters");

    if (dto.{LookupEntity}Id <= 0)
        return BadRequest("{LookupEntity} is required");

    var {entity} = new {Entity}
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        {LookupEntity}Id = dto.{LookupEntity}Id,
        ViewCount = 0
    };

    _dbContext.{Entities}.Add({entity});
    await _dbContext.SaveChangesAsync();

    return Ok({entity}.Id);
}
```

**✅ After** (Logic in correct layers):
```csharp
// Step 1: Create Command in Application layer
// File: {Project}.Application/Features/{Entities}/Requests/Commands/Create{Entity}Command.cs
namespace {Project}.Application.Features.{Entities}.Requests.Commands;

using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;
using MediatR;

public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; } = null!;
}

// Step 2: Create Validator in Application layer
// File: {Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
namespace {Project}.Application.DTOs.{Entity}.Validators;

using FluentValidation;
using {Project}.Application.Contracts.Persistence;

public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly I{RelatedEntity3}Repository _{relatedEntity3}Repository;
    private readonly I{RelatedEntity4}Repository _{relatedEntity4}Repository;

    public Create{Entity}DtoValidator(
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository,
        I{RelatedEntity3}Repository {relatedEntity3}Repository,
        I{RelatedEntity4}Repository {relatedEntity4}Repository)
    {
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _{relatedEntity3}Repository = {relatedEntity3}Repository;
        _{relatedEntity4}Repository = {relatedEntity4}Repository;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.{LookupEntity}Id)
            .NotEmpty().WithMessage("{LookupEntity} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{lookupEntity}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{LookupEntity} not found");

        RuleFor(x => x.{RelatedEntity2}Id)
            .NotEmpty().WithMessage("{RelatedEntity2} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity2}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity2} not found");

        RuleFor(x => x.{RelatedEntity1}Id)
            .NotEmpty().WithMessage("{RelatedEntity1} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity1}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity1} not found");

        RuleFor(x => x.{RelatedEntity3}Id)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _{relatedEntity3}Repository.Exists(id.Value);
            })
            .When(x => x.{RelatedEntity3}Id.HasValue)
            .WithMessage("{RelatedEntity3} does not exist.");

        RuleFor(x => x.{RelatedEntity4}Id)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _{relatedEntity4}Repository.Exists(id.Value);
            })
            .When(x => x.{RelatedEntity4}Id.HasValue)
            .WithMessage("{RelatedEntity4} does not exist.");
    }
}

// Step 3: Create Handler in Application layer
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity}.Validators;
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Responses;
using {Project}.Domain;
using MediatR;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly I{RelatedEntity3}Repository _{relatedEntity3}Repository;
    private readonly I{RelatedEntity4}Repository _{relatedEntity4}Repository;
    private readonly IMapper _mapper;

    public Create{Entity}CommandHandler(
        I{Entity}Repository {entity}Repository,
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository,
        I{RelatedEntity3}Repository {relatedEntity3}Repository,
        I{RelatedEntity4}Repository {relatedEntity4}Repository,
        IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _{relatedEntity3}Repository = {relatedEntity3}Repository;
        _{relatedEntity4}Repository = {relatedEntity4}Repository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // ✅ Validate using FluentValidation - instantiated manually with dependencies
        var validator = new Create{Entity}DtoValidator(
            _{relatedEntity1}Repository,
            _{relatedEntity2}Repository,
            _{lookupEntity}Repository,
            _{relatedEntity3}Repository,
            _{relatedEntity4}Repository);

        var validationResult = await validator.ValidateAsync(request.{Entity}Dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "{Entity} creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // ✅ Map DTO to Entity
        var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);
        {entity}.ViewCount = 0;

        // ✅ Save through repository
        {entity} = await _{entity}Repository.Create({entity});

        response.Success = true;
        response.Id = {entity}.Id;
        response.Message = "{Entity} created successfully.";

        return response;
    }
}

// Step 4: Thin Controller in API layer
// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Entity}Controller(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
    {
        var command = new Create{Entity}Command { {Entity}Dto = dto };
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
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
using {Project}.Infrastructure.Email;  // ❌ VIOLATION!
using SendGrid;  // ❌ Infrastructure concern

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly SendGridEmailService _emailService;  // ❌ Concrete class

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        // ... create {entity}

        await _emailService.SendAsync(email);  // ❌ Tightly coupled

        return response;
    }
}
```

**✅ After** (Dependency Inversion):
```csharp
// Step 1: Define interface in Application layer
// File: {Project}.Application/Contracts/Infrastructure/IEmailService.cs
namespace {Project}.Application.Contracts.Infrastructure;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IEmailService
{
    Task Send{Entity}CreatedNotificationAsync(
        {IdType} {entity}Id,
        string {entity}Title,
        string ownerEmail,
        CancellationToken cancellationToken = default);
}

// Step 2: Use interface in Application layer
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Infrastructure;  // ✅ Same layer interface
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity}.Validators;
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Responses;
using {Project}.Domain;
using MediatR;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IEmailService _emailService;  // ✅ Interface
    private readonly IMapper _mapper;
    // ... other dependencies

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // Validation...
        var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);
        {entity}.ViewCount = 0;
        {entity} = await _{entity}Repository.Create({entity});

        // ✅ Calls interface, doesn't know about SendGrid
        await _emailService.Send{Entity}CreatedNotificationAsync(
            {entity}.Id,
            {entity}.Title,
            request.{Entity}Dto.OwnerEmail,
            cancellationToken);

        return response;
    }
}

// Step 3: Implement in Infrastructure layer
// File: {Project}.Infrastructure/Email/SendGridEmailService.cs
namespace {Project}.Infrastructure.Email;

using System;
using System.Threading;
using System.Threading.Tasks;
using {Project}.Application.Contracts.Infrastructure;  // ✅ Implements interface
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

    public async Task Send{Entity}CreatedNotificationAsync(
        {IdType} {entity}Id,
        string {entity}Title,
        string ownerEmail,
        CancellationToken cancellationToken = default)
    {
        var from = new EmailAddress("noreply@example.com", "Application Name");
        var to = new EmailAddress(ownerEmail);
        var subject = $"{Entity} Created: {{entity}Title}";
        var htmlContent = $"<p>Your {entity} <strong>{{entity}Title}</strong> has been created successfully.</p>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

        try
        {
            var response = await _client.SendEmailAsync(msg, cancellationToken);
            _logger.LogInformation("Email sent to {Email} for {Entity} {{EntityId}}", ownerEmail, {entity}Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for {Entity} {{EntityId}}", {entity}Id);
            throw;
        }
    }
}

// Step 4: Register in API (Composition Root)
// File: {Project}.API/Program.cs
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
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
using {Project}.Persistence;  // ❌ VIOLATION!
using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly {DbContext} _context;  // ❌ Concrete class

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        return await _context.{Entities}  // ❌ Direct DbSet access
            .Include(e => e.{LookupEntity})
            .Where(e => e.StatusId == 2)
            .ToListAsync(cancellationToken);
    }
}
```

**✅ After** (Repository pattern):
```csharp
// Step 1: Define repository interface in Application layer
// File: {Project}.Application/Contracts/Persistence/I{Entity}Repository.cs
namespace {Project}.Application.Contracts.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using {Project}.Domain;

public interface I{Entity}Repository : IGenericRepository<{Entity}, {IdType}>
{
    Task<{Entity}?> Get{Entity}WithDetails({IdType} id);
    Task<List<{Entity}>> Get{Entities}WithDetails();
    Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId);
}

// Step 2: Use interface in Application layer
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;  // ✅ Same layer
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;

public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;  // ✅ Abstraction
    private readonly IMapper _mapper;

    public Get{Entity}ListRequestHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();  // ✅ Returns List<{Entity}>
        return _mapper.Map<List<{Entity}ListDto>>({entities});  // ✅ Maps to DTOs
    }
}

// Step 3: Implement repository in Persistence layer
// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
namespace {Project}.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using {Project}.Application.Contracts.Persistence;  // ✅ Implements interface
using {Project}.Domain;
using Microsoft.EntityFrameworkCore;

public class {Entity}Repository : GenericRepository<{Entity}, {IdType}>, I{Entity}Repository
{
    private readonly {DbContext} _dbContext;

    public {Entity}Repository({DbContext} dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{RelatedEntity3})
                .ThenInclude(r => r.{NestedEntity})
            .Include(e => e.Status)
            .Include(e => e.{RelatedEntity4})
            .Include(e => e.{RelatedEntity5})
            .Include(e => e.{RelatedEntity6})
            .Include(e => e.{RelatedEntity7})
            .Include(e => e.{RelatedEntity8})
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{RelatedEntity3})
                .ThenInclude(r => r.{NestedEntity})
            .Include(e => e.Status)
            .Include(e => e.{RelatedEntity4})
            .Include(e => e.{RelatedEntity5})
            .Include(e => e.{RelatedEntity6})
            .Include(e => e.{RelatedEntity7})
            .Include(e => e.{RelatedEntity8})
            .ToListAsync();
    }

    public async Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId)
    {
        {IdType} userGuid;
        var isGuid = {IdType}.TryParse(userId, out userGuid);

        var query = _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{RelatedEntity3})
                .ThenInclude(r => r.{NestedEntity})
            .Include(e => e.{RelatedEntity7})
            .Include(e => e.Status)
            .Include(e => e.{RelatedEntity4})
            .Include(e => e.{RelatedEntity5})
            .Include(e => e.{RelatedEntity6})
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userGuid && u.{RelatedEntity3}Id == e.{RelatedEntity3}Id) ||
                _dbContext.{ParentEntity}Members.Any(pm =>
                    pm.UserId == userGuid &&
                    _dbContext.{ParentEntities}.Any(p => p.Id == pm.{ParentEntity}Id && p.{RelatedEntity3}Id == e.{RelatedEntity3}Id)));
        }

        return await query.ToListAsync();
    }
}

// Step 4: Register in API (Composition Root)
// File: {Project}.API/Program.cs
builder.Services.AddScoped<I{Entity}Repository, {Entity}Repository>();
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
// File: {Project}.Domain/{Entity}.cs
using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class {Entity}
{
    [Required]  // ❌ Presentation concern
    [MaxLength(500)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Arbitrary validation rule
    public int? MaxAttendees { get; set; }
}
```

**✅ After** (Proper separation):
```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;

public class {Entity}
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int ViewCount { get; set; }

    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; } = null!;

    [ForeignKey("{RelatedEntity1}")]
    public {LookupIdType} {RelatedEntity1}Id { get; set; }
    public {RelatedEntity1} {RelatedEntity1} { get; set; } = null!;

    // ✅ No validation annotations - domain is pure
}

// File: {Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
namespace {Project}.Application.DTOs.{Entity}.Validators;

using FluentValidation;
using {Project}.Application.Contracts.Persistence;

// ✅ INPUT VALIDATION: Can vary by use case
public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;

    public Create{Entity}DtoValidator(
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository)
    {
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.{LookupEntity}Id)
            .NotEmpty().WithMessage("{LookupEntity} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{lookupEntity}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{LookupEntity} not found");
    }
}

// File: {Project}.Application/DTOs/{Entity}/Validators/Update{Entity}DtoValidator.cs
// ✅ UPDATE validation can be different (e.g., allow partial updates)
public class Update{Entity}DtoValidator : AbstractValidator<Update{Entity}Dto>
{
    public Update{Entity}DtoValidator()
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
| Example | "{Entity} must have {RelatedEntity}" | "Title required for CREATE, optional for UPDATE" |

---

## Pattern #5: Sharing Code Between Layers (DTOs)

### Scenario: Sharing DTOs Between API and Blazor

**❌ Before** (DTOs in wrong layer):
```csharp
// File: {Project}.API/Models/{Entity}Dto.cs
namespace {Project}.API.Models;  // ❌ API-specific

public class {Entity}Dto
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// File: {Project}.Blazor.Client/Models/{Entity}Dto.cs
namespace {Project}.Blazor.Client.Models;  // ❌ Duplicated!

public class {Entity}Dto  // ❌ Same DTO copied
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

**✅ After** (DTOs in Application layer):
```csharp
// File: {Project}.Application/DTOs/{Entity}/{Entity}ListDto.cs
namespace {Project}.Application.DTOs.{Entity};

using System;

// ✅ Shared DTO in Application layer
public class {Entity}ListDto
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string {LookupEntity}Name { get; set; } = string.Empty;
    public string {RelatedEntity1}Name { get; set; } = string.Empty;
    public string {RelatedEntity2}Name { get; set; } = string.Empty;
}

// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

using System.Collections.Generic;
using System.Threading.Tasks;
using {Project}.Application.DTOs.{Entity};  // ✅ References Application DTOs
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Entity}Controller(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
    {
        var {entities} = await _mediator.Send(new Get{Entity}ListRequest());
        return Ok({entities});
    }
}

// File: {Project}.Blazor.Client/Pages/{Entities}List.razor
@using {Project}.Application.DTOs.{Entity}  @* ✅ References same DTOs *@
@inject HttpClient Http

@code {
    private List<{Entity}ListDto>? _{entities};

    protected override async Task OnInitializedAsync()
    {
        _{entities} = await Http.GetFromJsonAsync<List<{Entity}ListDto>>("/api/{entity}");
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

**Example from {Project}.API/Program.cs**:
```csharp
// File: {Project}.API/Program.cs
using {Project}.Application;
using {Project}.Infrastructure;
using {Project}.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Application layer services
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly));
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ✅ Register Persistence layer services (DbContext, Repositories)
builder.Services.AddDbContext<{DbContext}>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<I{Entity}Repository, {Entity}Repository>();
builder.Services.AddScoped<I{RelatedEntity3}Repository, {RelatedEntity3}Repository>();
builder.Services.AddScoped<I{ParentEntity}Repository, {ParentEntity}Repository>();

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
