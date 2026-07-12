// ABOUTME: Unit tests for setup-time auth provider configuration secret validation.
// ABOUTME: Proves redacted server-owned secrets are accepted without trusting browser ownership metadata.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class SaveAuthProviderConfigurationCommandHandlerTests
{
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly SaveAuthProviderConfigurationCommandHandler _handler;

    public SaveAuthProviderConfigurationCommandHandlerTests()
    {
        _configurationService = Substitute.For<IAuthProviderConfigurationService>();
        _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
        _configurationService.ReadConfigurationAsync().Returns(new AuthProviderConfigurationDto());

        _handler = new SaveAuthProviderConfigurationCommandHandler(
            _configurationService,
            _jwtAuthorityRefreshNotifier,
            Substitute.For<ILogger<SaveAuthProviderConfigurationCommandHandler>>());
    }

    [Test]
    public async Task Handle_WithRedactedConfiguredKeycloakSecret_AppliesConfiguration()
    {
        _configurationService.ReadConfigurationAsync().Returns(new AuthProviderConfigurationDto
        {
            KeycloakAuthority = "https://id.example.test/realms/event",
            KeycloakClientId = "islamu-event-blazor",
            KeycloakClientSecretOwnership = { Configured = true }
        });
        var configuration = CreateKeycloakConfiguration();

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.Received(1).ApplyConfigurationAsync(configuration);
        await _jwtAuthorityRefreshNotifier.Received(1).ReloadAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithForgedConfiguredOwnershipAndNoServerSecret_ReturnsValidationFailure()
    {
        var configuration = CreateKeycloakConfiguration();
        configuration.KeycloakClientSecretOwnership.Configured = true;

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Invalid auth provider configuration");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());
    }

    private static SaveAuthProviderConfigurationCommand CreateCommand(
        AuthProviderConfigurationDto configuration)
    {
        return new SaveAuthProviderConfigurationCommand
        {
            Configuration = configuration
        };
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
