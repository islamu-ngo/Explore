# Code Conventions & Governance

## ⚠️ CRITICAL RULES (Must Follow - No Exceptions)

These rules are based on 45+ entity implementations in the dbml-sync project. Violations will break the architecture.

### 1. **Repositories Return ENTITIES, Never DTOs**
```csharp
// ❌ WRONG
Task<List<EventListDto>> GetEventsWithDetails();

// ✅ CORRECT
Task<List<Event>> GetEventsWithDetails();
```

### 2. **Validators Use Manual Instantiation (NOT DI)**
```csharp
// ❌ WRONG - DI injection
public CreateEventCommandHandler(IValidator<CreateEventDto> validator) { }

// ✅ CORRECT - Manual instantiation
var validator = new CreateEventDtoValidator(_{Entity}Repository);
```

### 3. **Navigation Properties on Link Tables Are Readonly**
```csharp
// ❌ WRONG - Write through navigation
org.Members.Add(member);

// ✅ CORRECT - Write through repository
await _organizationMemberRepository.Create(member);
```

### 4. **Use int Instead of long** (except size/cursor and absolutly neccesery fields)
```csharp
// ❌ WRONG
public long Id { get; set; }

// ✅ CORRECT
public int Id { get; set; }  // or Guid for main entities
```

### 5. **No Default Values in Entities**
```csharp
// ❌ WRONG
public int TotalViews { get; set; } = 0;

// ✅ CORRECT
public int TotalViews { get; set; }  // Set in handler or database
```

### 6. **Do Not Remove Using Statements**
```csharp
// ✅ KEEP all using statements even if they appear unused
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ... etc
```

### 7. **Commands Return BaseCommandResponse<Guid>**
```csharp
// ❌ WRONG
public class CreateEventCommand : IRequest<Guid>

// ✅ CORRECT
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
```

### 8. **GET Endpoints Are AllowAnonymous, Write Endpoints Are Authorize**
```csharp
[HttpGet]
[AllowAnonymous]  // ✅ Public read access

[HttpPost]
[Authorize]  // ✅ Authenticated write access
```

### 9. **Extract UserId with Fallback Pattern**
```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
```

### 10. **File-Scoped Namespaces**
```csharp
// ✅ CORRECT
namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler { }
```

---

## C# Style Guide
- **Naming**: PascalCase for public members, _camelCase for private fields
- **File-scoped namespaces**: Use file-scoped namespace declarations
- **Always use `int` instead of `long`** unless absolutely necessary (only for size/cursor fields)

## CQRS Pattern with MediatR

### Architecture Flow

```
Controller → MediatR → Handler → Repository → Entity → AutoMapper → DTO
```

### Command Pattern (Write Operations)

**Command Request**:
```csharp
// File: Explore.Application/Features/{Entity}s/Requests/Commands/Create{Entity}Command.cs
namespace Explore.Application.Features.{Entity}s.Requests.Commands;

using MediatR;
using Explore.Application.DTOs.{Entity};

public class Create{Entity}Command : IRequest<BaseCommandResponse<Guid>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; }
}
```

**Command Handler**:
```csharp
// File: Explore.Application/Features/{Entity}s/Handlers/Commands/Create{Entity}CommandHandler.cs
using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.{Entity}.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.{Entity}s.Handlers.Commands;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<Guid>>
{
    private readonly I{Entity}Repository _{Entity}Repository;
    private readonly IMapper _mapper;

    public Create{Entity}CommandHandler(
        I{Entity}Repository {Entity}Repository,
        IMapper mapper)
    {
        _{Entity}Repository = {Entity}Repository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validate using FluentValidation - CRITICAL: Validator instantiated manually with dependencies
        var validator = new Create{Entity}DtoValidator(_{RelatedEntity1}Repository, _{RelatedEntity2}Repository, ...);
        var validationResult = await validator.ValidateAsync(request.{Entity}Dto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "{Entity} creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);

        // Save through repository
        entity = await _{Entity}Repository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "{Entity} created successfully.";

        return response;
    }
}
```

