# Validation Integration - ISLAMU Event Conventions

## ISLAMU Event Validation Pattern

**CRITICAL**: Validation is **manual in handlers** (not automated pipeline behavior).

## Validator Structure

**Real Example from Event**:
```csharp
// File: Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs
namespace Explore.Application.DTOs.Event.Validators;

using FluentValidation;
using Explore.Application.Contracts.Persistence;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;

    public CreateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IStorageObjectRepository storageObjectRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _storageObjectRepository = storageObjectRepository;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event type is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventTypeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Event type not found");

        RuleFor(x => x.AudienceGenderId)
            .NotEmpty().WithMessage("Audience gender is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceGenderRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience gender not found");

        RuleFor(x => x.AudienceAgeId)
            .NotEmpty().WithMessage("Audience age is required")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceAgeRepository.Exists(id);
                return exists;
            })
            .WithMessage("Audience age not found");

        RuleFor(x => x.OrganizationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _organizationRepository.Exists(id.Value);
            })
            .When(x => x.OrganizationId.HasValue)
            .WithMessage("Organization does not exist.");

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

## Handler Validation Pattern

**Real Example from CreateEventCommandHandler**:
```csharp
// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // 1. Create validator instance (manual instantiation)
    var validator = new CreateEventDtoValidator(
        _audienceAgeRepository,
        _audienceGenderRepository,
        _eventTypeRepository,
        _organizationRepository,
        _storageObjectRepository);

    // 2. Validate DTO
    var validationResult = await validator.ValidateAsync(request.EventDto, cancellationToken);

    // 3. Check result
    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Event creation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 4. Proceed with business logic
    var @event = _mapper.Map<Event>(request.EventDto);
    @event.TotalViews = 0;
    @event.TenantId = _tenantContext.TenantId;
    @event = await _eventRepository.Create(@event);

    response.Success = true;
    response.Id = @event.Id;
    response.Message = "Event created successfully.";

    return response;
}
```

**Critical Steps**:
1. **Instantiate validator manually** with repository dependencies
2. **Validate DTO** using `ValidateAsync`
3. **Check `IsValid`** property
4. **Return errors** in `BaseCommandResponse.Errors` list
5. **Early return** if validation fails

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

RuleFor(x => x.MaxAudienceAttendees)
    .InclusiveBetween(1, 10000).WithMessage("Max attendees must be between 1 and 10000");
```

### Email Validation
```csharp
RuleFor(x => x.Email)
    .EmailAddress().WithMessage("Invalid email address");
```

### Async Database Validation (Foreign Keys)
```csharp
RuleFor(x => x.EventTypeId)
    .NotEmpty().WithMessage("Event type is required")
    .MustAsync(async (id, cancellation) =>
    {
        var exists = await _eventTypeRepository.Exists(id);
        return exists;
    })
    .WithMessage("Event type not found");
```

### Conditional Validation
```csharp
RuleFor(x => x.ExternalRegistrationUrl)
    .NotEmpty()
    .When(x => x.IsRegistrationRequired)
    .WithMessage("External registration URL is required when registration is enabled");

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
        // Custom logic: slug must be unique.
        // Implement using a repository method if/when one exists (e.g., GetBySlug).
        return true;
    })
    .WithMessage("Slug must be unique");
```

### Complex Validation (Multiple Properties)
```csharp
RuleFor(x => x)
    .Must(x => x.FirstSessionDate <= x.LastSessionDate)
    .WithMessage("First session date must be before or equal to last session date")
    .When(x => x.FirstSessionDate.HasValue && x.LastSessionDate.HasValue);
```

## Update DTO Validation

**Different from Create**:
```csharp
// File: Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs
public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    public UpdateEventDtoValidator()
    {
        // ID is required for updates
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Event ID is required");

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

## Error Response Format

**Validation Failure Response**:
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "success": false,
  "message": "Event creation failed.",
  "errors": [
    "Title is required",
    "Event type not found",
    "Audience gender is required"
  ]
}
```

**Success Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "success": true,
  "message": "Event created successfully.",
  "errors": []
}
```

## Why Manual Validation?

| Approach | ISLAMU Event Pattern | Pipeline Behavior |
|----------|---------------------|-------------------|
| **When runs** | Manually in handler | Automatically before handler |
| **Validator creation** | Manual instantiation | DI injection |
| **Dependencies** | Passed to constructor | Must be registered in DI |
| **Flexibility** | High - can skip validation | Fixed - always runs |
| **Error handling** | BaseCommandResponse | Exceptions or custom |
| **Control** | Explicit in handler | Hidden in pipeline |

**Benefits of Manual Validation**:
- ✅ Fine-grained control over when validation runs
- ✅ Validators can have different dependencies per use case
- ✅ No need to register validators in DI container
- ✅ Clear and explicit validation flow
- ✅ Easy to understand for new developers

## FluentValidation NuGet Package

```xml
<PackageReference Include="FluentValidation" Version="11.9.0" />
```

**No need for** `FluentValidation.AspNetCore` or `FluentValidation.DependencyInjectionExtensions` - we don't use pipeline behaviors or DI registration.

---

**See Also**:
- [command-patterns.md](command-patterns.md) - Complete command handler examples
- [handler-patterns.md](handler-patterns.md) - Handler structure and patterns
