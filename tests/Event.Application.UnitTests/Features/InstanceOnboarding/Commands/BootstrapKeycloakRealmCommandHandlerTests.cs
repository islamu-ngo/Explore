// ABOUTME: Unit tests for setup-time Keycloak bootstrap command handler behavior.
// ABOUTME: Verifies validation, safe failure handling, runtime config persistence, and no admin-secret persistence.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Onboarding;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class BootstrapKeycloakRealmCommandHandlerTests
{
    private readonly IKeycloakBootstrapService _keycloakBootstrapService;
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger;
    private readonly BootstrapKeycloakRealmCommandHandler _handler;

    public BootstrapKeycloakRealmCommandHandlerTests()
    {
        _keycloakBootstrapService = Substitute.For<IKeycloakBootstrapService>();
        _configurationService = Substitute.For<IAuthProviderConfigurationService>();
        _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
        _bootstrapAuditLogger = Substitute.For<IInstanceBootstrapAuditLogger>();
        var logger = Substitute.For<ILogger<BootstrapKeycloakRealmCommandHandler>>();

        _handler = new BootstrapKeycloakRealmCommandHandler(
            _keycloakBootstrapService,
            _configurationService,
            _jwtAuthorityRefreshNotifier,
            _bootstrapAuditLogger,
            logger);
    }

    [Test]
    public async Task Handle_WhenRequestIsInvalid_ReturnsValidationFailureWithoutSideEffects()
    {
        var command = new BootstrapKeycloakRealmCommand
        {
            BootstrapRequest = CreateValidRequest(
                keycloakBaseUrl: "file:///etc/passwd",
                bootstrapAdminPassword: string.Empty)
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("keycloak_bootstrap_validation_failed");
        await _keycloakBootstrapService.DidNotReceive()
            .BootstrapAsync(Arg.Any<KeycloakBootstrapRequestDto>(), Arg.Any<CancellationToken>());
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.KeycloakBootstrapFailed
            && auditEvent.Operation == "keycloak_bootstrap"
            && auditEvent.Outcome == "validation_failed"
            && auditEvent.FailureCode == "keycloak_bootstrap_validation_failed"
            && !AuditEventContains(auditEvent, command.BootstrapRequest.BootstrapAdminPassword)));
    }

    [Test]
    public async Task Handle_WhenBootstrapFails_ReturnsSafeFailureWithoutPersistingConfiguration()
    {
        var request = CreateValidRequest();
        _keycloakBootstrapService.BootstrapAsync(request, Arg.Any<CancellationToken>())
            .Returns(new KeycloakBootstrapResultDto
            {
                Success = false,
                FailureCode = "keycloak_admin_rejected",
                Message = "Keycloak rejected the bootstrap request."
            });

        var result = await _handler.Handle(new BootstrapKeycloakRealmCommand
        {
            BootstrapRequest = request
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("keycloak_admin_rejected");
        await Assert.That(result.Message).DoesNotContain(request.BootstrapAdminPassword);
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _jwtAuthorityRefreshNotifier.DidNotReceive().ReloadAsync(Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.KeycloakBootstrapStarted
            && auditEvent.Operation == "keycloak_bootstrap"
            && auditEvent.Outcome == "started"));
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.KeycloakBootstrapFailed
            && auditEvent.Operation == "keycloak_bootstrap"
            && auditEvent.Outcome == "failed"
            && auditEvent.FailureCode == "keycloak_admin_rejected"
            && !AuditEventContains(auditEvent, request.BootstrapAdminPassword)));
    }

    [Test]
    public async Task Handle_WhenBootstrapSucceeds_PersistsRuntimeAuthConfigurationOnly()
    {
        var request = CreateValidRequest();
        AuthProviderConfigurationDto? persistedConfiguration = null;

        _keycloakBootstrapService.BootstrapAsync(request, Arg.Any<CancellationToken>())
            .Returns(new KeycloakBootstrapResultDto
            {
                Success = true,
                Message = "Keycloak bootstrap completed successfully.",
                Realm = request.Realm,
                BlazorClientId = request.BlazorClientId,
                ApiClientId = request.ApiClientId,
                Mode = request.Mode,
                BlazorClientUpdated = true,
                ApiClientUpdated = true
            });

        _configurationService.ApplyConfigurationAsync(
                Arg.Do<AuthProviderConfigurationDto>(x => persistedConfiguration = x))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new BootstrapKeycloakRealmCommand
        {
            BootstrapRequest = request
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(persistedConfiguration).IsNotNull();
        await Assert.That(persistedConfiguration!.KeycloakEnabled).IsTrue();
        await Assert.That(persistedConfiguration.KeycloakAuthority).IsEqualTo("https://keycloak.example.com/realms/ISLAMU");
        await Assert.That(persistedConfiguration.KeycloakClientId).IsEqualTo(request.BlazorClientId);
        await Assert.That(persistedConfiguration.KeycloakClientSecret).IsEqualTo(request.BlazorClientSecret);
        await Assert.That(persistedConfiguration.KeycloakClientSecret).IsNotEqualTo(request.BootstrapAdminPassword);
        await Assert.That(persistedConfiguration.GoogleClientSecret).IsNotEqualTo(request.BootstrapAdminPassword);
        await _jwtAuthorityRefreshNotifier.Received(1).ReloadAsync(Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.KeycloakBootstrapStarted
            && auditEvent.Operation == "keycloak_bootstrap"
            && auditEvent.Outcome == "started"));
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.KeycloakBootstrapSucceeded
            && auditEvent.Operation == "keycloak_bootstrap"
            && auditEvent.Outcome == "succeeded"
            && auditEvent.Provider == "keycloak"
            && auditEvent.ClientId == request.BlazorClientId
            && !AuditEventContains(auditEvent, request.BootstrapAdminPassword)));
    }

    [Test]
    public async Task Validator_RejectsControlCharactersAndOversizedSecrets()
    {
        var validator = new KeycloakBootstrapRequestDtoValidator();
        var request = CreateValidRequest(realm: "ISLAMU\n", blazorClientSecret: new string('x', 4097));

        var result = await validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("Keycloak realm must not contain control characters.");
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("Blazor client secret is too long.");
    }

    private static KeycloakBootstrapRequestDto CreateValidRequest(
        string keycloakBaseUrl = "https://keycloak.example.com/",
        string realm = "ISLAMU",
        string blazorClientSecret = "runtime-blazor-secret",
        string bootstrapAdminPassword = "one-time-admin-secret")
    {
        return new KeycloakBootstrapRequestDto
        {
            KeycloakBaseUrl = keycloakBaseUrl,
            Realm = realm,
            BlazorClientId = "islamu-event-blazor",
            BlazorClientSecret = blazorClientSecret,
            ApiClientId = "islamu-event-api",
            ApiClientSecret = "optional-api-secret",
            Mode = KeycloakBootstrapMode.PatchExistingRealm,
            BootstrapAdminUsername = "bootstrap-admin",
            BootstrapAdminPassword = bootstrapAdminPassword
        };
    }

    private static bool AuditEventContains(InstanceBootstrapAuditEvent auditEvent, string? value)
    {
        return !string.IsNullOrEmpty(value)
            && auditEvent.ToString().Contains(value, StringComparison.Ordinal);
    }
}
