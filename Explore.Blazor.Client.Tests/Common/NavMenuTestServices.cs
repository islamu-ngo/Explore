// ABOUTME: Shared helper for registering NavMenu component dependencies in bUnit tests.
// ABOUTME: Consolidates duplicated service setup from NavMenuAdminTests and AuthenticationFlowTests.

using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Docking;
using NSubstitute;

namespace Explore.Blazor.Client.Tests.Common;

/// <summary>
/// Registers all services required by the NavMenu component.
/// Eliminates duplication across test files that render NavMenu or full-page layouts.
/// </summary>
/// <remarks>
    /// NavMenu injects: IUserService, IUserSettingsService, IPublicExperienceService, IInstanceOnboardingService,
/// ITenantNavigationService, IEventCreationEligibilityService, IOrganizationService,
/// IGroupService, SidebarState, NotificationService, ITranslationService, IHttpClientFactory.
/// </remarks>
public static class NavMenuTestServices
{
    /// <summary>
    /// Registers all NavMenu dependencies with sensible defaults.
    /// </summary>
    /// <param name="ctx">The test context to register services on.</param>
    /// <param name="publicExperienceSettings">
    /// Optional settings model. When null, GetSettingsAsync returns null (anonymous/default experience).
    /// Use <see cref="PublicExperienceSettingsBuilder"/> to construct complex configurations.
    /// </param>
    /// <param name="deploymentMode">
    /// Onboarding deployment mode. Defaults to "MultiTenant".
    /// Common values: "MultiTenant", "SingleTenant".
    /// </param>
    /// <param name="onboardingCompleted">
    /// Whether instance onboarding is complete. Defaults to true.
    /// Set to false to test pre-onboarding states.
    /// </param>
    public static void Register(
        BlazorTestContext ctx,
        PublicExperienceSettingsModel? publicExperienceSettings = null,
        string deploymentMode = "MultiTenant",
        bool onboardingCompleted = true,
        bool isCurrentUserInstanceAdmin = false,
        bool isCurrentUserTenantAdmin = false)
    {
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns((UserDto?)null);
        ctx.Services.AddSingleton(userService);

        var userSettingsService = Substitute.For<IUserSettingsService>();
        userSettingsService.GetSettingsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SettingGroupResponseDto?>(null));
        ctx.Services.AddSingleton(userSettingsService);

        var publicExperienceService = Substitute.For<IPublicExperienceService>();
        publicExperienceService.GetSettingsAsync()
            .Returns(Task.FromResult(publicExperienceSettings));
        publicExperienceService.ResolveHomeRoute(Arg.Any<PublicExperienceSettingsModel?>())
            .Returns(callInfo =>
            {
                var settings = callInfo.Arg<PublicExperienceSettingsModel?>();
                return settings?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true
                    ? "/home"
                    : "/events";
            });
        ctx.Services.AddSingleton(publicExperienceService);

        var instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = onboardingCompleted,
            IsAuthenticated = isCurrentUserInstanceAdmin,
            IsCurrentUserInstanceAdmin = isCurrentUserInstanceAdmin,
            SelectedDeploymentMode = deploymentMode
        });
        ctx.Services.AddSingleton(instanceOnboardingService);

        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = isCurrentUserTenantAdmin,
            IsCurrentUserTenantAdministrator = isCurrentUserTenantAdmin
        });
        ctx.Services.AddSingleton(tenantOnboardingService);

        var tenantNavigationService = Substitute.For<ITenantNavigationService>();
        tenantNavigationService.GetNavigationLinksAsync().Returns(new List<TenantNavigationLinkDto>());
        ctx.Services.AddSingleton(tenantNavigationService);

        var eligibilityService = Substitute.For<IEventCreationEligibilityService>();
        eligibilityService.GetEligibilityAsync().Returns(EventCreationEligibility.NotEligible);
        ctx.Services.AddSingleton(eligibilityService);

        var organizationService = Substitute.For<IOrganizationService>();
        organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());
        ctx.Services.AddSingleton(organizationService);

        var groupService = Substitute.For<IGroupService>();
        groupService.GetMyGroupsAsync().Returns(new List<GroupPublisherListDto>());
        ctx.Services.AddSingleton(groupService);

        ctx.Services.AddSingleton(new Explore.Blazor.Client.Services.SidebarState());
        ctx.Services.AddScoped<DockLayoutState>();
        ctx.Services.AddScoped<IDockPanelRegistry>(provider => provider.GetRequiredService<DockLayoutState>());
        ctx.Services.AddSingleton(MockServiceFactory.CreateNotificationService());
        ctx.Services.AddSingleton(MockServiceFactory.CreateTranslationService());
        ctx.Services.AddSingleton(Substitute.For<IHttpClientFactory>());
    }
}
