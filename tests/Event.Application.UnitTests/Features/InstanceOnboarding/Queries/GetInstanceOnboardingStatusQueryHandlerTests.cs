// ABOUTME: Unit tests for instance onboarding status setup-secret guidance.
// ABOUTME: Verifies non-sensitive operator recovery messaging for setup access states.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Handlers.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Queries;

public class GetInstanceOnboardingStatusQueryHandlerTests
{
    private readonly IInstanceBootstrapStateRepository _bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();

    [Test]
    public async Task Handle_WhenGeneratedSetupSecretActive_ReturnsSafeConfigurationGuidance()
    {
        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((Explore.Domain.InstanceBootstrapState?)null);
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(false);

        using var cancellationSource = new CancellationTokenSource();
        var result = await CreateHandler().Handle(new GetInstanceOnboardingStatusQuery(), cancellationSource.Token);

        await Assert.That(result.SetupSecretState).IsEqualTo("Generated");
        await Assert.That(result.SetupSecretGuidance).Contains("Configure SETUP_SECRET");
        await Assert.That(result.SetupSecretGuidance).DoesNotContain("startup logs");
        await _bootstrapRepository.Received(1).GetCurrent(cancellationSource.Token);
    }

    [Test]
    public async Task Handle_WhenEnvironmentSetupSecretActive_ReturnsEnvironmentGuidance()
    {
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(true);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(true);

        var result = await CreateHandler().Handle(new GetInstanceOnboardingStatusQuery(), CancellationToken.None);

        await Assert.That(result.SetupSecretState).IsEqualTo("Environment");
        await Assert.That(result.SetupSecretGuidance).Contains("SETUP_SECRET environment variable");
    }

    [Test]
    public async Task Handle_WhenSetupModeInactive_ReturnsSafeRestartGuidance()
    {
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _setupSecretProvider.IsSetupModeActive.Returns(false);
        _setupSecretProvider.IsFromEnvironmentVariable.Returns(false);

        var result = await CreateHandler().Handle(new GetInstanceOnboardingStatusQuery(), CancellationToken.None);

        await Assert.That(result.SetupSecretState).IsEqualTo("Unavailable");
        await Assert.That(result.SetupSecretGuidance).Contains("configure SETUP_SECRET");
        await Assert.That(result.SetupSecretGuidance).DoesNotContain("logged setup secret");
    }

    private GetInstanceOnboardingStatusQueryHandler CreateHandler() =>
        new(
            _bootstrapRepository,
            _adminContext,
            _systemSettingRepository,
            _currentUserService,
            _setupSecretProvider,
            _deploymentModeProvider);
}
