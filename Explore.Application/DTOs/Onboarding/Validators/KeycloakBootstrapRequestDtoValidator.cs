// ABOUTME: FluentValidation rules for setup-time Keycloak bootstrap requests.
// ABOUTME: Blocks malformed URLs, blank identifiers, control characters, and oversized secret payloads before side effects.

using FluentValidation;

namespace Explore.Application.DTOs.Onboarding.Validators;

public class KeycloakBootstrapRequestDtoValidator : AbstractValidator<KeycloakBootstrapRequestDto>
{
    private const int IdentifierMaxLength = 128;
    private const int SecretMaxLength = 4096;
    private const int UrlMaxLength = 2048;

    public KeycloakBootstrapRequestDtoValidator()
    {
        RuleFor(x => x.KeycloakBaseUrl)
            .NotEmpty()
            .WithMessage("Keycloak base URL is required.")
            .MaximumLength(UrlMaxLength)
            .WithMessage("Keycloak base URL is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Keycloak base URL must not contain control characters.")
            .Must(BeSafeHttpUrl)
            .WithMessage("Keycloak base URL must be an absolute HTTP or HTTPS URL without user info or fragments.");

        RuleFor(x => x.Realm)
            .NotEmpty()
            .WithMessage("Keycloak realm is required.")
            .MaximumLength(IdentifierMaxLength)
            .WithMessage("Keycloak realm is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Keycloak realm must not contain control characters.");

        RuleFor(x => x.BlazorClientId)
            .NotEmpty()
            .WithMessage("Blazor client ID is required.")
            .MaximumLength(IdentifierMaxLength)
            .WithMessage("Blazor client ID is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Blazor client ID must not contain control characters.");

        RuleFor(x => x.BlazorClientSecret)
            .NotEmpty()
            .WithMessage("Blazor client secret is required.")
            .MaximumLength(SecretMaxLength)
            .WithMessage("Blazor client secret is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Blazor client secret must not contain control characters.");

        RuleFor(x => x.ApiClientId)
            .MaximumLength(IdentifierMaxLength)
            .WithMessage("API client ID is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("API client ID must not contain control characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApiClientId));

        RuleFor(x => x.ApiClientSecret)
            .MaximumLength(SecretMaxLength)
            .WithMessage("API client secret is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("API client secret must not contain control characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApiClientSecret));

        RuleFor(x => x.BootstrapAdminUsername)
            .NotEmpty()
            .WithMessage("Bootstrap admin username is required.")
            .MaximumLength(IdentifierMaxLength)
            .WithMessage("Bootstrap admin username is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Bootstrap admin username must not contain control characters.");

        RuleFor(x => x.BootstrapAdminPassword)
            .NotEmpty()
            .WithMessage("Bootstrap admin password is required.")
            .MaximumLength(SecretMaxLength)
            .WithMessage("Bootstrap admin password is too long.")
            .Must(NotContainControlCharacters)
            .WithMessage("Bootstrap admin password must not contain control characters.");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Keycloak bootstrap mode is not supported.");
    }

    private static bool BeSafeHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && string.IsNullOrEmpty(uri.UserInfo)
               && string.IsNullOrEmpty(uri.Fragment);
    }

    private static bool NotContainControlCharacters(string? value)
    {
        return value is null || !value.Any(char.IsControl);
    }
}
