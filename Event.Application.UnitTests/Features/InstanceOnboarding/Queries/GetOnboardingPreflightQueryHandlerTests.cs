// ABOUTME: Unit tests for convention-first onboarding preflight read model checks.
// ABOUTME: Verifies blocking launch readiness and warning checks remain deterministic.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Queries;

public sealed class GetOnboardingPreflightQueryHandlerTests
{
    private readonly IInstanceBootstrapStateRepository _bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();

    [Test]
    public async Task Handle_WhenSingleTenantSetupIsActive_ReturnsPassingBlockingChecksAndWarnings()
    {
        var handler = CreateHandler(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://auth.example.org/realms/islamu",
            ["Keycloak:ClientId"] = "islamu-event-blazor",
            ["PublicBaseUrl"] = "https://events.example.org"
        });

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsTimedOut.Returns(false);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(true);
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns((Tenant?)null);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        using var cancellationSource = new CancellationTokenSource();
        var result = await handler.Handle(new GetOnboardingPreflightQuery(), cancellationSource.Token);

        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsReadyToLaunch).IsTrue();
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "setup_secret" && check.Status == OnboardingPreflightCheckStatus.Pass);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "default_tenant" && check.Status == OnboardingPreflightCheckStatus.Pass);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "auth_config" && check.Status == OnboardingPreflightCheckStatus.Pass);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "canonical_host" && check.Status == OnboardingPreflightCheckStatus.Pass);
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "smtp" && check.Status == OnboardingPreflightCheckStatus.Warning);
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "object_storage" && check.Status == OnboardingPreflightCheckStatus.Warning);
        await _bootstrapRepository.Received(1).GetCurrent(cancellationSource.Token);
    }

    [Test]
    public async Task Handle_WhenSetupSecretTimedOutAndAuthMissing_ReturnsBlockingFailures()
    {
        var handler = CreateHandler();

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(false);
        _setupSecretProvider.IsTimedOut.Returns(true);
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns((Tenant?)null);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await handler.Handle(new GetOnboardingPreflightQuery(), CancellationToken.None);

        await Assert.That(result.IsReadyToLaunch).IsFalse();
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "setup_secret" && check.Status == OnboardingPreflightCheckStatus.Fail);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "auth_config" && check.Status == OnboardingPreflightCheckStatus.Fail);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "canonical_host" && check.Status == OnboardingPreflightCheckStatus.Fail);
    }

    private GetOnboardingPreflightQueryHandler CreateHandler(Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? [])
            .Build();

        return new GetOnboardingPreflightQueryHandler(
            _bootstrapRepository,
            _deploymentModeProvider,
            _setupSecretProvider,
            _tenantRepository,
            _systemSettingRepository,
            configuration);
    }
}
