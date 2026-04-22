// ABOUTME: Validates authenticated user appearance preference updates.
// ABOUTME: Restricts theme mode, direction, and language to codebase-supported values; defers DefaultThemeId existence check to the handler.

namespace Explore.Application.DTOs.Appearance.Validators;

using Explore.Domain.Common.Localization;
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

        RuleFor(preferences => preferences.Language)
            .NotEmpty().WithMessage("Language is required.")
            .Must(code => CultureRegistry.Contains(code))
            .WithMessage("Language must be a supported culture code registered in CultureRegistry.");

        RuleFor(preferences => preferences.DefaultThemeId)
            .Must(themeId => themeId is null || themeId != Guid.Empty)
            .WithMessage("DefaultThemeId must be either null (inherit) or a non-empty Guid.");
    }
}
