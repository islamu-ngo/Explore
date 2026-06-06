// ABOUTME: Unit tests for root startup routing decisions after instance onboarding.
// ABOUTME: Guards against redirecting ordinary authenticated users into instance admin routes.

namespace Explore.Blazor.Client.Tests.Services;

public sealed class StartupRoutingServiceTests
{
    [Test]
    public async Task GetRootDecisionAsync_ReturnsPublicHome_ForAuthenticatedNonAdminInMultiTenant()
    {
        var instanceOnboarding = Substitute.For<IInstanceOnboardingService>();
        instanceOnboarding.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = false,
            SelectedDeploymentMode = "MultiTenant"
        });

        var publicExperience = CreatePublicExperienceService();
        var service = new StartupRoutingService(instanceOnboarding, publicExperience);

        var decision = await service.GetRootDecisionAsync();

        await Assert.That(decision).IsEqualTo(StartupRouteDecision.PublicHome);
    }

    [Test]
    public async Task GetRootDecisionAsync_ReturnsInstanceAdmin_ForAuthenticatedInstanceAdminInMultiTenant()
    {
        var instanceOnboarding = Substitute.For<IInstanceOnboardingService>();
        instanceOnboarding.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "MultiTenant"
        });

        var publicExperience = CreatePublicExperienceService();
        var service = new StartupRoutingService(instanceOnboarding, publicExperience);

        var decision = await service.GetRootDecisionAsync();

        await Assert.That(decision).IsEqualTo(StartupRouteDecision.InstanceAdmin);
    }

    private static IPublicExperienceService CreatePublicExperienceService()
    {
        var publicExperience = Substitute.For<IPublicExperienceService>();
        publicExperience.GetCachedShellAsync().Returns(Task.FromResult<PublicExperienceShellModel?>(null));
        publicExperience.GetCachedSettingsAsync().Returns(Task.FromResult<PublicExperienceSettingsModel?>(null));
        publicExperience.ResolveHomeRoute(Arg.Any<PublicExperienceShellModel?>()).Returns("/events");
        publicExperience.ResolveHomeRoute(Arg.Any<PublicExperienceSettingsModel?>()).Returns("/events");
        return publicExperience;
    }
}
