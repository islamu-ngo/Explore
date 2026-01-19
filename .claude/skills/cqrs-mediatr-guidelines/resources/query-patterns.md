# Query Patterns - Project Conventions

> **Project-Agnostic Query Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Query Conventions

Queries in this project follow a specific pattern with `Request` suffix (not `Query`) and return DTOs directly.

## File Organization

```
{Project}.Application/Features/{Entities}/
├── Requests/Queries/
│   ├── Get{Entity}ListRequest.cs
│   ├── Get{Entity}DetailsRequest.cs
│   └── GetMy{Entities}Request.cs
└── Handlers/Queries/
    ├── Get{Entity}ListRequestHandler.cs
    ├── Get{Entity}DetailsRequestHandler.cs
    └── GetMy{Entities}RequestHandler.cs

{Project}.Application/DTOs/{Entity}/
├── {Entity}ListDto.cs          # For list views
├── {Entity}Dto.cs              # For detail views
└── (other DTOs)
```

## List Query Pattern

### 1. Query Request

**Example**:
```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Queries/Get{Entity}ListRequest.cs
namespace {Project}.Application.Features.{Entities}.Requests.Queries;

using System.Collections.Generic;
using {Project}.Application.DTOs.{Entity};
using MediatR;

public class Get{Entity}ListRequest : IRequest<List<{Entity}ListDto>>
{
}
```

**Key Conventions**:
- ✅ Class name ends with `Request` (not `Query`)
- ✅ Implements `IRequest<List<TDto>>`
- ✅ Returns `List<{Entity}ListDto>` directly (no wrapper)
- ✅ Can include filter properties if needed

### 2. Query Handler

**Example**:
```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;

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
        // ✅ Repository returns List<{Entity}> (entities)
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();

        // ✅ AutoMapper maps entities → DTOs
        return _mapper.Map<List<{Entity}ListDto>>({entities});
    }
}
```

**Key Patterns**:
- ✅ Uses `I{Entity}Repository`, not DbContext
- ✅ Repository method returns `List<{Entity}>` (entities)
- ✅ AutoMapper maps entities to DTOs
- ✅ Returns DTOs directly, no `BaseCommandResponse` wrapper

### 3. List DTO

**Example**:
```csharp
// File: {Project}.Application/DTOs/{Entity}/{Entity}ListDto.cs
namespace {Project}.Application.DTOs.{Entity};

using System;

public class {Entity}ListDto
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string {LookupEntity}Name { get; set; } = string.Empty;
    public string {RelatedEntity1}Name { get; set; } = string.Empty;
    public string {RelatedEntity2}Name { get; set; } = string.Empty;
}
```

**Conventions**:
- ✅ Minimal properties for list views
- ✅ Include navigation property names (e.g., `{LookupEntity}Name`, not just `{LookupEntity}Id`)
- ✅ Suffix with `ListDto`

## Details Query Pattern

### 1. Query Request

**Example**:
```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Queries/Get{Entity}DetailsRequest.cs
namespace {Project}.Application.Features.{Entities}.Requests.Queries;

using System;
using {Project}.Application.DTOs.{Entity};
using MediatR;

public class Get{Entity}DetailsRequest : IRequest<{Entity}Dto?>
{
    public {IdType} Id { get; set; }
}
```

### 2. Query Handler

**Example**:
```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}DetailsRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;

public class Get{Entity}DetailsRequestHandler : IRequestHandler<Get{Entity}DetailsRequest, {Entity}Dto?>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public Get{Entity}DetailsRequestHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    public async Task<{Entity}Dto?> Handle(Get{Entity}DetailsRequest request, CancellationToken cancellationToken)
    {
        // ✅ Repository returns {Entity}? (nullable entity)
        var {entity} = await _{entity}Repository.Get{Entity}WithDetails(request.Id);

        // ✅ Return null if not found (controller handles NotFound)
        if ({entity} == null)
        {
            return null;
        }

        // ✅ Map entity → DTO
        return _mapper.Map<{Entity}Dto>({entity});
    }
}
```

### 3. Details DTO

