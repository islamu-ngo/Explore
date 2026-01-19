# Quick Reference - Critical Rules

> **Project-Agnostic .NET Clean Architecture Quick Reference**
>
> Fast lookup for critical rules and patterns. For full details, see [GOVERNANCE.md](GOVERNANCE.md).
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

**Last Updated**: January 2026

---

## 10 Critical Rules (Never Violate)

### 1. Repositories Return ENTITIES, Never DTOs

```csharp
// ❌ WRONG
public interface I{Entity}Repository
{
    Task<List<{Entity}ListDto>> Get{Entities}WithDetails();  // WRONG
}

// ✅ CORRECT
public interface I{Entity}Repository
{
    Task<List<{Entity}>> Get{Entities}WithDetails();  // CORRECT
}

// Handler maps entities to DTOs
public async Task<List<{Entity}ListDto>> Handle(...)
{
    var entities = await _{entity}Repository.Get{Entities}WithDetails();  // Entities
    return _mapper.Map<List<{Entity}ListDto>>(entities);  // DTOs
}
```

**Why (SOLID - Single Responsibility)**: Repositories handle data access; handlers handle mapping.

---

### 2. Validators Use Manual Instantiation (NOT DI)

```csharp
// ❌ WRONG - DI injection
public Create{Entity}CommandHandler(
    I{Entity}Repository {entity}Repository,
    IValidator<Create{Entity}Dto> validator)  // WRONG
{
    _validator = validator;
}

// ✅ CORRECT - Manual instantiation
public async Task<BaseCommandResponse<{IdType}>> Handle(...)
{
    var validator = new Create{Entity}DtoValidator(
        _{relatedEntity}Repository,
        _{lookupEntity}Repository);

    var validationResult = await validator.ValidateAsync(request.{Entity}Dto);
}
```

**Why**: Fine-grained control; avoids DI complexity; consistent pattern.

---

### 3. Navigation Properties on Link Tables Are Readonly

```csharp
// ❌ WRONG - Write through navigation
var {parentEntity} = await _{parentEntity}Repository.GetById(id);
{parentEntity}.{ChildEntities}.Add({childEntity});  // WRONG
await _dbContext.SaveChangesAsync();

// ✅ CORRECT - Write through repository
var {linkEntity} = new {LinkEntity}
{
    {ParentEntity}Id = id,
    {ChildEntity}Id = childId
};
await _{linkEntity}Repository.Create({linkEntity});  // CORRECT
```

**Why**: Tenant isolation; explicit writes; prevents accidental cross-tenant data mixing.

---

### 4. Use int Instead of long (Except Size/Cursor)

```csharp
// ❌ WRONG
public long Id { get; set; }

// ✅ CORRECT
public int Id { get; set; }      // For lookup tables
public Guid Id { get; set; }     // For main entities
public long Size { get; set; }   // OK for file size
public long Cursor { get; set; } // OK for pagination cursor
```

**Why**: Consistent conventions; `int` sufficient for most scenarios.

---

### 5. No Default Values in Entities

```csharp
// ❌ WRONG
public class {Entity}
{
    public int ViewCount { get; set; } = 0;  // WRONG
}

// ✅ CORRECT
public class {Entity}
{
    public int ViewCount { get; set; }  // Set in handler or DB
}

// In handler:
var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);
entity.ViewCount = 0;  // Set here
```

**Why**: Clear initialization; avoids confusion between default and database values.

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
using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;
// ... keep all
```

**Why**: Build system may need them; prevents hard-to-debug issues.

---

### 7. Commands Return BaseCommandResponse<{IdType}>

```csharp
// ❌ WRONG
public class Create{Entity}Command : IRequest<{IdType}>

// ✅ CORRECT
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

**Why**: Consistent error handling; validation messages; success/failure distinction.

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

**Why (Security)**: Public discovery; protected writes; standard REST pattern.

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

**Claim Priority**: `sub` → `nameidentifier` → `sid`

**Why**: Different auth providers use different claim names.

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

## CQRS Pattern Quick Reference

### Command Pattern (Write)

```csharp
// 1. Command Request
public class Create{Entity}Command : IRequest<BaseCommandResponse<{IdType}>>
{
    public Create{Entity}Dto {Entity}Dto { get; set; } = null!;
}

// 2. Command Handler
public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly I{RelatedEntity}Repository _{relatedEntity}Repository;
    private readonly IMapper _mapper;

    public async Task<BaseCommandResponse<{IdType}>> Handle(...)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // Instantiate validator manually
        var validator = new Create{Entity}DtoValidator(_{relatedEntity}Repository);
        var validationResult = await validator.ValidateAsync(request.{Entity}Dto);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);
        entity = await _{entity}Repository.Create(entity);

        response.Success = true;
        response.Id = entity.Id;
        return response;
    }
}
```

