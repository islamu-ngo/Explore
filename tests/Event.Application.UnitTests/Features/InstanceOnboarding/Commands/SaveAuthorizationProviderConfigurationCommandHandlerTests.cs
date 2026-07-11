// ABOUTME: Unit tests for setup-time authorization provider saving and Cerbos verification behavior.
// ABOUTME: Verifies validation, local-provider persistence, and Cerbos endpoint normalization before save.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class SaveAuthorizationProviderConfigurationCommandHandlerTests
{
    private readonly IAuthorizationProviderConfigurationService _configurationService;
    private readonly ILogger<SaveAuthorizationProviderConfigurationCommandHandler> _logger;
    private readonly SaveAuthorizationProviderConfigurationCommandHandler _handler;

    public SaveAuthorizationProviderConfigurationCommandHandlerTests()
    {
        _configurationService = Substitute.For<IAuthorizationProviderConfigurationService>();
        _logger = Substitute.For<ILogger<SaveAuthorizationProviderConfigurationCommandHandler>>();

        _handler = new SaveAuthorizationProviderConfigurationCommandHandler(_configurationService, _logger);
    }

    [Test]
    public async Task Handle_WhenConfigurationIsInvalid_ReturnsValidationFailure()
    {
        var command = new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = new AuthorizationProviderConfigurationDto
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = string.Empty
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Invalid authorization provider configuration");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthorizationProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WhenProviderIsLocal_AppliesConfigurationWithoutVerification()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "local"
        };

        var result = await _handler.Handle(new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = configuration
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.DidNotReceive().VerifyCerbosEndpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _configurationService.Received(1).ApplyConfigurationAsync(configuration);
    }

    [Test]
    public async Task Handle_WhenCerbosEndpointVerifies_NormalizesEndpointAndMarksVerified()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.example.com:443"
        };

        _configurationService.VerifyCerbosEndpointAsync("https://cerbosgrpc.example.com:443", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = configuration
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.Received(1)
            .VerifyCerbosEndpointAsync("https://cerbosgrpc.example.com:443", Arg.Any<CancellationToken>());
        await _configurationService.Received(1)
            .ApplyConfigurationAsync(Arg.Is<AuthorizationProviderConfigurationDto>(x =>
                x.Provider == "cerbos"
                && x.CerbosGrpcEndpoint == "https://cerbosgrpc.example.com:443"
                && x.CerbosEndpointVerified));
    }

    [Test]
    public async Task Handle_WhenCerbosAdminEndpointIsRejected_ReturnsFailureWithoutSaving()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosAdminEndpoint = "https://127.0.0.1:3592",
            CerbosAdminUsername = "admin",
            CerbosAdminPassword = "secret"
        };

        _configurationService.VerifyCerbosEndpointAsync(configuration.CerbosGrpcEndpoint, Arg.Any<CancellationToken>())
            .Returns(true);
        _configurationService.VerifyCerbosAdminEndpointAsync(configuration.CerbosAdminEndpoint, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = configuration
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Admin API endpoint");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthorizationProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WhenCerbosAdminEndpointIsAllowed_PreservesWriteOnlyCredentialsForPersistence()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosAdminEndpoint = "https://tenant-cerbos.example.com:3592",
            CerbosAdminUsername = "admin",
            CerbosAdminPassword = "secret"
        };

        _configurationService.VerifyCerbosEndpointAsync(configuration.CerbosGrpcEndpoint, Arg.Any<CancellationToken>())
            .Returns(true);
        _configurationService.VerifyCerbosAdminEndpointAsync(configuration.CerbosAdminEndpoint, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = configuration
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.Received(1).ApplyConfigurationAsync(Arg.Is<AuthorizationProviderConfigurationDto>(x =>
            x.CerbosAdminEndpoint == "https://tenant-cerbos.example.com:3592"
            && x.CerbosAdminUsername == "admin"
            && x.CerbosAdminPassword == "secret"));
    }

    [Test]
    public async Task Handle_WhenCerbosEndpointDoesNotVerify_ReturnsFailureWithoutSaving()
    {
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443"
        };

        _configurationService.VerifyCerbosEndpointAsync(configuration.CerbosGrpcEndpoint, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new SaveAuthorizationProviderConfigurationCommand
        {
            Configuration = configuration
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("could not be verified");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthorizationProviderConfigurationDto>());
    }
}
