# Complete Feature Example - Full CQRS Cycle

> **Project-Agnostic Complete Example**
>
> This document shows the **complete CQRS cycle** for a generic entity, demonstrating all patterns and conventions.
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

---

## Complete CQRS Cycle Overview

```
User Request → Controller → MediatR → Handler → Repository → Entity → Database
                               ↓
                          Validation (manual instantiation)
                               ↓
                          AutoMapper (Entity ↔ DTO)
                               ↓
                          Response (BaseCommandResponse<{IdType}>)
```

---

## 1. Create Entity - Full Command Flow

### Domain Entity

**File**: `{Project}.Domain/{Entity}.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace {Project}.Domain;

public class {Entity}
{
    public {IdType} Id { get; set; }

    [ForeignKey("{LookupEntity}")]
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupEntity} {LookupEntity} { get; set; }

    public string Title { get; set; }
    public string? Description { get; set; }

    [ForeignKey("{RelatedEntity1}")]
    public {LookupIdType} {RelatedEntity1}Id { get; set; }
    public {RelatedEntity1} {RelatedEntity1} { get; set; }

    [ForeignKey("{RelatedEntity2}")]
    public {LookupIdType} {RelatedEntity2}Id { get; set; }
    public {RelatedEntity2} {RelatedEntity2} { get; set; }

    [ForeignKey("{ParentEntity}")]
    public {IdType} {ParentEntity}Id { get; set; }
    public {ParentEntity} {ParentEntity} { get; set; }

    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    [ForeignKey("FeaturedImage")]
    public {IdType}? FeaturedImageId { get; set; }
    public StorageObject? FeaturedImage { get; set; }

    public int ViewCount { get; set; }
    public bool IsActive { get; set; }
    public string? ExternalUrl { get; set; }

    [ForeignKey("Status")]
    public {LookupIdType} StatusId { get; set; }
    public Status Status { get; set; }

    [ForeignKey("Visibility")]
    public {LookupIdType} VisibilityId { get; set; }
    public Visibility Visibility { get; set; }

    [ForeignKey("Tenant")]
    public {IdType} TenantId { get; set; }
    public Tenant Tenant { get; set; }

    public string? Slug { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Timezone { get; set; }
}
```

### Create DTO

**File**: `{Project}.Application/DTOs/{Entity}/Create{Entity}Dto.cs`

```csharp
using System;

namespace {Project}.Application.DTOs.{Entity};

public class Create{Entity}Dto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Lookup references
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public {LookupIdType} {RelatedEntity1}Id { get; set; }
    public {LookupIdType} {RelatedEntity2}Id { get; set; }

    // Optional parent reference
    public {IdType}? {ParentEntity}Id { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image (optional)
    public {IdType}? FeaturedImageId { get; set; }

    // Configuration
    public bool IsActive { get; set; }
    public string? ExternalUrl { get; set; }

    // Status & Visibility (DTO defaults are OK; do not add defaults in Domain entities)
    public {LookupIdType} StatusId { get; set; } = 1;
    public {LookupIdType} VisibilityId { get; set; } = 1;

    // Date range
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Timezone { get; set; }

    // TenantId is set by the handler from the authenticated context.
}
```

### Validator (CRITICAL: Manual Instantiation)

**File**: `{Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs`

```csharp
using FluentValidation;
using {Project}.Application.Contracts.Persistence;

namespace {Project}.Application.DTOs.{Entity}.Validators;

public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly I{ParentEntity}Repository _{parentEntity}Repository;
    private readonly IStorageObjectRepository _storageObjectRepository;

    public Create{Entity}DtoValidator(
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository,
        I{ParentEntity}Repository {parentEntity}Repository,
        IStorageObjectRepository storageObjectRepository)
    {
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _{parentEntity}Repository = {parentEntity}Repository;
        _storageObjectRepository = storageObjectRepository;

        // Title validation
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        // Lookup entity validation with async database check
        RuleFor(x => x.{LookupEntity}Id)
            .NotEmpty().WithMessage("{LookupEntity} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{lookupEntity}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{LookupEntity} not found");

        // Related entity 1 validation
        RuleFor(x => x.{RelatedEntity1}Id)
            .NotEmpty().WithMessage("{RelatedEntity1} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity1}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity1} not found");

        // Related entity 2 validation
        RuleFor(x => x.{RelatedEntity2}Id)
            .NotEmpty().WithMessage("{RelatedEntity2} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity2}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity2} not found");

        // Parent entity validation (optional)
        RuleFor(x => x.{ParentEntity}Id)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _{parentEntity}Repository.Exists(id.Value);
            })
            .When(x => x.{ParentEntity}Id.HasValue)
            .WithMessage("{ParentEntity} does not exist.");

        RuleFor(x => x.FeaturedImageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _storageObjectRepository.Exists(id.Value);
            })
            .When(x => x.FeaturedImageId.HasValue)
            .WithMessage("FeaturedImageId does not exist.");

        // Price validation
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).When(x => x.Price.HasValue)
            .WithMessage("Price cannot be negative");

        // Currency validation
        RuleFor(x => x.CurrencyCode)
            .MaximumLength(3)
            .When(x => !string.IsNullOrEmpty(x.CurrencyCode))
            .WithMessage("Currency code must be 3 characters (ISO 4217)");
    }
}
```

