# Common Violations and Error Messages

> **Project-Agnostic Violation Examples**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Violation #1: Domain Referencing Infrastructure

### The Problem

```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

using Microsoft.EntityFrameworkCore;  // ❌ VIOLATION!

public class {Entity}
{
    [Key]  // ❌ Data annotation from EF Core
    public {IdType} Id { get; set; }

    public string Title { get; set; } = string.Empty;
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Domain
File: {Project}.Domain/{Entity}.cs
Violation: Domain layer cannot reference Microsoft.EntityFrameworkCore

REASON:
Domain must be framework-agnostic. EF Core is an infrastructure concern.

FIX:
1. Remove [Key] attribute
2. Use [ForeignKey] for navigation properties only
3. Configure constraints in Persistence layer using Fluent API
```

### The Fix

```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;  // ✅ Only for [ForeignKey]

public class {Entity}
{
    public {IdType} Id { get; set; }  // ✅ Plain C# property
    public string Title { get; set; } = string.Empty;

    // ✅ OK - Specifies relationship metadata
    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; }

    [ForeignKey("{RelatedEntity}")]
    public {IdType} {RelatedEntity}Id { get; set; }
    public {RelatedEntity} {RelatedEntity} { get; set; }
}
```

---

## Violation #2: Application Referencing DbContext Directly

### The Problem

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

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

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
Violation: Application layer cannot reference {Project}.Persistence or Microsoft.EntityFrameworkCore

REASON:
Application should not depend on concrete database implementation.
This makes it impossible to:
- Unit test without a database
- Switch database providers
- Mock data for testing

FIX:
1. Create I{Entity}Repository interface in Application layer
2. Use interface instead of concrete DbContext
3. Implement repository in Persistence layer
```

### The Fix

```csharp
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
}

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

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity})
            .ToListAsync();
    }
}

// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;  // ✅ Interface in same layer
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
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();  // ✅ Returns entities
        return _mapper.Map<List<{Entity}ListDto>>({entities});  // ✅ Maps to DTOs
    }
}

// File: {Project}.API/Program.cs (DI Registration)
builder.Services.AddScoped<I{Entity}Repository, {Entity}Repository>();
```

---

## Violation #3: Application Using ASP.NET Core Types

### The Problem

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

using Microsoft.AspNetCore.Http;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;   // ❌ VIOLATION!

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, ActionResult<{IdType}>>
{
    public async Task<ActionResult<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        // ... create {entity} logic

        return new CreatedAtActionResult("GetById", "{Entity}", new { id = {entity}Id }, {entity}Id);
        // ❌ Returning ASP.NET Core type
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Application
File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
Violation: Application layer cannot reference Microsoft.AspNetCore.*

REASON:
Application should be framework-agnostic. It should work with:
- REST APIs
- gRPC services
- Console applications
- Background jobs
Returning ActionResult ties it to ASP.NET Core.

FIX:
1. Return BaseCommandResponse<{IdType}> (or plain {IdType})
2. Let Controller map to ActionResult
```

### The Fix

```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Commands/Create{Entity}Command.cs
namespace {Project}.Application.Features.{Entities}.Requests.Commands;

using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;
using MediatR;

public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>  // ✅ Framework-agnostic
{
    public Create{Entity}Dto {Entity}Dto { get; set; }
}

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
    private readonly IMapper _mapper;

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // Validation...
        var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);
        {entity} = await _{entity}Repository.Create({entity});

        response.Success = true;
        response.Id = {entity}.Id;
        response.Message = "{Entity} created successfully.";

        return response;  // ✅ Framework-agnostic response
    }
}

// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

using System;
using System.Threading.Tasks;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;  // ✅ OK in API layer

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

        // ✅ Controller handles HTTP-specific concerns
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
```

---

## Violation #4: Domain Using Data Annotations for Validation

### The Problem

```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

using System.ComponentModel.DataAnnotations;  // ❌ VIOLATION!

public class {Entity}
{
    public {IdType} Id { get; set; }

    [Required]  // ❌ Presentation concern
    [MaxLength(500)]  // ❌ Database concern
    public string Title { get; set; } = string.Empty;

    [Range(1, 10000)]  // ❌ Validation belongs in Application
    public int? MaxAttendees { get; set; }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Domain
File: {Project}.Domain/{Entity}.cs
Violation: Domain layer should not use System.ComponentModel.DataAnnotations for validation

REASON:
Data annotations mix concerns:
- [Required], [Range] = Validation (belongs in Application with FluentValidation)
- [MaxLength] = Database constraint (belongs in Persistence)

Validation rules can differ by use case:
- Creating might require Title
- Updating might allow partial updates
Domain should be pure business entities.

FIX:
1. Remove data annotations (except [ForeignKey])
2. Add FluentValidation in Application layer
```

### The Fix

```csharp
// File: {Project}.Domain/{Entity}.cs
namespace {Project}.Domain;

using System;
using System.ComponentModel.DataAnnotations.Schema;

// ✅ NO validation annotations
public class {Entity}
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int TotalViews { get; set; }

    // ✅ Only [ForeignKey] is acceptable
    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; }

    [ForeignKey("{RelatedEntity}")]
    public {IdType} {RelatedEntity}Id { get; set; }
    public {RelatedEntity} {RelatedEntity} { get; set; }
}

// File: {Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
namespace {Project}.Application.DTOs.{Entity}.Validators;

using FluentValidation;
using {Project}.Application.Contracts.Persistence;

// ✅ Application layer validates INPUT
public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly I{RelatedEntity}Repository _{relatedEntity}Repository;

    public Create{Entity}DtoValidator(
        I{LookupEntity}Repository {lookupEntity}Repository,
        I{RelatedEntity}Repository {relatedEntity}Repository)
    {
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _{relatedEntity}Repository = {relatedEntity}Repository;

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

        RuleFor(x => x.{RelatedEntity}Id)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _{relatedEntity}Repository.Exists(id.Value);
            })
            .When(x => x.{RelatedEntity}Id.HasValue)
            .WithMessage("{RelatedEntity} not found");
    }
}
```