**Command Return Type**:
- All commands return `BaseCommandResponse<Guid>` (for Create/Update/Delete)
- Response contains: Success (bool), Message (string), Id (Guid), Errors (List<string>)

### Query Pattern (Read Operations)

**Query Request**:
```csharp
// File: Explore.Application/Features/{Entity}s/Requests/Queries/Get{Entity}ListRequest.cs
namespace Explore.Application.Features.{Entity}s.Requests.Queries;

public class Get{Entity}ListRequest : IRequest<List<{Entity}ListDto>>
{
}
```

**Query Handler**:
```csharp
// File: Explore.Application/Features/{Entity}s/Handlers/Queries/Get{Entity}ListRequestHandler.cs
using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.{Entity};

namespace Explore.Application.Features.{Entity}s.Handlers.Queries;

public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{Entity}Repository;
    private readonly IMapper _mapper;

    public Get{Entity}ListRequestHandler(I{Entity}Repository {Entity}Repository, IMapper mapper)
    {
        _{Entity}Repository = {Entity}Repository;
        _mapper = mapper;
    }

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        // Repository returns ENTITIES
        var entities = await _{Entity}Repository.Get{Entities}WithDetails();

        // AutoMapper maps ENTITIES to DTOs
        return _mapper.Map<List<{Entity}ListDto>>(entities);
    }
}
```

**Query Return Types**:
- GetList: returns `List<{Entity}ListDto>`
- GetDetails: returns `{Entity}Dto` (nullable)
- Custom queries: return appropriate DTO types

### Folder Structure

```
Explore.Application/Features/{Entity}s/
├── Requests/
│   ├── Commands/
│   │   ├── Create{Entity}Command.cs
│   │   ├── Update{Entity}Command.cs
│   │   └── Delete{Entity}Command.cs
│   └── Queries/
│       ├── Get{Entity}ListRequest.cs
│       ├── Get{Entity}DetailsRequest.cs
│       └── Get{Entities}By{RelatedEntity}Request.cs (custom queries)
└── Handlers/
    ├── Commands/
    │   ├── Create{Entity}CommandHandler.cs
    │   ├── Update{Entity}CommandHandler.cs
    │   └── Delete{Entity}CommandHandler.cs
    └── Queries/
        ├── Get{Entity}ListRequestHandler.cs
        ├── Get{Entity}DetailsRequestHandler.cs
        └── Get{Entities}By{RelatedEntity}RequestHandler.cs (custom queries)

Explore.Application/DTOs/{Entity}/
├── {Entity}Dto.cs
├── {Entity}ListDto.cs
├── Create{Entity}Dto.cs
├── Update{Entity}Dto.cs
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

## Repository Pattern

### CRITICAL RULE: Repositories Return ENTITIES, Not DTOs

```csharp
// ❌ WRONG - Repository returns DTOs
public interface IEventRepository
{
    Task<List<EventListDto>> GetEventsWithDetails();  // WRONG
}

// ✅ CORRECT - Repository returns entities
public interface IEventRepository
{
    Task<List<Event>> GetEventsWithDetails();  // CORRECT
}
```

### Implementation Pattern

**Interface** (in Application layer):
```csharp
// File: Explore.Application/Contracts/Persistence/I{Entity}Repository.cs
namespace Explore.Application.Contracts.Persistence;

public interface I{Entity}Repository : IGenericRepository<{Entity}, Guid>
{
    Task<List<{Entity}>> Get{Entities}WithDetails();
    Task<{Entity}?> Get{Entity}WithDetails(Guid id);
    Task<List<{Entity}>> Get{Entities}ByOrganization(Guid organizationId);
}
```

**Implementation** (in Persistence layer):
```csharp
// File: Explore.Persistence/Repositories/{Entity}Repository.cs
namespace Explore.Persistence.Repositories;

public class {Entity}Repository : GenericRepository<{Entity}, Guid>, I{Entity}Repository
{
    private readonly ExploreDbContext _dbContext;

