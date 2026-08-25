using FluentValidation;

namespace Explore.Application.DTOs.Location.Validators;

public class CreateLocationDtoValidator : AbstractValidator<CreateLocationDto>
{
    public CreateLocationDtoValidator()
    {
        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Address)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Postcode)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Country)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.City)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p)
            .Must(p => p.Latitude.HasValue == p.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided or both omitted.");

        RuleFor(p => p.Latitude)
            .InclusiveBetween(-90, 90).When(p => p.Latitude.HasValue)
            .WithMessage("{PropertyName} must be between -90 and 90.");

        RuleFor(p => p.Longitude)
            .InclusiveBetween(-180, 180).When(p => p.Longitude.HasValue)
            .WithMessage("{PropertyName} must be between -180 and 180.");

        RuleFor(p => p.Timezone)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Timezone))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here
    }
}
