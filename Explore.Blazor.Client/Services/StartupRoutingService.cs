// ABOUTME: Centralizes root/startup routing decisions using instance onboarding and public experience settings.

using Explore.Blazor.Client.Contracts.Providers;

namespace Explore.Blazor.Client.Services;

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

        var shellTask = _publicExperienceService.GetCachedShellAsync();
        var shell = shellTask is null ? null : await shellTask;
        string homeRoute;
        if (shell is not null)
        {
            homeRoute = _publicExperienceService.ResolveHomeRoute(shell);
        }
        else
        {
            var settingsTask = _publicExperienceService.GetCachedSettingsAsync();
            var settings = settingsTask is null ? null : await settingsTask;
            homeRoute = _publicExperienceService.ResolveHomeRoute(settings);
        }

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