    public {Entity}Repository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .ToListAsync();
    }
}
```

### Navigation Properties on Link Tables

**CRITICAL RULE**: Navigation properties on link/mapping tables are **readonly for queries only**. Writes must go through the link table repository directly.

```csharp
// Example: Organization.Members navigation property
public class Organization
{
    public Guid Id { get; set; }

    // ✅ OK - Readonly navigation for querying
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();

    // ❌ NEVER - Do not add members through this navigation
    // Use OrganizationMemberRepository.Create() instead
}
```

**Correct Usage**:
```csharp
// ✅ CORRECT - Query using navigation (readonly)
var org = await organizationRepository.GetById(orgId);
var members = org.Members;  // Readonly, query only

// ✅ CORRECT - Write using repository
var member = new OrganizationMember { OrganizationId = orgId, UserId = userId };
await organizationMemberRepository.Create(member);  // Use repository for writes
```

## DTO Pattern

### Four DTOs per Entity

```
Explore.Application/DTOs/{Entity}/
├── {Entity}Dto.cs              // Full details with navigation properties
├── {Entity}ListDto.cs          // List view (minimal properties)
├── Create{Entity}Dto.cs        // Create payload (no Id, no TenantId)
├── Update{Entity}Dto.cs        // Update payload (Id required, updatable only)
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

### DTO Conventions

**{Entity}Dto**:
- All properties from entity
- Include navigation property details (e.g., EventTitle, LocationFullName)
- Used for GET /api/v1/{entity}/{id}

**{Entity}ListDto**:
- Subset of properties for lists
- Include navigation property names for display
- Used for GET /api/v1/{entity}

**Create{Entity}Dto**:
- Only properties needed to create
- No Id property (set by system)
- No TenantId (set by server based on JWT claims)
- Used for POST /api/v1/{entity}

**Update{Entity}Dto**:
- Id property required
- Updatable properties only
- Used for PUT /api/v1/{entity}/{id}

## AutoMapper Profiles

### Naming Convention

AutoMapper automatically maps navigation properties when names match. You only need manual mapping when names DON'T match.

```csharp
// ✅ SIMPLE - Names match, auto-mapping works
CreateMap<EventSession, EventSessionDto>()
    .ReverseMap();

// ✅ SIMPLE - Navigation property auto-mapping
// If DTO has "EventTitle" and entity has "Event.Title", AutoMapper figures it out
// ❌ AVOID - Manual mapping not needed for matching names
// .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event.Title))  // NOT NEEDED
// .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location.FullName))  // NOT NEEDED
```

### Profile Location

Add all mappings to:
```
Explore.Application/Profiles/MappingProfile.cs
```

### Standard Mappings

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity ↔ DTO (bidirectional)
        CreateMap<Event, EventDto>()
            .ReverseMap();

        // Entity ↔ ListDto (unidirectional, read-only)
        CreateMap<Event, EventListDto>();

        // CreateDto → Entity (unidirectional, write-only)
        CreateMap<CreateEventDto, Event>();

        // UpdateDto → Entity (unidirectional, write-only)
        CreateMap<UpdateEventDto, Event>();

        // Repeat for each entity...
    }
}
```

## Validation

### CRITICAL PATTERN: Validators Use Manual Instantiation (NOT DI Injection)

**IMPORTANT**: Validators are instantiated in handlers with dependencies passed to constructor. They are NOT injected via DI.

```csharp
// File: Explore.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
using FluentValidation;
using Explore.Application.Contracts.Persistence;