**Example**:
```csharp
// File: {Project}.Application/DTOs/{Entity}/{Entity}Dto.cs
namespace {Project}.Application.DTOs.{Entity};

using System;

public class {Entity}Dto
{
    public {IdType} Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }

    public {LookupIdType} {LookupEntity}Id { get; set; }
    public string {LookupEntity}FullName { get; set; } = string.Empty;
    public string {LookupEntity}MasterCode { get; set; } = string.Empty;

    public {LookupIdType} {RelatedEntity1}Id { get; set; }
    public string {RelatedEntity1}FullName { get; set; } = string.Empty;
    public string {RelatedEntity1}MasterCode { get; set; } = string.Empty;

    public {LookupIdType} {RelatedEntity2}Id { get; set; }
    public string {RelatedEntity2}FullName { get; set; } = string.Empty;
    public string {RelatedEntity2}MasterCode { get; set; } = string.Empty;
    public int? {RelatedEntity2}MinValue { get; set; }
    public int? {RelatedEntity2}MaxValue { get; set; }

    public {IdType} OwnerId { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string? OwnerHandle { get; set; }
    public int OwnerTypeId { get; set; }
    public string OwnerTypeFullName { get; set; } = string.Empty;
    public {IdType}? OwnerProfilePictureId { get; set; }
    public string? OwnerProfilePictureUri { get; set; }

    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    public {IdType}? FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    public bool IsActive { get; set; }
    public string? ExternalUrl { get; set; }

    public {LookupIdType} StatusId { get; set; }
    public string StatusFullName { get; set; } = string.Empty;
    public string StatusMasterCode { get; set; } = string.Empty;

    public {LookupIdType} VisibilityTypeId { get; set; }
    public string VisibilityTypeFullName { get; set; } = string.Empty;
    public string VisibilityTypeMasterCode { get; set; } = string.Empty;

    public {LookupIdType} FormatId { get; set; }
    public string FormatFullName { get; set; } = string.Empty;
    public string FormatMasterCode { get; set; } = string.Empty;

    public {LookupIdType}? OptionalLookupId { get; set; }
    public string? OptionalLookupFullName { get; set; }
    public string? OptionalLookupMasterCode { get; set; }

    public int? ChildCount { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Timezone { get; set; }

    public int ViewCount { get; set; }

    public {IdType} TenantId { get; set; }

    public {IdType}? ExternalRecordId { get; set; }
    public string? ExternalRecordUri { get; set; }
    public string? ExternalRecordCid { get; set; }
}
```

**Conventions**:
- ✅ All entity properties
- ✅ Include both IDs and Names for foreign keys
- ✅ Suffix with `Dto` (not `DetailsDto`)

## User-Specific Query Pattern

**Example**:
```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Queries/GetMy{Entities}Request.cs
namespace {Project}.Application.Features.{Entities}.Requests.Queries;

using System.Collections.Generic;
using {Project}.Application.DTOs.{Entity};
using MediatR;

public class GetMy{Entities}Request : IRequest<List<{Entity}ListDto>>
{
    public string UserId { get; set; } = string.Empty;
}

// File: {Project}.Application/Features/{Entities}/Handlers/Queries/GetMy{Entities}RequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;

public class GetMy{Entities}RequestHandler : IRequestHandler<GetMy{Entities}Request, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public GetMy{Entities}RequestHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    public async Task<List<{Entity}ListDto>> Handle(GetMy{Entities}Request request, CancellationToken cancellationToken)
    {
        // ✅ Repository handles complex filtering
        var {entities} = await _{entity}Repository.GetMy{Entities}WithDetails(request.UserId);

        // ✅ Map to DTOs
        return _mapper.Map<List<{Entity}ListDto>>({entities});
    }
}
```

## Repository Pattern for Queries

**Example**:
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
    Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId);
}

// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
namespace {Project}.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.Owner)
                .ThenInclude(o => o.OwnerType)
            .Include(e => e.Status)
            .Include(e => e.VisibilityType)
            .Include(e => e.Format)
            .Include(e => e.OptionalLookup)
            .Include(e => e.FeaturedImage)
            .Include(e => e.ExternalRecord)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.Owner)
                .ThenInclude(o => o.OwnerType)
            .Include(e => e.Status)
            .Include(e => e.VisibilityType)
            .Include(e => e.Format)
            .Include(e => e.OptionalLookup)
            .Include(e => e.FeaturedImage)
            .ToListAsync();
    }

    public async Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId)
    {
        // Current implementation supports GUID user ids and checks both:
        // - direct user owner ownership
        // - organization membership ownership
        {IdType} userGuid;
        var isGuid = {IdType}.TryParse(userId, out userGuid);

        var query = _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.Owner)
                .ThenInclude(o => o.OwnerType)
            .Include(e => e.FeaturedImage)
            .Include(e => e.Status)
            .Include(e => e.VisibilityType)
            .Include(e => e.Format)
            .Include(e => e.OptionalLookup)
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userGuid && u.OwnerId == e.OwnerId) ||
                _dbContext.{ParentEntity}Members.Any(pm =>
                    pm.UserId == userGuid &&
                    _dbContext.{ParentEntities}.Any(p => p.Id == pm.{ParentEntity}Id && p.OwnerId == e.OwnerId)));
        }

        return await query.ToListAsync();
    }
}
```

**Critical Patterns**:
- ✅ Repository returns `List<{Entity}>` (entities), not DTOs
- ✅ Use `.Include()` and `.ThenInclude()` for eager loading
- ✅ Complex filtering happens in repository
- ✅ Handler maps entities → DTOs

## AutoMapper Configuration

**Example**:
```csharp
// File: {Project}.Application/Profiles/MappingProfile.cs
namespace {Project}.Application.Profiles;

using AutoMapper;
using {Project}.Application.DTOs.{Entity};
using {Project}.Domain;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // {Entity} → {Entity}ListDto (AutoMapper infers navigation properties)
        CreateMap<{Entity}, {Entity}ListDto>();

        // {Entity} → {Entity}Dto (AutoMapper infers navigation properties)
        CreateMap<{Entity}, {Entity}Dto>();

        // Create{Entity}Dto → {Entity}
        CreateMap<Create{Entity}Dto, {Entity}>();

        // Update{Entity}Dto → {Entity}
        CreateMap<Update{Entity}Dto, {Entity}>();
    }
}
```

**AutoMapper Note**:

This codebase uses explicit `.ForMember(...)` mappings for many "*FullName" / "*MasterCode" / owner fields.

## Naming Conventions

| Pattern | Example | Returns |
|---------|---------|---------|
| Get{Entity}ListRequest | Get{Entity}ListRequest | `List<{Entity}ListDto>` |
| Get{Entity}DetailsRequest | Get{Entity}DetailsRequest | `{Entity}Dto?` |
| GetMy{Entities}Request | GetMy{Entities}Request | `List<{Entity}ListDto>` |
| Check{Condition}Query | CheckUserExistsQuery | `bool` |

**Note**: Most queries use `Request` suffix. Some newer ones use `Query` suffix (both patterns exist).

## Controller Usage

**Example**:
```csharp
// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public {Entity}Controller(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    // ✅ Public read access
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
    {
        var {entities} = await _mediator.Send(new Get{Entity}ListRequest());
        return Ok({entities});
    }

    // ✅ Public read access for single item
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
    {
        var {entity} = await _mediator.Send(new Get{Entity}DetailsRequest { Id = id });

        if ({entity} == null)
            return NotFound();

        return Ok({entity});
    }

    // ✅ User-specific query (authenticated)
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetMy{Entities}()
    {
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

**Key Points**:
- ✅ GET endpoints use `[AllowAnonymous]` for public read
- ✅ Return `null` for not found, handle in controller with `NotFound()`
- ✅ Simple `return Ok(result)` - no wrapper needed
- ✅ Extract userId from JWT claims for user-specific queries

---

**Next**: See [command-patterns.md](command-patterns.md) for write operations and [handler-patterns.md](handler-patterns.md) for common handler patterns.
