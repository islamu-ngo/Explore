# Validation Integration - Complete Reference

> **Project-Agnostic Validation Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

## Validation Pattern Overview

**CRITICAL**: Validation is **manual in handlers** (not automated pipeline behavior).

---

## Validator Structure

```csharp
// File: {Project}.Application/DTOs/{Entity}/Validators/Create{Entity}DtoValidator.cs
namespace {Project}.Application.DTOs.{Entity}.Validators;

using FluentValidation;
using {Project}.Application.Contracts.Persistence;

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

        RuleFor(x => x.{RelatedEntity1}Id)
            .NotEmpty().WithMessage("{RelatedEntity1} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity1}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity1} not found");

        RuleFor(x => x.{RelatedEntity2}Id)
            .NotEmpty().WithMessage("{RelatedEntity2} is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _{relatedEntity2}Repository.Exists(id);
                return exists;
            })
            .WithMessage("{RelatedEntity2} not found");

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
    }
}
```

**Key Patterns**:
- ✅ Validators take repositories in constructor
- ✅ Use `MustAsync` for async database validation
- ✅ Check foreign key existence with repository
- ✅ Custom error messages with `.WithMessage()`

---

## Handler Validation Pattern

```csharp
// File: {Project}.Application/Features/{Entities}/Handlers/Commands/Create{Entity}CommandHandler.cs
public async Task<BaseCommandResponse<{IdType}>> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<{IdType}>();

    // 1. Create validator instance (manual instantiation)
    var validator = new Create{Entity}DtoValidator(
        _{relatedEntity1}Repository,
        _{relatedEntity2}Repository,
        _{lookupEntity}Repository,
        _{parentEntity}Repository,
        _storageObjectRepository);

    // 2. Validate DTO
    var validationResult = await validator.ValidateAsync(request.{Entity}Dto, cancellationToken);

    // 3. Check result
    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "{Entity} creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 4. Proceed with business logic
    var {entity} = _mapper.Map<{Entity}>(request.{Entity}Dto);
    {entity}.ViewCount = 0;
    {entity}.TenantId = _tenantContext.TenantId;
    {entity} = await _{entity}Repository.Create({entity});

    response.Success = true;
    response.Id = {entity}.Id;
    response.Message = "{Entity} created successfully.";

    return response;
}
```

**Critical Steps**:
1. **Instantiate validator manually** with repository dependencies
2. **Validate DTO** using `ValidateAsync`
3. **Check `IsValid`** property
4. **Return errors** in `BaseCommandResponse.Errors` list
5. **Early return** if validation fails

---

## Common Validation Rules

### Required Field
```csharp
RuleFor(x => x.Title)
    .NotEmpty().WithMessage("Title is required");
```

### String Length
```csharp
RuleFor(x => x.Title)
    .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

RuleFor(x => x.Description)
    .MinimumLength(10).WithMessage("Description must be at least 10 characters")
    .MaximumLength(5000);
```

### Numeric Range
```csharp
RuleFor(x => x.Price)
    .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");

RuleFor(x => x.MaxAttendees)
    .InclusiveBetween(1, 10000).WithMessage("Max attendees must be between 1 and 10000");
```

### Email Validation
```csharp
RuleFor(x => x.Email)
    .EmailAddress().WithMessage("Invalid email address");
```

### Async Database Validation (Foreign Keys)
```csharp
RuleFor(x => x.{LookupEntity}Id)
    .NotEmpty().WithMessage("{LookupEntity} is required")
    .MustAsync(async (id, cancellation) =>
    {
        var exists = await _{lookupEntity}Repository.Exists(id);
        return exists;
    })
    .WithMessage("{LookupEntity} not found");
```

### Conditional Validation
```csharp
RuleFor(x => x.ExternalUrl)
    .NotEmpty()
    .When(x => x.IsExternalEnabled)
    .WithMessage("External URL is required when external option is enabled");

RuleFor(x => x.Price)
    .GreaterThan(0)
    .When(x => !string.IsNullOrEmpty(x.CurrencyCode))
    .WithMessage("Price must be greater than 0 when currency is specified");
```

