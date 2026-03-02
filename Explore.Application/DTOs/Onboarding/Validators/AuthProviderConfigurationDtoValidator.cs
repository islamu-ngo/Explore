// ABOUTME: FluentValidation validator for auth provider configuration during instance setup.
// ABOUTME: Enforces at least one provider enabled and required credentials when a provider is toggled on.

using FluentValidation;

namespace Explore.Application.DTOs.Onboarding.Validators;

public class AuthProviderConfigurationDtoValidator : AbstractValidator<AuthProviderConfigurationDto>
{
    public AuthProviderConfigurationDtoValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneProviderEnabled)
            .WithMessage("At least one authentication provider must be enabled.");

        When(x => x.KeycloakEnabled, () =>
        {
            RuleFor(x => x.KeycloakAuthority)
                .NotEmpty()
                .WithMessage("Keycloak authority URL is required when Keycloak is enabled.");

            RuleFor(x => x.KeycloakClientId)
                .NotEmpty()
                .WithMessage("Keycloak client ID is required when Keycloak is enabled.");

            RuleFor(x => x.KeycloakClientSecret)
                .NotEmpty()
                .WithMessage("Keycloak client secret is required when Keycloak is enabled.");
        });

        When(x => x.AtprotoLoginEnabled, () =>
        {
            RuleFor(x => x.AtprotoPublicUrl)
                .NotEmpty()
                .WithMessage("A publicly accessible URL is required when ATProto login is enabled.");

            RuleFor(x => x.AtprotoPublicUrl)
                .Must(BeAValidUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.AtprotoPublicUrl))
                .WithMessage("ATProto public URL must be a valid URL.");
        });

        When(x => x.GoogleSsoEnabled, () =>
        {
            RuleFor(x => x.GoogleClientId)
                .NotEmpty()
                .WithMessage("Google client ID is required when Google SSO is enabled.");

            RuleFor(x => x.GoogleClientSecret)
                .NotEmpty()
                .WithMessage("Google client secret is required when Google SSO is enabled.");
        });
    }

    private static bool HasAtLeastOneProviderEnabled(AuthProviderConfigurationDto dto)
    {
        return dto.KeycloakEnabled || dto.AtprotoLoginEnabled || dto.GoogleSsoEnabled;
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