---

## Violation #5: Infrastructure Referencing API/Blazor

### The Problem

```csharp
// File: {Project}.Infrastructure/Email/EmailService.cs
namespace {Project}.Infrastructure.Email;

using {Project}.API.Controllers;  // ❌ VIOLATION!
using Microsoft.AspNetCore.Mvc;  // ❌ VIOLATION!

public class EmailService : IEmailService
{
    public async Task Send{Entity}NotificationAsync({IdType} {entity}Id)
    {
        // ❌ Calling controller directly
        var controller = new {Entity}Controller();
        var result = await controller.GetById({entity}Id);

        // ... send email
    }
}
```

**Error Message**:
```
⚠️ CLEAN ARCHITECTURE VIOLATION DETECTED

Layer: Infrastructure
File: {Project}.Infrastructure/Email/EmailService.cs
Violation: Infrastructure layer cannot reference {Project}.API or presentation layers

REASON:
Infrastructure is below Presentation in the architecture.
Controllers should call Infrastructure, not the other way around.

FIX:
1. Pass data as parameters to EmailService
2. Or use Application layer to orchestrate data retrieval
```

### The Fix

```csharp
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
        string recipientEmail,
        CancellationToken cancellationToken = default);
}

// File: {Project}.Infrastructure/Email/EmailService.cs
namespace {Project}.Infrastructure.Email;

using System;
using System.Threading;
using System.Threading.Tasks;
using {Project}.Application.Contracts.Infrastructure;  // ✅ Application interface only
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly ILogger<SendGridEmailService> _logger;

    public async Task Send{Entity}CreatedNotificationAsync(
        {IdType} {entity}Id,
        string {entity}Title,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        // ✅ Uses passed data, doesn't fetch it itself
        var from = new EmailAddress("noreply@example.org", "{Project}");
        var to = new EmailAddress(recipientEmail);
        var subject = $"{Entity} Created: {{entity}Title}";
        var htmlContent = $"<p>Your {entity} <strong>{{entity}Title}</strong> has been created successfully.</p>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

        try
        {
            var response = await _client.SendEmailAsync(msg, cancellationToken);
            _logger.LogInformation("Email sent to {Email} for {entity} {{Entity}Id}", recipientEmail, {entity}Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for {entity} {{Entity}Id}", {entity}Id);
            throw;
        }
    }
}
```

---

## Violation #6: Circular References Between Projects

### The Problem

```xml
<!-- File: {Project}.Application/{Project}.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
  <ProjectReference Include="..\{Project}.Infrastructure\{Project}.Infrastructure.csproj" />
  <!-- ❌ VIOLATION! -->
</ItemGroup>

<!-- File: {Project}.Infrastructure/{Project}.Infrastructure.csproj -->
<ItemGroup>
  <ProjectReference Include="..\{Project}.Application\{Project}.Application.csproj" />
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

### The Fix

```xml
<!-- File: {Project}.Application/{Project}.Application.csproj -->
<ItemGroup>
  <!-- ✅ ONLY reference Domain -->
  <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
</ItemGroup>

<!-- File: {Project}.Infrastructure/{Project}.Infrastructure.csproj -->
<ItemGroup>
  <!-- ✅ References Application and Domain -->
  <ProjectReference Include="..\{Project}.Application\{Project}.Application.csproj" />
  <ProjectReference Include="..\{Project}.Domain\{Project}.Domain.csproj" />
</ItemGroup>

<!-- File: {Project}.API/{Project}.API.csproj -->
<ItemGroup>
  <!-- ✅ API references all (Composition Root) -->
  <ProjectReference Include="..\{Project}.Application\{Project}.Application.csproj" />
  <ProjectReference Include="..\{Project}.Infrastructure\{Project}.Infrastructure.csproj" />
  <ProjectReference Include="..\{Project}.Persistence\{Project}.Persistence.csproj" />
</ItemGroup>
```

---

## Quick Violation Detection Commands

### Search for Domain Violations
```bash
# Find prohibited using statements in Domain
rg "using (Microsoft\.(EntityFrameworkCore|AspNetCore)|{Project}\.(Application|Infrastructure|API|Blazor))" {Project}.Domain/

# Find validation annotations in Domain (except [ForeignKey])
rg "\[Required\]|\[MaxLength\]|\[Range\]|\[StringLength\]" {Project}.Domain/
```

### Search for Application Violations
```bash
# Find prohibited using statements in Application
rg "using (Microsoft\.EntityFrameworkCore|{Project}\.(Infrastructure|Persistence|API|Blazor))" {Project}.Application/

# Find direct DbContext usage
rg "{DbContext}" {Project}.Application/

# Find ASP.NET Core types in Application
rg "ActionResult|IActionResult|HttpContext" {Project}.Application/
```

### Verify Project References
```bash
# Domain should have NO project references
dotnet list {Project}.Domain/{Project}.Domain.csproj reference

# Application should ONLY reference Domain
dotnet list {Project}.Application/{Project}.Application.csproj reference

# Persistence should reference Domain + Application
dotnet list {Project}.Persistence/{Project}.Persistence.csproj reference
```

### Check for circular references
```bash
dotnet build --no-incremental
```

---

**Next**: See [fix-patterns.md](fix-patterns.md) for comprehensive fix strategies.
