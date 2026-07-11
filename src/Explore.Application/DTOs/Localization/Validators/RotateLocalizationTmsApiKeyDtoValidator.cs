// ABOUTME: Validates localization TMS API key rotation input before it is encrypted into SecretBinding metadata.
// ABOUTME: Manually instantiated by the rotation handler to preserve the repository validator convention.

namespace Explore.Application.DTOs.Localization.Validators;

using FluentValidation;

public sealed class RotateLocalizationTmsApiKeyDtoValidator : AbstractValidator<RotateLocalizationTmsApiKeyDto>
{
    public RotateLocalizationTmsApiKeyDtoValidator()
    {
        RuleFor(x => x.TmsApiKey)
            .NotEmpty().WithMessage("TMS API key is required.")
            .MaximumLength(4096).WithMessage("TMS API key must be 4096 characters or fewer.")
            .Must(value => value is null || !value.Any(char.IsControl))
            .WithMessage("TMS API key cannot contain control characters.");
    }
}