### Command Request

**File**: `{Project}.Application/Features/{Entities}/Requests/Commands/Create{Entity}Command.cs`

```csharp
using MediatR;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;

namespace {Project}.Application.Features.{Entities}.Requests.Commands;

public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; }
}
```

### Command Handler (CRITICAL: Shows All Patterns)

**File**: `{Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs`

```csharp
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

namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly I{ParentEntity}Repository _{parentEntity}Repository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public Create{Entity}CommandHandler(
        I{Entity}Repository {entity}Repository,
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository,
        I{ParentEntity}Repository {parentEntity}Repository,
        IStorageObjectRepository storageObjectRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _{parentEntity}Repository = {parentEntity}Repository;
        _storageObjectRepository = storageObjectRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // CRITICAL: Manual validator instantiation with all dependencies
        var validator = new Create{Entity}DtoValidator(
            _{relatedEntity1}Repository,
            _{relatedEntity2}Repository,
            _{lookupEntity}Repository,
            _{parentEntity}Repository,
            _storageObjectRepository);

        var validationResult = await validator.ValidateAsync(request.{Entity}Dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "{Entity} creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity using AutoMapper
        var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);

        // Set properties not in DTO (system-generated values)
        {entity}.ViewCount = 0;
        {entity}.TenantId = _tenantContext.TenantId;

        // Save through repository
        {entity} = await _{entity}Repository.Create({entity});

        response.Success = true;
        response.Id = {entity}.Id;
        response.Message = "{Entity} created successfully.";

        return response;
    }
}
```

### Controller (Complete with Authorization)

**File**: `{Project}.API/Controllers/{Entity}Controller.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Features.{Entities}.Requests.Queries;
using {Project}.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace {Project}.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<{Entity}Controller> _logger;

    public {Entity}Controller(
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<{Entity}Controller> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // POST: api/v1/{entity}
    [HttpPost]
    [EndpointSummary("Create {Entity}")]
    [EndpointDescription("Creates a new {entity}. Requires authentication.")]
    [Authorize]  // CRITICAL: Write operations require authentication
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), Status400BadRequest)]
    [ProducesResponseType(Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
    {
        var command = new Create{Entity}Command { {Entity}Dto = dto };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // GET: api/v1/{entity}
    [HttpGet]
    [EndpointSummary("List {Entities}")]
    [EndpointDescription("Returns a list of {entities}.")]
    [AllowAnonymous]  // CRITICAL: Read operations are public
    [ProducesResponseType(typeof(List<{Entity}ListDto>), Status200OK)]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
    {
        var {entities} = await _mediator.Send(new Get{Entity}ListRequest());
        return Ok({entities});
    }

    // GET: api/v1/{entity}/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get {Entity} Details")]
    [EndpointDescription("Returns {entity} details.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof({Entity}Dto), Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
    {
        var {entity} = await _mediator.Send(new Get{Entity}DetailsRequest { Id = id });

        if ({entity} == null)
        {
            return NotFound(new { error = "{Entity} not found" });
        }

        return Ok({entity});
    }

    // GET: api/v1/{entity}/my
    [HttpGet("my")]
    [EndpointSummary("List My {Entities}")]
    [EndpointDescription("Returns {entities} owned by the current user.")]
    [Authorize]
    [ProducesResponseType(typeof(List<{Entity}ListDto>), Status200OK)]
    [ProducesResponseType(Status401Unauthorized)]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetMy{Entities}()
    {
        // CRITICAL: UserId extraction with fallback pattern
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var {entities} = await _mediator.Send(new GetMy{Entities}Request { UserId = userId });
        return Ok({entities});
    }
}
```

---

## 2. Get Entity List - Full Query Flow

### List DTO (Optimized for Display)

**File**: `{Project}.Application/DTOs/{Entity}/{Entity}ListDto.cs`

