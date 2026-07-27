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
        await Assert.That(result.WarningChecks.Any(check => check.Code.StartsWith("dns_", StringComparison.Ordinal))).IsFalse();
        await _bootstrapRepository.Received(1).GetCurrent(cancellationSource.Token);
    }

    [Test]
    public async Task Handle_WhenMultiTenantSetupIsActive_ReturnsDnsChecklistWarnings()
    {
        var handler = CreateHandler(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://auth.example.org/realms/islamu",
            ["Keycloak:ClientId"] = "islamu-event-blazor",
            ["PublicBaseUrl"] = "https://events.example.org",
            ["ControlPlane:PublicOrigin"] = "https://admin.example.org"
        });

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.MultiTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(true);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.Domains.AllowTenantCustomDomain,
            Value = "true"
        });

        var result = await handler.Handle(new GetOnboardingPreflightQuery(), CancellationToken.None);

        await Assert.That(result.DeploymentMode).IsEqualTo("MultiTenant");
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "dns_public_platform" && check.Message.Contains("events.example.org", StringComparison.Ordinal));
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "dns_wildcard_tenant" && check.Message.Contains("*.events.example.org", StringComparison.Ordinal));
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "dns_control_plane" && check.Message.Contains("admin.example.org", StringComparison.Ordinal));
        await Assert.That(result.WarningChecks).Contains(check => check.Code == "dns_custom_domain_cname" && check.Message.Contains("Tenant custom domains are enabled", StringComparison.Ordinal));
    }

    [Test]
    public async Task Handle_WhenSetupModeInactiveAndAuthMissing_ReturnsBlockingFailures()
    {
        var handler = CreateHandler();

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(false);
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns((Tenant?)null);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await handler.Handle(new GetOnboardingPreflightQuery(), CancellationToken.None);

        await Assert.That(result.IsReadyToLaunch).IsFalse();
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "setup_secret" && check.Status == OnboardingPreflightCheckStatus.Fail);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "auth_config" && check.Status == OnboardingPreflightCheckStatus.Fail);
        await Assert.That(result.BlockingChecks).Contains(check => check.Code == "canonical_host" && check.Status == OnboardingPreflightCheckStatus.Fail);
    }

    [Test]
    public async Task Handle_WhenObjectStorageConfiguredInConfiguration_OmitsObjectStorageWarning()
    {
        var handler = CreateHandler(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://auth.example.org/realms/islamu",
            ["Keycloak:ClientId"] = "islamu-event-blazor",
            ["PublicBaseUrl"] = "https://events.example.org",
            ["STORAGE_S3_BUCKET_NAME"] = "my-cloud-bucket"
        });

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(true);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await handler.Handle(new GetOnboardingPreflightQuery(), CancellationToken.None);

        await Assert.That(result.WarningChecks.Any(check => check.Code == "object_storage")).IsFalse();
    }

    [Test]
    public async Task Handle_WhenObjectStorageConfiguredInS3ConfigResolver_OmitsObjectStorageWarning()
    {
        var s3ConfigResolver = Substitute.For<Explore.Application.Contracts.Infrastructure.IS3ConfigResolver>();
        s3ConfigResolver.IsConfiguredAsync(Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://auth.example.org/realms/islamu",
            ["Keycloak:ClientId"] = "islamu-event-blazor",
            ["PublicBaseUrl"] = "https://events.example.org"
        }, s3ConfigResolver);

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(true);
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await handler.Handle(new GetOnboardingPreflightQuery(), CancellationToken.None);

        await Assert.That(result.WarningChecks.Any(check => check.Code == "object_storage")).IsFalse();
    }

    private GetOnboardingPreflightQueryHandler CreateHandler(
        Dictionary<string, string?>? values = null,
        Explore.Application.Contracts.Infrastructure.IS3ConfigResolver? s3ConfigResolver = null)
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
            configuration,
            s3ConfigResolver);
    }
}
