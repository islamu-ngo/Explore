// ABOUTME: Unit tests for confidential Keycloak client-secret validation during onboarding.
// ABOUTME: Distinguishes a missing credential from an existing server-side secret redacted from the request.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;

namespace Event.Application.UnitTests.DTOs.Onboarding;

public class AuthProviderConfigurationDtoValidatorTests
{
    [Test]
    public async Task Validate_KeycloakWithoutSecretAndNoConfiguredSecret_IsInvalid()
    {
        var validator = new AuthProviderConfigurationDtoValidator();

        var result = await validator.ValidateAsync(CreateKeycloakConfiguration());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AuthProviderConfigurationDto.KeycloakClientSecret))).IsTrue();
    }

    [Test]
    public async Task Validate_KeycloakWithoutSecretAndConfiguredSecret_IsValid()
    {
        var current = CreateKeycloakConfiguration();
        current.KeycloakClientSecretOwnership.Configured = true;
        var validator = new AuthProviderConfigurationDtoValidator(current);

        var result = await validator.ValidateAsync(CreateKeycloakConfiguration());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_KeycloakWithoutSecretForDifferentClient_IsInvalid()
    {
        var current = CreateKeycloakConfiguration();
        current.KeycloakClientSecretOwnership.Configured = true;
        var validator = new AuthProviderConfigurationDtoValidator(current);
        var requested = CreateKeycloakConfiguration();
        requested.KeycloakClientId = "replacement-client";

        var result = await validator.ValidateAsync(requested);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AuthProviderConfigurationDto.KeycloakClientSecret))).IsTrue();
    }

    private static AuthProviderConfigurationDto CreateKeycloakConfiguration()
    {
        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://id.example.test/realms/event",
            KeycloakClientId = "islamu-event-blazor",
            KeycloakClientSecret = string.Empty
        };
    }
}
