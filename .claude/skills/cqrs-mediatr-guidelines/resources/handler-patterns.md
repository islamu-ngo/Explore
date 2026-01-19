# Handler Patterns - Complete Reference

> **Project-Agnostic Handler Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Repository Usage in Handlers

**CRITICAL RULE**: Handlers use repositories (not DbContext directly).

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
namespace {Project}.Application.Features.{Entities}.Handlers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using {Project}.Application.Contracts.Persistence;  // ✅ Repository interfaces
using {Project}.Application.DTOs.{Entity}.Validators;
using {Project}.Application.Features.{Entities}.Requests.Commands;
using {Project}.Application.Responses;
using {Project}.Domain;
using MediatR;

public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, BaseCommandResponse<{IdType}>>
{
    private readonly I{Entity}Repository _{entity}Repository;  // ✅ Repository interface
    private readonly I{RelatedEntity1}Repository _{relatedEntity1}Repository;
    private readonly I{RelatedEntity2}Repository _{relatedEntity2}Repository;
    private readonly I{LookupEntity}Repository _{lookupEntity}Repository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    // ✅ Inject repositories, not DbContext
    public Create{Entity}CommandHandler(
        I{Entity}Repository {entity}Repository,
        I{RelatedEntity1}Repository {relatedEntity1}Repository,
        I{RelatedEntity2}Repository {relatedEntity2}Repository,
        I{LookupEntity}Repository {lookupEntity}Repository,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _{entity}Repository = {entity}Repository;
        _{relatedEntity1}Repository = {relatedEntity1}Repository;
        _{relatedEntity2}Repository = {relatedEntity2}Repository;
        _{lookupEntity}Repository = {lookupEntity}Repository;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<{IdType}>();

        // Validation...
        var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);
        {entity}.ViewCount = 0;

        // ✅ Use repository methods
        {entity} = await _{entity}Repository.Create({entity});

        response.Success = true;
        response.Id = {entity}.Id;
        response.Message = "{Entity} created successfully.";

        return response;
    }
}
```

**Why Repositories?**
- ✅ Application layer doesn't depend on EF Core
- ✅ Can mock repositories for unit tests
- ✅ Encapsulates complex queries
- ✅ Consistent data access patterns

---

## AutoMapper Usage

**Query Handler Example**:
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

**Common AutoMapper Patterns**:
```csharp
// Map DTO → Entity (Create)
var entity = _mapper.Map<{Entity}>(request.{Entity}Dto);

// Map DTO → Existing Entity (Update)
_mapper.Map(request.{Entity}Dto, existingEntity);

// Map Entity → DTO (Read)
var dto = _mapper.Map<{Entity}Dto>(entity);

