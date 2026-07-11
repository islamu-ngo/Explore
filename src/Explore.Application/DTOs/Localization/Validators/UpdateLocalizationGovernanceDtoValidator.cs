// ABOUTME: Validates UpdateLocalizationGovernanceDto — provider rules, culture membership, kill-switch consistency.
// ABOUTME: Manually instantiated by the handler (repo convention: no DI for validators).

namespace Explore.Application.DTOs.Localization.Validators;

using Explore.Domain.Common.Localization;
using FluentValidation;

public class UpdateLocalizationGovernanceDtoValidator : AbstractValidator<UpdateLocalizationGovernanceDto>
{
    private static readonly string[] AllowedProviders = ["none", "tolgee", "weblate"];

    public UpdateLocalizationGovernanceDtoValidator()
    {
        RuleFor(x => x.TmsProvider)
            .NotEmpty().WithMessage("TmsProvider is required.")
            .Must(p => AllowedProviders.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("TmsProvider must be one of: none, tolgee, weblate.");

        RuleFor(x => x.TmsApiUrl)
            .NotEmpty().WithMessage("TmsApiUrl is required when a TMS provider is selected.")
            .Must(BeValidAbsoluteUrl).WithMessage("TmsApiUrl must be an absolute http(s) URL.")
            .When(x => !string.Equals(x.TmsProvider, "none", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.TmsProjectId)
            .NotEmpty().WithMessage("TmsProjectId is required when a TMS provider is selected.")
            .When(x => !string.Equals(x.TmsProvider, "none", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.TmsComponent)
            .NotEmpty().WithMessage("TmsComponent (component slug) is required for Weblate.")
            .When(x => string.Equals(x.TmsProvider, "weblate", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.EnabledLanguages)
            .NotNull().WithMessage("EnabledLanguages is required.")
            .Must(list => list is { Length: > 0 })
            .WithMessage("At least one language must be enabled.")
            .Must(list => list.All(code => CultureRegistry.Contains(code)))
            .WithMessage("EnabledLanguages contains codes that are not in CultureRegistry.");

        RuleFor(x => x.FallbackLanguage)
            .NotEmpty().WithMessage("FallbackLanguage is required.")
            .Must(code => CultureRegistry.Contains(code))
            .WithMessage("FallbackLanguage must be a supported culture code.");

        RuleFor(x => x)
            .Must(dto => dto.EnabledLanguages.Contains(dto.FallbackLanguage, StringComparer.OrdinalIgnoreCase))
            .WithMessage("FallbackLanguage must be present in EnabledLanguages.")
            .WithName("FallbackLanguage");

        RuleFor(x => x)
            .Must(dto => dto.EnabledLanguages.Contains(dto.DefaultLanguage, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DefaultLanguage must be present in EnabledLanguages.")
            .WithName("DefaultLanguage");

        RuleFor(x => x.DefaultLanguage)
            .NotEmpty().WithMessage("DefaultLanguage is required.")
            .Must(code => CultureRegistry.Contains(code))
            .WithMessage("DefaultLanguage must be a supported culture code.");
    }

    private static bool BeValidAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