### Custom Validation Logic
```csharp
RuleFor(x => x.Slug)
    .MustAsync(async (dto, slug, cancellation) =>
    {
        // Custom logic: slug must be unique
        var existing = await _{entity}Repository.GetBySlug(slug);
        return existing == null;
    })
    .WithMessage("Slug must be unique");
```

### Complex Validation (Multiple Properties)
```csharp
RuleFor(x => x)
    .Must(x => x.StartDate <= x.EndDate)
    .WithMessage("Start date must be before or equal to end date")
    .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
```

---

## Update DTO Validation

**Different from Create**:
```csharp
// File: {Project}.Application/DTOs/{Entity}/Validators/Update{Entity}DtoValidator.cs
public class Update{Entity}DtoValidator : AbstractValidator<Update{Entity}Dto>
{
    public Update{Entity}DtoValidator()
    {
        // ID is required for updates
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("{Entity} ID is required");

        // Title might be optional for partial updates
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

**Key Differences**:
- ✅ `Id` property is required
- ⚠️ Some fields might be optional (partial updates)
- ✅ Validation rules can be less strict than create

---

## Error Response Format

**Validation Failure Response**:
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "success": false,
  "message": "{Entity} creation failed.",
  "errors": [
    "Title is required",
    "{LookupEntity} not found",
    "{RelatedEntity1} is required"
  ]
}
```

**Success Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "success": true,
  "message": "{Entity} created successfully.",
  "errors": []
}
```

---

## Why Manual Validation?

| Approach | Manual Pattern | Pipeline Behavior |
|----------|---------------|-------------------|
| **When runs** | Manually in handler | Automatically before handler |
| **Validator creation** | Manual instantiation | DI injection |
| **Dependencies** | Passed to constructor | Must be registered in DI |
| **Flexibility** | High - can skip validation | Fixed - always runs |
| **Error handling** | BaseCommandResponse | Exceptions or custom |
| **Control** | Explicit in handler | Hidden in pipeline |

**Benefits of Manual Validation**:
- ✅ Fine-grained control over when validation runs
- ✅ Validators can have different repository dependencies per use case
- ✅ No need to register validators in DI container
- ✅ Clear and explicit validation flow
- ✅ Easy to understand for new developers

---

## Self-Referencing Entity Validation

For entities with parent relationships (e.g., hierarchical categories):

```csharp
public class Create{Entity}DtoValidator : AbstractValidator<Create{Entity}Dto>
{
    private readonly I{Entity}Repository _{entity}Repository;

    public Create{Entity}DtoValidator(I{Entity}Repository {entity}Repository)
    {
        _{entity}Repository = {entity}Repository;

        // Prevent self-reference (relevant for Update scenarios where Id exists)
        RuleFor(x => x.ParentId)
            .NotEqual(x => x.Id)
            .When(x => x.ParentId.HasValue && x.Id != default)
            .WithMessage("{Entity} cannot be its own parent");

        // Validate parent exists
        RuleFor(x => x.ParentId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _{entity}Repository.Exists(id.Value);
            })
            .When(x => x.ParentId.HasValue)
            .WithMessage("Parent {Entity} not found");

        // Prevent circular reference (for update)
        RuleFor(x => x.ParentId)
            .MustAsync(NotCreateCircularReference)
            .When(x => x.ParentId.HasValue && x.Id != default)
            .WithMessage("Would create circular reference");
    }

    private async Task<bool> NotCreateCircularReference(
        Create{Entity}Dto dto,
        {IdType}? parentId,
        CancellationToken cancellation)
    {
        // Implementation depends on business logic
        return true;
    }
}
```

---

## FluentValidation NuGet Package

```xml
<PackageReference Include="FluentValidation" Version="11.9.0" />
```

**No need for** `FluentValidation.AspNetCore` or `FluentValidation.DependencyInjectionExtensions` - we don't use pipeline behaviors or DI registration.

---

**See Also**:
- [command-patterns.md](command-patterns.md) - Complete command handler examples
- [handler-patterns.md](handler-patterns.md) - Handler structure and patterns