// Map Entity Collection → DTO Collection (List)
var dtos = _mapper.Map<List<{Entity}ListDto>>(entities);
```

---

## CancellationToken Pattern

**Always include `CancellationToken` parameter in Handle method**:

```csharp
public async Task<BaseCommandResponse<{IdType}>> Handle(
    Create{Entity}Command request,
    CancellationToken cancellationToken)  // ✅ Always include
{
    // Pass cancellationToken to all async operations that accept it
    await _{entity}Repository.Create({entity});
}
```

**Why?**
- ✅ Enables request cancellation
- ✅ Prevents wasted work when client disconnects
- ✅ Required by MediatR `IRequestHandler<TRequest, TResponse>`

---

## Manual Validator Instantiation Pattern

**CRITICAL PATTERN**: Validators are instantiated manually with dependencies, NOT injected via DI.

```csharp
public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<{IdType}>();

    // ✅ CRITICAL: Manual validator instantiation with all required repositories
    var validator = new Create{Entity}DtoValidator(
        _{relatedEntity1}Repository,
        _{relatedEntity2}Repository,
        _{lookupEntity}Repository,
        _{storageObject}Repository);

    var validationResult = await validator.ValidateAsync(request.{Entity}Dto, cancellationToken);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "{Entity} creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // Continue with business logic...
}
```

**Why Manual Instantiation?**
- ✅ Fine-grained control over dependencies
- ✅ Validators can have different repository dependencies per use case
- ✅ No need to register validators in DI container
- ✅ Clear and explicit

---

## Command Handler Pattern (Create)

**Full Pattern**:
```csharp
public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<{IdType}>();

    // 1. Manual validation
    var validator = new Create{Entity}DtoValidator(/* repositories */);
    var validationResult = await validator.ValidateAsync(request.{Entity}Dto);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "{Entity} creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 2. Map DTO → Entity
    var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);

    // 3. Set default values (not in DTO)
    {entity}.ViewCount = 0;
    {entity}.TenantId = _tenantContext.TenantId;

    // 4. Save through repository
    {entity} = await _{entity}Repository.Create({entity});

    // 5. Return success response
    response.Success = true;
    response.Id = {entity}.Id;
    response.Message = "{Entity} created successfully.";

    return response;
}
```

---

## Command Handler Pattern (Update)

**Full Pattern**:
```csharp
public async Task<BaseCommandResponse<{IdType}>> Handle(Update{Entity}Command request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<{IdType}>();

    // 1. Validate
    var validator = new Update{Entity}DtoValidator(/* repositories */);
    var validationResult = await validator.ValidateAsync(request.{Entity}Dto);

    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "{Entity} update failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 2. Get existing entity
    var {entity} = await _{entity}Repository.GetById(request.{Entity}Dto.Id);

    if ({entity} == null)
    {
        response.Success = false;
        response.Message = "{Entity} not found.";
        return response;
    }

    // 3. Map DTO → Existing Entity
    _mapper.Map(request.{Entity}Dto, {entity});

    // 4. Update through repository
    await _{entity}Repository.Update({entity});

    // 5. Return success response
    response.Success = true;
    response.Id = {entity}.Id;
    response.Message = "{Entity} updated successfully.";

    return response;
}
```

---

## Command Handler Pattern (Delete)

**Full Pattern**:
```csharp
public async Task<bool> Handle(Delete{Entity}Command request, CancellationToken cancellationToken)
{
    // 1. Get entity
    var {entity} = await _{entity}Repository.GetById(request.Id);

    // 2. Check if exists
    if ({entity} == null)
        return false;

    // 3. Delete through repository
    await _{entity}Repository.Delete({entity});

    // 4. Return success
    return true;
}
```

**Note**: Delete returns `bool`, not `BaseCommandResponse<{IdType}>`.

---

## Query Handler Pattern (List)

**Full Pattern**:
```csharp
public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
{
    // 1. Get entities from repository
    var {entities} = await _{entity}Repository.Get{Entities}WithDetails();

    // 2. Map entities → DTOs
    return _mapper.Map<List<{Entity}ListDto>>({entities});
}
```

**Key Points**:
- ❌ No validation needed for queries
- ❌ No BaseCommandResponse wrapper
- ✅ Return DTOs directly
- ✅ Repository returns entities, handler maps to DTOs

---

## Query Handler Pattern (Details)

**Full Pattern**:
```csharp
public async Task<{Entity}Dto> Handle(Get{Entity}DetailsRequest request, CancellationToken cancellationToken)
{
    // 1. Get entity from repository
    var {entity} = await _{entity}Repository.Get{Entity}WithDetails(request.Id);

    // 2. Return null if not found (controller handles NotFound)
    if ({entity} == null)
        return null;

    // 3. Map entity → DTO
    return _mapper.Map<{Entity}Dto>({entity});
}
```

**Key Points**:
- ✅ Return `null` for not found (not exception)
- ✅ Controller checks for null and returns `NotFound()`
- ✅ Simple and straightforward

---

## Error Handling Pattern

**Commands**:
```csharp
// Validation errors
if (!validationResult.IsValid)
{
    response.Success = false;
    response.Message = "Operation failed.";
    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
    return response;
}

// Not found errors
if (entity == null)
{
    response.Success = false;
    response.Message = "Entity not found.";
    return response;
}

// Success
response.Success = true;
response.Id = entity.Id;
response.Message = "Operation successful.";
return response;
```

**Queries**:
```csharp
// Not found
if (entity == null)
    return null;  // Let controller handle NotFound()

// Success
return _mapper.Map<{Entity}Dto>(entity);
```

---

## Dependency Injection Registration

**Handlers are auto-registered by MediatR**:
```csharp
// File: {Project}.API/Program.cs
using {Project}.Application.Profiles;

// Register MediatR (scans assembly for handlers)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly));

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

**No need to manually register each handler!**

---

## Common Patterns Summary

| Pattern | Command | Query |
|---------|---------|-------|
| **Response Type** | `BaseCommandResponse<{IdType}>` (or `bool` for Delete) | DTO type directly |
| **Validation** | Manual with validator | Not needed |
| **Repository** | CRUD operations | Read with `.Include()` |
| **Mapping** | DTO → Entity | Entity → DTO |
| **Error Handling** | Success flag + errors | Return null |
| **Controller** | Check `Success` flag | Check for null |

---

**See Also**:
- [command-patterns.md](command-patterns.md) - Command request/handler structure
- [query-patterns.md](query-patterns.md) - Query request/handler structure
- [validation-integration.md](validation-integration.md) - FluentValidation patterns
