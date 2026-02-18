// ABOUTME: Centralizes root/startup routing decisions using instance onboarding and public experience settings.

namespace Explore.Blazor.Client.Services;

public interface IStartupRoutingService
{
    Task<StartupRouteDecision> GetRootDecisionAsync();
}

public sealed class StartupRoutingService : IStartupRoutingService
{
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly IPublicExperienceService _publicExperienceService;

    public StartupRoutingService(
        IInstanceOnboardingService instanceOnboardingService,
        IPublicExperienceService publicExperienceService)
    {
        _instanceOnboardingService = instanceOnboardingService;
        _publicExperienceService = publicExperienceService;
    }

    public async Task<StartupRouteDecision> GetRootDecisionAsync()
    {
        var instanceStatus = await _instanceOnboardingService.GetStatusAsync();
        if (instanceStatus == null)
        {
            return StartupRouteDecision.PublicHome;
        }

        if (!instanceStatus.IsCompleted)
        {
            return StartupRouteDecision.Setup;
        }

        if (instanceStatus.IsAuthenticated &&
            instanceStatus.SelectedDeploymentMode?.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return StartupRouteDecision.InstanceAdmin;
        }

        var settings = await _publicExperienceService.GetCachedSettingsAsync();
        var homeRoute = _publicExperienceService.ResolveHomeRoute(settings);
        return homeRoute.Equals("/home", StringComparison.OrdinalIgnoreCase)
            ? StartupRouteDecision.PublicLanding
            : StartupRouteDecision.PublicHome;
    }
}

public enum StartupRouteDecision
{
    PublicHome,
    PublicLanding,
    Setup,
    InstanceAdmin
}