```csharp
using System;

namespace {Project}.Application.DTOs.{Entity};

public class {Entity}ListDto
{
    public {IdType} Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Lookup entity
    public {LookupIdType} {LookupEntity}Id { get; set; }
    public string {LookupEntity}FullName { get; set; }

    // Related entities
    public {LookupIdType} {RelatedEntity1}Id { get; set; }
    public string {RelatedEntity1}FullName { get; set; }
    public {LookupIdType} {RelatedEntity2}Id { get; set; }
    public string {RelatedEntity2}FullName { get; set; }

    // Parent entity (Owner)
    public {IdType} {ParentEntity}Id { get; set; }
    public string {ParentEntity}DisplayName { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image
    public {IdType} FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    // Configuration
    public bool IsActive { get; set; }
    public string? ExternalUrl { get; set; }

    // Status & Visibility
    public {LookupIdType} StatusId { get; set; }
    public string StatusFullName { get; set; }
    public {LookupIdType} VisibilityId { get; set; }
    public string VisibilityFullName { get; set; }

    // Date range
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Timezone { get; set; }

    // Metadata
    public int ViewCount { get; set; }

    // Tenant
    public {IdType} TenantId { get; set; }
}
```

### Query Request

**File**: `{Project}.Application/Features/{Entities}/Requests/Queries/Get{Entity}ListRequest.cs`

```csharp
using System.Collections.Generic;
using {Project}.Application.DTOs.{Entity};
using MediatR;

namespace {Project}.Application.Features.{Entities}.Requests.Queries;

public class Get{Entity}ListRequest : IRequest<List<{Entity}ListDto>>
{
    // No parameters - returns all entities
}
```

### Query Handler (Shows Repository → DTO Mapping)

**File**: `{Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;

namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public Get{Entity}ListRequestHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        // CRITICAL: Repository returns ENTITIES (not DTOs)
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();

        // CRITICAL: AutoMapper maps entities to DTOs
        return _mapper.Map<List<{Entity}ListDto>>({entities});
    }
}
```

### Repository Interface

**File**: `{Project}.Application/Contracts/Persistence/I{Entity}Repository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using {Project}.Domain;

namespace {Project}.Application.Contracts.Persistence;

public interface I{Entity}Repository : IGenericRepository<{Entity}, {IdType}>
{
    Task<{Entity}?> Get{Entity}WithDetails({IdType} id);
    Task<List<{Entity}>> Get{Entities}WithDetails();
    Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId);
}
```

### Repository Implementation (Complex Include Chains)

**File**: `{Project}.Persistence/Repositories/{Entity}Repository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;
using Microsoft.EntityFrameworkCore;

namespace {Project}.Persistence.Repositories;

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
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .ToListAsync();
    }

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId)
    {
        {IdType} userIdParsed;
        var isValid = {IdType}.TryParse(userId, out userIdParsed);

        var query = _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .AsQueryable();

        if (isValid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userIdParsed && u.{ParentEntity}Id == e.{ParentEntity}Id));
        }

        return await query.ToListAsync();
    }
}
```

### AutoMapper Profile

**File**: `{Project}.Application/Profiles/MappingProfile.cs` (excerpt)

```csharp
using AutoMapper;
using {Project}.Application.DTOs.{Entity};
using {Project}.Domain;

namespace {Project}.Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // {Entity} mappings
        CreateMap<{Entity}, {Entity}Dto>()
            .ReverseMap();

        CreateMap<{Entity}, {Entity}ListDto>();

        CreateMap<Create{Entity}Dto, {Entity}>();

        CreateMap<Update{Entity}Dto, {Entity}>();

        // ... other entity mappings
    }
}
```

---

## Key Patterns Demonstrated

### 1. **Manual Validator Instantiation** ⚠️
```csharp
// CRITICAL: Validators are instantiated with dependencies in handlers
var validator = new Create{Entity}DtoValidator(
    _{relatedEntity1}Repository,
    _{relatedEntity2}Repository,
    _{lookupEntity}Repository,
    _{parentEntity}Repository,
    _storageObjectRepository);
```

### 2. **Repository Returns Entities, Not DTOs** ⚠️
```csharp
// Repository returns entities
var {entities} = await _{entity}Repository.Get{Entities}WithDetails();

// Handler maps to DTOs using AutoMapper
return _mapper.Map<List<{Entity}ListDto>>({entities});
```

### 3. **BaseCommandResponse<{IdType}> for Commands** ⚠️
```csharp
public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; }
}
```

### 4. **Authorization Pattern** ⚠️
```csharp
[HttpGet]
[AllowAnonymous]  // Public read access

[HttpPost]
[Authorize]  // Authenticated write access
```

### 5. **UserId Extraction with Fallback** ⚠️
```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
```

### 6. **Complex EF Core Include Chains** ⚠️
```csharp
return await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .Include(e => e.{LookupEntity})
    .Include(e => e.FeaturedImage)
    .ToListAsync();
```

---

This complete example shows **every layer** of the CQRS cycle, from domain entity to API controller, with all critical patterns highlighted.
