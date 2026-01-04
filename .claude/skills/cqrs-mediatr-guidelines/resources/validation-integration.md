# Validation Integration

## ISLAMU Event Validation Pattern

Validation is **manual in handlers** (not automated pipeline behavior).

## Validator Structure

```csharp
// DTOs/Event/Validators/CreateEventDtoValidator.cs
using FluentValidation;
using Explore.Application.Contracts.Persistence;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IOrganizationRepository _organizationRepository;

    public CreateEventDtoValidator(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(200);

        RuleFor(x => x.OrganizationId)
            .MustAsync(async (id, token) =>
            {
                return await _organizationRepository.Exists(id);
            }).WithMessage("Organization does not exist");
    }
}
```

## Handler Validation Pattern

```csharp
public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var response = new BaseCommandResponse<Guid>();

    // 1. Create validator instance (manual)
    var validator = new CreateEventDtoValidator(_organizationRepository);

    // 2. Validate
    var validationResult = await validator.ValidateAsync(request.EventDto);

    // 3. Check result
    if (!validationResult.IsValid)
    {
        response.Success = false;
        response.Message = "Validation failed.";
        response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return response;
    }

    // 4. Proceed with logic
    // ...
}
```

## Common Validation Rules

```csharp
// Required
RuleFor(x => x.Title).NotEmpty();

// Length
RuleFor(x => x.Title).MaximumLength(200);

// Range
RuleFor(x => x.Capacity).InclusiveBetween(1, 10000);

// Email
RuleFor(x => x.Email).EmailAddress();

// Async validation (database check)
RuleFor(x => x.OrganizationId)
    .MustAsync(async (id, token) => await _repo.Exists(id))
    .WithMessage("Not found");

// Conditional
RuleFor(x => x.EndDate)
    .GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue);
```

---

See [command-patterns.md](command-patterns.md) for complete handler examples.