public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{Entity}Repository _{Entity}Repository;
    private readonly I{RelatedEntity1}Repository _{RelatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{RelatedEntity2}Repository;

    public Create{Entity}DtoValidator(
        I{Entity}Repository {Entity}Repository,
        I{RelatedEntity1}Repository {RelatedEntity1}Repository,
        I{RelatedEntity2}Repository {RelatedEntity2}Repository)
    {
        _{Entity}Repository = {Entity}Repository;
        _{RelatedEntity1}Repository = {RelatedEntity1}Repository;
        _{RelatedEntity2}Repository = {RelatedEntity2}Repository;

        // Standard validation rules
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500);

        // Date/time validation
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required")
            .GreaterThan(DateTime.Now).WithMessage("Event must start in future");

        // Foreign key validation with repository
        RuleFor(x => x.{RelatedEntity1}Id)
            .NotEmpty().WithMessage("{RelatedEntity1} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{RelatedEntity1}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity1} not found");

        RuleFor(x => x.{RelatedEntity2}Id)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var exists = await _{RelatedEntity2}Repository.Exists(id.Value);
                return exists;
            })
            .WithMessage("{RelatedEntity2} not found");
    }
}
```

**Real Example from CreateEventCommandHandler:**
```csharp
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // ✅ CORRECT: Validator instantiated manually with all required repositories
    var validator = new CreateEventDtoValidator(
        _audienceAgeRepository, 
        _audienceGenderRepository, 
        _eventTypeRepository, 
        _actorRepository, 
        _storageObjectRepository);
    
    var validationResult = await validator.ValidateAsync(request.EventDto);
    
    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Event creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // Map DTO to Entity
    var @event = _mapper.Map<Event>(request.EventDto);
    @event.TotalViews = 0;

    // Save through repository
    @event = await _eventRepository.Create(@event);

    response.Success = true;
    response.Id = @event.Id;
    response.Message = "Event created successfully.";

    return response;
}
```

### Self-Referencing Entities

For entities with parent relationships (e.g., Category):

```csharp
// Create Category DTO Validator
public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryDtoValidator(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

        // Prevent circular reference
        RuleFor(x => x.ParentId)
            .NotEqual(x => x.Id)
            .When(x => x.ParentId.HasValue)
            .WithMessage("Category cannot be its own parent");

        RuleFor(x => x.ParentId)
            .MustAsync(NotCreateCircularReference)
            .When(x => x.ParentId.HasValue)
            .WithMessage("Would create circular reference");
    }
}
```

## Controller Pattern

### Standard Controller Structure

```csharp
// File: Explore.API/Controllers/{Entity}Controller.cs
using MediatR;
using Explore.Application.Features.{Entity}s.Requests.Commands;
using Explore.Application.Features.{Entity}s.Requests.Queries;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // GET: api/v1/{entity}
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
    {
        var {Entities} = await _mediator.Send(new Get{Entity}ListRequest());
        return Ok({Entities});
    }

    // GET: api/v1/{entity}/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<{Entity}Dto>> GetById(Guid id)
    {
        var {Entity} = await _mediator.Send(new Get{Entity}DetailsRequest { Id = id });
        return Ok({Entity});
    }

    // POST: api/v1/{entity}
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] Create{Entity}Dto dto)
    {
        var command = new Create{Entity}Command { {Entity}Dto = dto };
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // PUT: api/v1/{entity}/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] Update{Entity}Dto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "{Entity} ID mismatch" });
        }

        var command = new Update{Entity}Command { {Entity}Dto = dto };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/{entity}/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        var command = new Delete{Entity}Command { Id = id };
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new { error = "{Entity} not found or you don't have permission to delete it" });
        }

        return NoContent();
    }
}
```

### Authorization Patterns

- **GET endpoints**: `[AllowAnonymous]` - public read access
- **POST/PUT/DELETE**: `[Authorize]` - authenticated write access
- **User extraction**: Always extract userId from JWT claims with fallback order: `sub` → `nameidentifier` → `sid`

```csharp
// Extract userId from JWT claims with fallback
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

## Key Rules Summary

1. **Repositories return ENTITIES, never DTOs** - DTO mapping in handlers via AutoMapper
2. **Navigation properties on link tables are readonly** - writes via repository
3. **Use int instead of long** (except size/cursor)
4. **No default values in entities** - set values in code or database
5. **Validators use manual instantiation** - instantiated with dependencies, NOT DI injected
6. **Commands return BaseCommandResponse<Guid>** - for Create/Update/Delete
7. **Queries return DTOs directly** - no wrapper
8. **GET endpoints are AllowAnonymous** - public read
9. **POST/PUT/DELETE are Authorize** - authenticated write
10. **Extract userId with fallback** - sub → nameidentifier → sid
