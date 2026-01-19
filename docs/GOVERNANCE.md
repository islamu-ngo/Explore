# Code Conventions & Governance

> **Project-Agnostic .NET Clean Architecture Standards**
>
> This document defines coding conventions and architectural patterns for .NET Clean Architecture projects.
> All placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md) for substitutions.

**Last Updated**: January 2026

---

## Table of Contents

1. [Critical Rules](#critical-rules)
2. [Design Principles](#design-principles)
3. [C# Style Guide](#c-style-guide)
4. [CQRS Pattern with MediatR](#cqrs-pattern-with-mediatr)
5. [Repository Pattern](#repository-pattern)
6. [DTO Pattern](#dto-pattern)
7. [Validation Pattern](#validation-pattern)
8. [Controller Pattern](#controller-pattern)
9. [AutoMapper Configuration](#automapper-configuration)

---

## Critical Rules

> These 10 rules are **non-negotiable**. Violations will break the architecture.

### 1. Repositories Return ENTITIES, Never DTOs

```csharp
// ❌ WRONG - Repository returns DTOs
public interface I{Entity}Repository
{
    Task<List<{Entity}ListDto>> Get{Entities}WithDetails();  // WRONG
}

// ✅ CORRECT - Repository returns entities
public interface I{Entity}Repository
{
    Task<List<{Entity}>> Get{Entities}WithDetails();  // CORRECT
}
```

**Why (SOLID - Single Responsibility)**: Repositories handle data access only. DTO mapping is the handler's responsibility.

---

### 2. Validators Use Manual Instantiation (NOT DI)

```csharp
// ❌ WRONG - DI injection
public Create{Entity}CommandHandler(IValidator<Create{Entity}Dto> validator) { }

// ✅ CORRECT - Manual instantiation in handler
var validator = new Create{Entity}DtoValidator(_{relatedEntity}Repository, _{lookupEntity}Repository);
var result = await validator.ValidateAsync(request.{Entity}Dto);
```

**Why**: Fine-grained control over validator dependencies; avoids DI complexity; consistent pattern.

---

### 3. Navigation Properties on Link Tables Are Readonly

```csharp
// ❌ WRONG - Write through navigation
{parentEntity}.{ChildEntities}.Add({childEntity});
await _dbContext.SaveChangesAsync();

// ✅ CORRECT - Write through repository
var {linkEntity} = new {LinkEntity} { {ParentEntity}Id = id, {ChildEntity}Id = childId };
await _{linkEntity}Repository.Create({linkEntity});
```

**Why (DRY)**: Centralized write logic in repositories; prevents tenant isolation leaks; explicit is better than implicit.

---

### 4. Use int Instead of long (Except Size/Cursor)

```csharp
// ❌ WRONG
public long Id { get; set; }

// ✅ CORRECT
public int Id { get; set; }      // For lookup tables
public Guid Id { get; set; }     // For main entities
public long Size { get; set; }   // OK for file sizes
public long Cursor { get; set; } // OK for pagination cursors
```

**Why**: Consistent conventions; `int` sufficient for most scenarios; `Guid` for distributed uniqueness.

---

### 5. No Default Values in Entity Properties

```csharp
// ❌ WRONG - Default in entity
public class {Entity}
{
    public int ViewCount { get; set; } = 0;  // WRONG
}

// ✅ CORRECT - Set in handler or database
public class {Entity}
{
    public int ViewCount { get; set; }  // Set in handler
}

// In handler:
var entity = _mapper.Map<{Entity}>(dto);
entity.ViewCount = 0;  // Explicit initialization
```

**Why (Clean Code)**: Explicit initialization; clear origin of values; avoids confusion between code and database defaults.

---

### 6. Do Not Remove Using Statements

```csharp
// ✅ KEEP all using statements even if they appear unused
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using FluentValidation;
// ... keep all
```

**Why**: Build system may need them; prevents hard-to-debug issues; IDE can optimize later.

---

### 7. Commands Return BaseCommandResponse<{IdType}>

```csharp
// ❌ WRONG - Returns raw type
public class Create{Entity}Command : IRequest<Guid>

// ✅ CORRECT - Returns wrapped response
public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
```

**Response Structure**:
```csharp
public class BaseCommandResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Id { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

**Why**: Consistent error handling; structured validation messages; clear success/failure distinction.

---

### 8. GET = AllowAnonymous, Write = Authorize

```csharp
[HttpGet]
[AllowAnonymous]  // ✅ Public read access
public async Task<ActionResult<List<{Entity}ListDto>>> GetAll() { }

[HttpPost]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create(...) { }

[HttpPut("{id}")]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Update(...) { }

[HttpDelete("{id}")]
[Authorize]  // ✅ Authenticated write access
public async Task<ActionResult> Delete({IdType} id) { }
```

**Why (Security)**: Public discovery; protected writes; standard REST security pattern.

---

### 9. Extract UserId with Fallback Pattern

```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst(
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

**Claim Priority**:
1. `sub` - Standard OIDC subject claim (preferred)
2. `nameidentifier` - Legacy JWT claim (fallback)
3. `sid` - Session ID (last resort)

**Why**: Different auth providers use different claim names; ensures compatibility.

---

### 10. File-Scoped Namespaces

```csharp
// ❌ WRONG - Nested namespace
namespace {Project}.Application.Features.{Entities}.Handlers.Commands
{
    public class Create{Entity}CommandHandler { }
}

// ✅ CORRECT - File-scoped namespace
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

public class Create{Entity}CommandHandler { }
```

**Why (Clean Code)**: C# 10+ convention; reduces nesting; cleaner code.

---

## Design Principles

### SOLID Principles Application

| Principle | Application in This Architecture |
|-----------|----------------------------------|
| **S**ingle Responsibility | Each handler handles ONE command/query; each repository handles ONE entity |
| **O**pen/Closed | Use interfaces for extension; add new handlers without modifying existing |
| **L**iskov Substitution | All repository implementations must fully implement their interfaces |
| **I**nterface Segregation | Small, focused interfaces (`I{Entity}Repository` not `IRepository`) |
| **D**ependency Inversion | Application depends on interfaces; Infrastructure implements them |

### Clean Architecture Rules

| Layer | Dependencies | Contains |
|-------|--------------|----------|
| **Domain** | NONE | Entities, Enums, Value Objects |
| **Application** | Domain only | Commands, Queries, Handlers, DTOs, Interfaces |
| **Infrastructure** | Application, Domain | Repositories, DbContext, External Services |
| **Presentation** | Application, Infrastructure | Controllers, Pages, DI Registration |

### Design Patterns Used

| Pattern | Implementation |
|---------|----------------|
| **Repository** | Data access abstraction via `I{Entity}Repository` |
| **CQRS** | Separate Commands (write) and Queries (read) |
| **Mediator** | MediatR decouples controllers from handlers |
| **Factory** | AutoMapper for entity/DTO creation |
| **Strategy** | Validators with injectable dependencies |

---

## C# Style Guide

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Public members | PascalCase | `public string Title { get; set; }` |
| Private fields | _camelCase | `private readonly I{Entity}Repository _{entity}Repository;` |
| Parameters | camelCase | `public void Method(string {entity}Name)` |
| Constants | PascalCase | `public const string DefaultValue = "...";` |
| Interfaces | IPascalCase | `public interface I{Entity}Repository` |

### Code Organization

```csharp
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

// 1. Using statements (grouped)
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;
using MediatR;

using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;
using {Project}.Domain;

// 2. Class declaration
public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    // 3. Private fields (readonly)
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    // 4. Constructor
    public Create{Entity}CommandHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    // 5. Public methods
    public async Task<BaseCommandResponse<{IdType}>> Handle(
        Create{Entity}Command request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }

    // 6. Private methods (if any)
}
```

---

## CQRS Pattern with MediatR

### Architecture Flow

```
HTTP Request → Controller → MediatR → Handler → Repository → Entity → AutoMapper → DTO → HTTP Response
```

### Command Pattern (Write Operations)

**Command Request**:
```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Commands/Create{Entity}Command.cs
namespace {Project}.Application.Features.{Entities}.Requests.Commands;

using MediatR;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Responses;

public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; } = null!;
}
```

**Command Handler**:
```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity}.Validators;
using {Project}.Application.Responses;
using {Project}.Domain;

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

    public async Task<BaseCommandResponse<{IdType}>> Handle(
        Create{Entity}Command request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // CRITICAL: Validator instantiated manually with dependencies
        var validator = new Create{Entity}DtoValidator(_{relatedEntity}Repository);
        var validationResult = await validator.ValidateAsync(request.{Entity}Dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "{Entity} creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to Entity
        var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);

        // Set properties not from DTO
        entity.CreatedAt = DateTime.UtcNow;

        // Save through repository
        entity = await _{entity}Repository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        response.Message = "{Entity} created successfully.";
        return response;
    }
}
```

### Query Pattern (Read Operations)

**Query Request**:
```csharp
// File: {Project}.Application/Features/{Entities}/Requests/Queries/Get{Entity}ListRequest.cs
namespace {Project}.Application.Features.{Entities}.Requests.Queries;

using System.Collections.Generic;
using MediatR;
using {Project}.Application.DTOs.{Entity};

public class Get{Entity}ListRequest : IRequest<List<{Entity}ListDto>>
{
    // Optional filter properties
    public {IdType}? {RelatedEntity}Id { get; set; }
}
```

**Query Handler**:
```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Queries/Get{Entity}ListRequestHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using {Project}.Application.Contracts.Persistence;
using {Project}.Application.DTOs.{Entity};

public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public Get{Entity}ListRequestHandler(I{Entity}Repository {entity}Repository, IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _mapper = mapper;
    }

    public async Task<List<{Entity}ListDto>> Handle(
        Get{Entity}ListRequest request,
        CancellationToken cancellationToken)
    {
        // Repository returns ENTITIES
        var entities = await _{entity}Repository.Get{Entities}WithDetails();

        // Handler maps ENTITIES to DTOs
        return _mapper.Map<List<{Entity}ListDto>>(entities);
    }
}
```

### Folder Structure

```
{Project}.Application/Features/{Entities}/
├── Requests/
│   ├── Commands/
│   │   ├── Create{Entity}Command.cs
│   │   ├── Update{Entity}Command.cs
│   │   └── Delete{Entity}Command.cs
│   └── Queries/
│       ├── Get{Entity}ListRequest.cs
│       ├── Get{Entity}DetailsRequest.cs
│       └── Get{Entities}By{RelatedEntity}Request.cs
└── Handlers/
    ├── Commands/
    │   ├── Create{Entity}CommandHandler.cs
    │   ├── Update{Entity}CommandHandler.cs
    │   └── Delete{Entity}CommandHandler.cs
    └── Queries/
        ├── Get{Entity}ListRequestHandler.cs
        ├── Get{Entity}DetailsRequestHandler.cs
        └── Get{Entities}By{RelatedEntity}RequestHandler.cs

{Project}.Application/DTOs/{Entity}/
├── {Entity}Dto.cs
├── {Entity}ListDto.cs
├── Create{Entity}Dto.cs
├── Update{Entity}Dto.cs
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

---

## Repository Pattern

### Interface Definition (Application Layer)

```csharp
// File: {Project}.Application/Contracts/Persistence/I{Entity}Repository.cs
namespace {Project}.Application.Contracts.Persistence;

using {Project}.Domain;

public interface I{Entity}Repository : IGenericRepository<{Entity}, {IdType}>
{
    Task<List<{Entity}>> Get{Entities}WithDetails();
    Task<{Entity}?> Get{Entity}WithDetails({IdType} id);
    Task<List<{Entity}>> Get{Entities}By{RelatedEntity}({IdType} {relatedEntity}Id);
}
```

### Implementation (Persistence Layer)

```csharp
// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
namespace {Project}.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;

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
            .Include(e => e.{RelatedEntity})
            .Include(e => e.{LookupEntity})
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{RelatedEntity})
            .Include(e => e.{LookupEntity})
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<{Entity}>> Get{Entities}By{RelatedEntity}({IdType} {relatedEntity}Id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Where(e => e.{RelatedEntity}Id == {relatedEntity}Id)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}
```

### Generic Repository Base

```csharp
// File: {Project}.Persistence/Repositories/GenericRepository.cs
namespace {Project}.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using {Project}.Application.Contracts.Persistence;

public class GenericRepository<T, TId> : IGenericRepository<T, TId> where T : class
{
    private readonly {DbContext} _dbContext;

    public GenericRepository({DbContext} dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T?> GetById(TId id) => await _dbContext.Set<T>().FindAsync(id);

    public async Task<List<T>> GetAll() => await _dbContext.Set<T>().ToListAsync();

    public async Task<bool> Exists(TId id) => await _dbContext.Set<T>().FindAsync(id) != null;

    public async Task<T> Create(T entity)
    {
        await _dbContext.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task Update(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }
}
```

---

## DTO Pattern

### Four DTOs per Entity

```
{Project}.Application/DTOs/{Entity}/
├── {Entity}Dto.cs              // Full details with navigation properties
├── {Entity}ListDto.cs          // List view (minimal properties)
├── Create{Entity}Dto.cs        // Create payload (no Id, no TenantId)
├── Update{Entity}Dto.cs        // Update payload (Id required)
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

### DTO Conventions

| DTO Type | Purpose | Key Rules |
|----------|---------|-----------|
| `{Entity}Dto` | Full details | All properties + flattened navigation (e.g., `{RelatedEntity}Name`) |
| `{Entity}ListDto` | List view | Subset of properties for lists |
| `Create{Entity}Dto` | Create payload | No Id, no TenantId (server sets) |
| `Update{Entity}Dto` | Update payload | Id required, updatable properties only |

---

## Validation Pattern

### Validator with Repository Dependencies

```csharp
// File: {Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
namespace {Project}.Application.DTOs.{Entity}.Validators;

using FluentValidation;
using {Project}.Application.Contracts.Persistence;

public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{RelatedEntity}Repository _{relatedEntity}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;

    public Create{Entity}DtoValidator(
        I{RelatedEntity}Repository {relatedEntity}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository)
    {
        _{relatedEntity}Repository = {relatedEntity}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;

        // Required field validation
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        // Foreign key validation
        RuleFor(x => x.{RelatedEntity}Id)
            .NotEmpty().WithMessage("{RelatedEntity} is required")
            .MustAsync(async (id, ct) => await _{relatedEntity}Repository.Exists(id))
            .WithMessage("{RelatedEntity} not found");

        // Optional FK validation
        RuleFor(x => x.{LookupEntity}Id)
            .MustAsync(async (id, ct) =>
            {
                if (!id.HasValue) return true;
                return await _{lookupEntity}Repository.Exists(id.Value);
            })
            .WithMessage("{LookupEntity} not found");
    }
}
```

---

## Controller Pattern

### Standard Controller Structure

```csharp
// File: {Project}.API/Controllers/{Entity}Controller.cs
namespace {Project}.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {Project}.Application.DTOs.{Entity};
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Features.{Entities}.Requests.Queries;
using {Project}.Application.Responses;

[Route("api/v1/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<{Entity}Controller> _logger;

    public {Entity}Controller(IMediator mediator, ILogger<{Entity}Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("Get all {Entities}")]
    [ProducesResponseType(typeof(List<{Entity}ListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
    {
        var result = await _mediator.Send(new Get{Entity}ListRequest());
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EndpointSummary("Get {Entity} by ID")]
    [ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
    {
        var result = await _mediator.Send(new Get{Entity}DetailsRequest { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [EndpointSummary("Create {Entity}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
    {
        var response = await _mediator.Send(new Create{Entity}Command { {Entity}Dto = dto });
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    [EndpointSummary("Update {Entity}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Update({IdType} id, [FromBody] Update{Entity}Dto dto)
    {
        if (id != dto.Id) return BadRequest(new { error = "ID mismatch" });
        var response = await _mediator.Send(new Update{Entity}Command { {Entity}Dto = dto });
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [EndpointSummary("Delete {Entity}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete({IdType} id)
    {
        var result = await _mediator.Send(new Delete{Entity}Command { Id = id });
        if (!result) return NotFound();
        return NoContent();
    }
}
```

---

## AutoMapper Configuration

### Profile Location

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
        // Entity ↔ DTO (bidirectional for queries)
        CreateMap<{Entity}, {Entity}Dto>().ReverseMap();

        // Entity → ListDto (read-only)
        CreateMap<{Entity}, {Entity}ListDto>();

        // CreateDto → Entity (write-only)
        CreateMap<Create{Entity}Dto, {Entity}>();

        // UpdateDto → Entity (write-only)
        CreateMap<Update{Entity}Dto, {Entity}>();

        // Repeat for each entity...
    }
}
```

### Navigation Property Flattening

AutoMapper automatically flattens navigation properties when names match:

```csharp
// Entity
public class {Entity}
{
    public {RelatedEntity} {RelatedEntity} { get; set; }  // Navigation
}

// DTO - AutoMapper finds {RelatedEntity}.Name automatically
public class {Entity}Dto
{
    public string {RelatedEntity}Name { get; set; }  // Flattened from {RelatedEntity}.Name
}
```

---

## Related Documentation

- **[TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md)** - Placeholder substitution guide
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Critical rules summary
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture overview

## Skills Reference

- **`clean-architecture-rules`** - Dependency rules and layer boundaries
- **`cqrs-mediatr-guidelines`** - CQRS patterns with MediatR
- **`dotnet-efcore-guidelines`** - EF Core and repository patterns
