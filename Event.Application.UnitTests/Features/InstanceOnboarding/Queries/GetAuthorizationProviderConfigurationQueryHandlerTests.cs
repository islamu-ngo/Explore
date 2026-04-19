// ABOUTME: Unit tests for reading authorization provider configuration for setup and admin flows.
// ABOUTME: Verifies Cerbos auto-selection only happens for detected, verified, not-yet-configured environments.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Queries;

public class GetAuthorizationProviderConfigurationQueryHandlerTests
{
    private readonly IAuthorizationProviderConfigurationService _configurationService;
    private readonly GetAuthorizationProviderConfigurationQueryHandler _handler;

    public GetAuthorizationProviderConfigurationQueryHandlerTests()
    {
        _configurationService = Substitute.For<IAuthorizationProviderConfigurationService>();
        _handler = new GetAuthorizationProviderConfigurationQueryHandler(_configurationService);
    }

    [Test]
    public async Task Handle_WhenNoCerbosEndpointExists_ReturnsConfigurationWithoutVerification()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "local",
            CerbosGrpcEndpoint = string.Empty
        };

        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _configurationService.IsConfiguredAsync().Returns(true);

        var result = await _handler.Handle(new GetAuthorizationProviderConfigurationQuery(), CancellationToken.None);

        await Assert.That(result.Provider).IsEqualTo("local");
        await _configurationService.DidNotReceive().VerifyCerbosEndpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDetectedEndpointVerifiesAndConfigurationIsUnset_AutoSelectsCerbos()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "local",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosDetectedFromEnvironment = true
        };

        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _configurationService.IsConfiguredAsync().Returns(false);
        _configurationService.VerifyCerbosEndpointAsync(configuration.CerbosGrpcEndpoint, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new GetAuthorizationProviderConfigurationQuery(), CancellationToken.None);

        await Assert.That(result.Provider).IsEqualTo("cerbos");
        await Assert.That(result.CerbosEndpointVerified).IsTrue();
    }

    [Test]
    public async Task Handle_WhenAlreadyConfigured_KeepsStoredProviderEvenIfEndpointVerifies()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "local",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosDetectedFromEnvironment = true
        };

        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _configurationService.IsConfiguredAsync().Returns(true);
        _configurationService.VerifyCerbosEndpointAsync(configuration.CerbosGrpcEndpoint, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new GetAuthorizationProviderConfigurationQuery(), CancellationToken.None);

        await Assert.That(result.Provider).IsEqualTo("local");
        await Assert.That(result.CerbosEndpointVerified).IsTrue();
    }
}
