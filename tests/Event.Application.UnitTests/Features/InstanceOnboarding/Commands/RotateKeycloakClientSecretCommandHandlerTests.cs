// ABOUTME: Unit tests for explicit Keycloak client-secret rotation command behavior.
// ABOUTME: Verifies ownership boundaries, validation, application-managed persistence, and auth refresh.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class RotateKeycloakClientSecretCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000751");

    private readonly IAdminContext _adminContext;
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IKeycloakBootstrapService _keycloakBootstrapService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly RotateKeycloakClientSecretCommandHandler _handler;

    public RotateKeycloakClientSecretCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _configurationService = Substitute.For<IAuthProviderConfigurationService>();
        _keycloakBootstrapService = Substitute.For<IKeycloakBootstrapService>();
        _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
        var logger = Substitute.For<ILogger<RotateKeycloakClientSecretCommandHandler>>();

        _handler = new RotateKeycloakClientSecretCommandHandler(
            _adminContext,
            _configurationService,
            _keycloakBootstrapService,
            _jwtAuthorityRefreshNotifier,
            logger);
    }

    [Test]
    public async Task Handle_WhenUserIsNotInstanceAdmin_ReturnsBlockedWithoutSideEffects()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(CreateCommand(CreateApplicationManagedRequest()), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo("blocked");
        await Assert.That(result.Message).Contains("Only instance administrators");
        await _configurationService.DidNotReceive().ReadConfigurationAsync();
        await _keycloakBootstrapService.DidNotReceive().RotateClientSecretAsync(
            Arg.Any<AuthProviderConfigurationDto>(),
            Arg.Any<KeycloakClientSecretRotationRequestDto>(),
            Arg.Any<CancellationToken>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenServerReportsDeploymentManaged_IgnoresForgedApplicationManagedRequest()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        var request = CreateApplicationManagedRequest();
        var configuration = CreateConfiguration();
        configuration = configuration with { KeycloakClientSecretOwnership = new() { Mode = "deployment-managed" } };



        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _keycloakBootstrapService.RotateClientSecretAsync(
                configuration,
                request,
                Arg.Any<CancellationToken>())
            .Returns(new KeycloakClientSecretRotationResultDto
            {
                Status = "rotated",
                ClientId = configuration.KeycloakClientId,
                SecretOwnershipMode = "application-managed"
            });

        var result = await _handler.Handle(CreateCommand(request), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo("operator-action-required");
        await Assert.That(result.RequiresRestart).IsTrue();
        await Assert.That(result.OperatorInstructions).Contains("deployment secret provider");
        await _configurationService.Received(1).ReadConfigurationAsync();
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _keycloakBootstrapService.DidNotReceive().RotateClientSecretAsync(
            Arg.Any<AuthProviderConfigurationDto>(),
            Arg.Any<KeycloakClientSecretRotationRequestDto>(),
            Arg.Any<CancellationToken>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenApplicationManagedRotationSucceeds_PersistsNewSecretAndRefreshesAuthSchemes()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        var request = CreateApplicationManagedRequest();
        var configuration = CreateConfiguration();
        AuthProviderConfigurationDto? persistedConfiguration = null;

        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _keycloakBootstrapService.RotateClientSecretAsync(configuration, request, Arg.Any<CancellationToken>())
            .Returns(new KeycloakClientSecretRotationResultDto
            {
                Status = "rotated",
                Message = "Keycloak client secret was rotated and saved as an application-managed secret.",
                ClientId = configuration.KeycloakClientId,
                SecretOwnershipMode = "application-managed"
            });
        _configurationService.ApplyConfigurationAsync(Arg.Do<AuthProviderConfigurationDto>(x => persistedConfiguration = x))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(CreateCommand(request), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo("rotated");
        await Assert.That(result.AuthSchemesReloaded).IsTrue();
        await Assert.That(persistedConfiguration).IsNotNull();
        await Assert.That(persistedConfiguration!.KeycloakClientSecret).IsEqualTo(request.NewClientSecret);
        await _configurationService.Received(1).ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _jwtAuthorityRefreshNotifier.Received(1).ReloadAsync(Arg.Any<CancellationToken>());

        var serializedResult = JsonSerializer.Serialize(result);
        await Assert.That(serializedResult).DoesNotContain(request.NewClientSecret!);
        await Assert.That(serializedResult).DoesNotContain(request.BootstrapAdminPassword!);
    }

    [Test]
    public async Task Handle_WhenApplicationManagedProviderBlocksRotation_DoesNotPersistNewSecretOrRefreshAuthSchemes()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        var request = CreateApplicationManagedRequest();
        var configuration = CreateConfiguration();

        _configurationService.ReadConfigurationAsync().Returns(configuration);
        _keycloakBootstrapService.RotateClientSecretAsync(configuration, request, Arg.Any<CancellationToken>())
            .Returns(new KeycloakClientSecretRotationResultDto
            {
                Status = "blocked",
                Message = "Keycloak rejected the client-secret rotation request.",
                ClientId = configuration.KeycloakClientId,
                SecretOwnershipMode = "application-managed",
                Operations =
                [
                    new KeycloakRealmSyncOperationDto
                    {
                        OperationId = "keycloak-client-secret-update-failed",
                        Category = "client-secret",
                        TargetType = "client",
                        Target = configuration.KeycloakClientId,
                        Action = "update",
                        Status = "blocked",
                        Summary = "Keycloak client secret could not be updated.",
                        Reason = "Verify temporary admin permissions and retry."
                    }
                ]
            });

        var result = await _handler.Handle(CreateCommand(request), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo("blocked");
        await Assert.That(configuration.KeycloakClientSecret).IsEqualTo("old-client-secret");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());

        var serializedResult = JsonSerializer.Serialize(result);
        await Assert.That(serializedResult).DoesNotContain(request.NewClientSecret!);
        await Assert.That(serializedResult).DoesNotContain(request.BootstrapAdminPassword!);
    }

    [Test]
    public async Task Validator_WhenApplicationManagedInputsAreMissing_ReturnsFailures()
    {
        var validator = new KeycloakClientSecretRotationRequestDtoValidator();

        var result = await validator.ValidateAsync(new KeycloakClientSecretRotationRequestDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("Confirm that the platform should manage the new Keycloak client secret.");
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("New Keycloak client secret is required for application-managed rotation.");
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("Temporary Keycloak admin username is required for application-managed rotation.");
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("Temporary Keycloak admin password is required for application-managed rotation.");
    }

    private static RotateKeycloakClientSecretCommand CreateCommand(KeycloakClientSecretRotationRequestDto request)
    {
        return new RotateKeycloakClientSecretCommand
        {
            UserId = TestUserId,
            Request = request
        };
    }

    private static KeycloakClientSecretRotationRequestDto CreateApplicationManagedRequest()
    {
        return new KeycloakClientSecretRotationRequestDto
        {
            ClientId = "islamu-event-blazor",
            SecretOwnershipMode = "application-managed",
            ConfirmApplicationManagedSecret = true,
            NewClientSecret = "replacement-client-secret",
            BootstrapAdminUsername = "bootstrap-admin",
            BootstrapAdminPassword = "temporary-admin-secret"
        };
    }

    private static AuthProviderConfigurationDto CreateConfiguration()
    {
        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://keycloak.example.com/realms/ISLAMU",
            KeycloakClientId = "islamu-event-blazor",
            KeycloakClientSecret = "old-client-secret",
            KeycloakClientSecretOwnership = new()
            {
                Mode = "application-managed"
            }
        };
    }
}