### Query Pattern (Read)

```csharp
// 1. Query Request
public class Get{Entity}ListRequest : IRequest<List<{Entity}ListDto>> { }

// 2. Query Handler
public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public async Task<List<{Entity}ListDto>> Handle(...)
    {
        var entities = await _{entity}Repository.Get{Entities}WithDetails();  // Entities
        return _mapper.Map<List<{Entity}ListDto>>(entities);  // DTOs
    }
}
```

---

## Folder Structure

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
├── {Entity}Dto.cs              // Full details
├── {Entity}ListDto.cs          // List view
├── Create{Entity}Dto.cs        // Create payload
├── Update{Entity}Dto.cs        // Update payload
└── Validators/
    ├── Create{Entity}DtoValidator.cs
    └── Update{Entity}DtoValidator.cs
```

---

## Controller Pattern

```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {Entity}Controller(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("Get all {Entities}")]
    [ProducesResponseType(typeof(List<{Entity}ListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll()
        => Ok(await _mediator.Send(new Get{Entity}ListRequest()));

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EndpointSummary("Get {Entity} by ID")]
    [ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
        => Ok(await _mediator.Send(new Get{Entity}DetailsRequest { Id = id }));

    [HttpPost]
    [Authorize]
    [EndpointSummary("Create {Entity}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
    {
        var response = await _mediator.Send(new Create{Entity}Command { {Entity}Dto = dto });
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    [EndpointSummary("Update {Entity}")]
    [Consumes("application/json")]
    public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Update({IdType} id, [FromBody] Update{Entity}Dto dto)
    {
        if (id != dto.Id) return BadRequest(new { error = "ID mismatch" });
        var response = await _mediator.Send(new Update{Entity}Command { {Entity}Dto = dto });
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [EndpointSummary("Delete {Entity}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete({IdType} id)
    {
        var result = await _mediator.Send(new Delete{Entity}Command { Id = id });
        return result ? NoContent() : NotFound();
    }
}
```

---

## API Endpoint Metadata (Required)

All controller actions must include OpenAPI attributes:

```csharp
[HttpGet("{id}")]
[EndpointSummary("Get {Entity} Details")]
[EndpointDescription("Returns full {entity} details including related entities.")]
[AllowAnonymous]
[ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
```

**Required Attributes**:
- `[EndpointSummary]` - Short title
- `[EndpointDescription]` - Detailed description
- `[ProducesResponseType]` - Success + failure codes
- `[Consumes]` - For POST/PUT with JSON body

---

## Common Mistakes & Fixes

| Mistake | Fix |
|---------|-----|
| Repository returns DTOs | Return entities; map in handler |
| Validator injected via DI | Instantiate manually in handler |
| `{parentEntity}.{ChildEntities}.Add()` | Use `_{linkEntity}Repository.Create()` |
| `public long Id` | Use `public int Id` or `public Guid Id` |
| `= 0` default in entity | Remove; set in handler |
| Removed using statements | Keep all using statements |
| `IRequest<{IdType}>` | Use `IRequest<BaseCommandResponse<{IdType}>>` |
| Nested namespaces | Use file-scoped namespaces |
| Missing userId fallback | Use sub → nameidentifier → sid |
| Missing [AllowAnonymous] on GET | Add for public read access |

---

## Design Principles Summary

| Principle | Application |
|-----------|-------------|
| **S**ingle Responsibility | Each handler handles ONE command/query |
| **O**pen/Closed | Use interfaces for extension |
| **L**iskov Substitution | Repositories implement interfaces correctly |
| **I**nterface Segregation | Small, focused interfaces |
| **D**ependency Inversion | Depend on abstractions |
| **DRY** | Use `GenericRepository<T, TId>` |
| **KISS** | Simple handlers, one responsibility |
| **YAGNI** | Don't add unused abstractions |

---

## Related Documentation

- **[TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md)** - Placeholder substitution guide
- **[GOVERNANCE.md](GOVERNANCE.md)** - Complete coding conventions
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture overview

## Skills Reference

- **`clean-architecture-rules`** - Dependency rules and layer boundaries
- **`cqrs-mediatr-guidelines`** - CQRS patterns with MediatR
- **`dotnet-efcore-guidelines`** - EF Core and repository patterns

---

**Remember**: These rules ensure architectural consistency across all projects. Follow them without exception.
