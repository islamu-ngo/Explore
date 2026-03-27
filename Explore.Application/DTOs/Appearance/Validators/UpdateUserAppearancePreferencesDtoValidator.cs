// ABOUTME: Validates authenticated user appearance preference updates.
// ABOUTME: Restricts theme mode to the supported appearance values already defined by the settings model.

namespace Explore.Application.DTOs.Appearance.Validators;

using FluentValidation;

public class UpdateUserAppearancePreferencesDtoValidator : AbstractValidator<UpdateUserAppearancePreferencesDto>
{
    public UpdateUserAppearancePreferencesDtoValidator()
    {
        RuleFor(preferences => preferences.ThemeMode)
            .NotEmpty().WithMessage("Theme mode is required.")
            .Must(mode => mode is "system" or "light" or "dark")
            .WithMessage("Theme mode must be one of: system, light, dark.");

        RuleFor(preferences => preferences.Direction)
            .NotEmpty().WithMessage("Direction is required.")
            .Must(dir => dir is "auto" or "ltr" or "rtl")
            .WithMessage("Direction must be one of: auto, ltr, rtl.");
    }
}
