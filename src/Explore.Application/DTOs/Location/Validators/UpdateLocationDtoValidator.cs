// ABOUTME: FluentValidation rules for grouped Location PATCH payloads.
// ABOUTME: Validates per-property groups and explicit clear semantics for nullable fields.

using FluentValidation;

namespace Explore.Application.DTOs.Location.Validators;

public class UpdateLocationDtoValidator : AbstractValidator<UpdateLocationDto>
{
    public UpdateLocationDtoValidator()
    {
        RuleFor(dto => dto.FullName!)
            .SetValidator(new UpdateLocationFullNameDtoValidator())
            .When(dto => dto.FullName is not null);

        RuleFor(dto => dto.Address!)
            .SetValidator(new UpdateLocationAddressDtoValidator())
            .When(dto => dto.Address is not null);

        RuleFor(dto => dto.Postcode!)
            .SetValidator(new UpdateLocationPostcodeDtoValidator())
            .When(dto => dto.Postcode is not null);

        RuleFor(dto => dto.Country!)
            .SetValidator(new UpdateLocationCountryDtoValidator())
            .When(dto => dto.Country is not null);

        RuleFor(dto => dto.City!)
            .SetValidator(new UpdateLocationCityDtoValidator())
            .When(dto => dto.City is not null);

        RuleFor(dto => dto.Latitude!)
            .SetValidator(new UpdateLocationLatitudeDtoValidator())
            .When(dto => dto.Latitude is not null);

        RuleFor(dto => dto.Longitude!)
            .SetValidator(new UpdateLocationLongitudeDtoValidator())
            .When(dto => dto.Longitude is not null);

        RuleFor(dto => dto.Timezone!)
            .SetValidator(new UpdateLocationTimezoneDtoValidator())
            .When(dto => dto.Timezone is not null);

        RuleFor(dto => dto)
            .Must(HasAnyGroup)
            .WithMessage("At least one location update group must be provided.");
    }

    private static bool HasAnyGroup(UpdateLocationDto dto) =>
        dto.FullName is not null ||
        dto.Address is not null ||
        dto.Postcode is not null ||
        dto.Country is not null ||
        dto.City is not null ||
        dto.Latitude is not null ||
        dto.Longitude is not null ||
        dto.Timezone is not null;
}

public class UpdateLocationFullNameDtoValidator : AbstractValidator<UpdateLocationFullNameDto>
{
    public UpdateLocationFullNameDtoValidator()
    {
        RuleFor(dto => dto.Value)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(500).WithMessage("Full name must not exceed 500 characters.");
    }
}

public class UpdateLocationAddressDtoValidator : AbstractValidator<UpdateLocationAddressDto>
{
    public UpdateLocationAddressDtoValidator()
    {
        RuleFor(dto => dto.Value)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}

public class UpdateLocationPostcodeDtoValidator : AbstractValidator<UpdateLocationPostcodeDto>
{
    public UpdateLocationPostcodeDtoValidator()
    {
        RuleFor(dto => dto.Value)
            .NotEmpty().WithMessage("Postcode is required.")
            .MaximumLength(500).WithMessage("Postcode must not exceed 500 characters.");
    }
}

public class UpdateLocationCountryDtoValidator : AbstractValidator<UpdateLocationCountryDto>
{
    public UpdateLocationCountryDtoValidator()
    {
        RuleFor(dto => dto.Value)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(500).WithMessage("Country must not exceed 500 characters.");
    }
}

public class UpdateLocationCityDtoValidator : AbstractValidator<UpdateLocationCityDto>
{
    public UpdateLocationCityDtoValidator()
    {
        RuleFor(dto => dto.Value)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(500).WithMessage("City must not exceed 500 characters.");
    }
}

public class UpdateLocationLatitudeDtoValidator : AbstractValidator<UpdateLocationLatitudeDto>
{
    public UpdateLocationLatitudeDtoValidator()
    {
        RuleFor(dto => dto)
            .Must(dto => dto.Value.HasValue)
            .WithMessage("Latitude group must include Value.");

        RuleFor(dto => dto.Value.Value)
            .InclusiveBetween(-90, 90)
            .When(dto => dto.Value.HasValue && dto.Value.Value.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");
    }
}

public class UpdateLocationLongitudeDtoValidator : AbstractValidator<UpdateLocationLongitudeDto>
{
    public UpdateLocationLongitudeDtoValidator()
    {
        RuleFor(dto => dto)
            .Must(dto => dto.Value.HasValue)
            .WithMessage("Longitude group must include Value.");

        RuleFor(dto => dto.Value.Value)
            .InclusiveBetween(-180, 180)
            .When(dto => dto.Value.HasValue && dto.Value.Value.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}

public class UpdateLocationTimezoneDtoValidator : AbstractValidator<UpdateLocationTimezoneDto>
{
    public UpdateLocationTimezoneDtoValidator()
    {
        RuleFor(dto => dto)
            .Must(dto => dto.Value.HasValue)
            .WithMessage("Timezone group must include Value.");

        RuleFor(dto => dto.Value.Value)
            .NotEmpty().WithMessage("Timezone must not be blank. Use null to clear it.")
            .MaximumLength(500).WithMessage("Timezone must not exceed 500 characters.")
            .When(dto => dto.Value.HasValue && dto.Value.Value is not null);
    }
}
