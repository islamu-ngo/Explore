// ABOUTME: Validates centralized Blazouter route registrations used by the client app.
// ABOUTME: Prevents regressions where valid pages exist but are missing from Routes.razor RouteConfig.

using Blazouter.Models;
using Explore.Blazor.Client.Routing;

namespace Explore.Blazor.Client.Tests.Routing;

public class RoutesConfigurationTests
{
    [Test]
    public async Task TicketRoutesPreservePublicRecoveryAndAuthenticatedAccountGuards()
    {
        string routes = await File.ReadAllTextAsync(FindRoutesFilePath());
        string recoveryLine = routes.Split('\n').Single(line =>
            line.Contains("Path = \"/tickets/recovery\"", StringComparison.Ordinal));
        string listLine = routes.Split('\n').Single(line =>
            line.Contains("Path = \"/tickets\"", StringComparison.Ordinal));
        string detailLine = routes.Split('\n').Single(line =>
            line.Contains("Path = \"/tickets/:ticketId\"", StringComparison.Ordinal));

        await Assert.That(routes).Contains("@using Explore.Blazor.Client.Pages.Tickets");
        await Assert.That(recoveryLine).DoesNotContain("RequireAuthenticated()");
        await Assert.That(listLine).Contains("Guards = RequireAuthenticated()");
        await Assert.That(detailLine).Contains("Guards = RequireAuthenticated()");
        await Assert.That(routes.IndexOf(recoveryLine, StringComparison.Ordinal))
            .IsLessThan(routes.IndexOf(detailLine, StringComparison.Ordinal));
    }

    [Test]
    public async Task Routes_ShouldInclude_OnboardingAuthProvider_Path()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/onboarding/auth-provider\"");
    }

    [Test]
    public async Task Routes_ShouldInclude_ErrorPage_Paths()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/errors/404\"");
        await Assert.That(routesContent).Contains("Path = \"/errors/403\"");
        await Assert.That(routesContent).Contains("Path = \"/errors/500\"");
    }

    [Test]
    public async Task Routes_ShouldNotInclude_RemovedMyRegistrations_Path()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).DoesNotContain("Path = \"/my/registrations\"");
        await Assert.That(routesContent).DoesNotContain("typeof(MyRegistrations)");
    }

    [Test]
    public async Task Routes_ShouldNotInclude_RemovedMyEvents_Path()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).DoesNotContain("Path = \"/my/events\"");
        await Assert.That(routesContent).DoesNotContain("typeof(MyEvents)");
    }

    [Test]
    public async Task Routes_ShouldNotInclude_RemovedWelcomeMarketingPage()
    {
        var routesContent = await File.ReadAllTextAsync(FindRoutesFilePath());

        await Assert.That(routesContent).DoesNotContain("Path = \"/welcome\"");
        await Assert.That(routesContent).DoesNotContain("typeof(LandingPageForNonUsers)");
    }

    [Test]
    public async Task StudioRoutes_ShouldUseAuthenticatedShellPagesOnly()
    {
        var routesContent = await File.ReadAllTextAsync(FindRoutesFilePath());

        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Studio");
        await Assert.That(routesContent).Contains("Path = \"/studio\", Component = typeof(StudioHome), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events\", Component = typeof(StudioEvents), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/schedule\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/registration\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/tickets\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/promotions\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/integrations\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        const string formsRoute = "Path = \"/studio/events/:eventId/forms\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()";
        const string checkInRoute = "Path = \"/studio/events/:eventId/check-in\", Component = typeof(StudioAdmissionCheckIn), Transition = RouteTransition.Fade, EnableCache = false, Guards = RequireAuthenticated()";
        const string genericRoute = "Path = \"/studio/events/:eventId\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()";
        await Assert.That(routesContent).Contains(formsRoute);
        await Assert.That(routesContent).Contains(checkInRoute);
        await Assert.That(routesContent.IndexOf(formsRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(genericRoute, StringComparison.Ordinal));
        await Assert.That(routesContent.IndexOf(checkInRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(genericRoute, StringComparison.Ordinal));
        await Assert.That(routesContent).Contains("Path = \"/studio/events/:eventId/publication\", Component = typeof(StudioEventShell), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
    }

    [Test]
    public async Task AiRoutes_ShouldUseAuthenticatedWorkspacePageOnly()
    {
        var routesContent = await File.ReadAllTextAsync(FindRoutesFilePath());

        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Ai");
        await Assert.That(routesContent).Contains("Path = \"/ai\", Component = typeof(AiWorkspace), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/ai/chats/:conversationId\", Component = typeof(AiWorkspace), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
    }

    [Test]
    public async Task SettingsRoutes_ShouldUseCanonicalPathsAndExistingGuardsOnly()
    {
        var routesContent = await File.ReadAllTextAsync(FindRoutesFilePath());
        var tenantNavigationPage = await File.ReadAllTextAsync(
            FindClientFilePath("Pages", "Admin", "Tenant", "Navigation.razor"));

        await Assert.That(routesContent).Contains("Path = \"/settings\", Component = typeof(Explore.Blazor.Client.Pages.User.Settings), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/settings/personal\", Component = typeof(Explore.Blazor.Client.Pages.User.Settings), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/settings/personal/:section\", Component = typeof(Explore.Blazor.Client.Pages.User.Settings), Transition = RouteTransition.Fade, Guards = RequireAuthenticated()");
        await Assert.That(routesContent).Contains("Path = \"/settings/organization/:OrganizationId\", Component = typeof(OrganizationAdminSettings), Transition = RouteTransition.Fade, Guards = RequireOrgAdmin()");
        await Assert.That(routesContent).Contains("Path = \"/settings/group/:GroupId\", Component = typeof(GroupAdminSettings), Transition = RouteTransition.Fade, Guards = RequireGroupAdmin()");
        await Assert.That(routesContent).Contains("Path = \"/settings/admin\", Component = typeof(TenantAdminSettings), Transition = RouteTransition.Fade, Guards = RequireTenantAdmin()");
        await Assert.That(routesContent).DoesNotContain("Path = \"/settings/tenant\"");
        await Assert.That(routesContent).Contains("Path = \"/settings/instance\", Component = typeof(InstanceAdminSettings), Transition = RouteTransition.Fade, Guards = RequireAdmin()");

        await Assert.That(routesContent).DoesNotContain(string.Concat("/admin/instance", "/settings"));
        await Assert.That(routesContent).DoesNotContain(string.Concat("/admin/tenant", "/settings"));
        await Assert.That(routesContent).DoesNotContain(string.Concat("/admin/organization/:OrganizationId", "/settings"));
        await Assert.That(routesContent).DoesNotContain(string.Concat("/admin/group/:GroupId", "/settings"));
        await Assert.That(tenantNavigationPage).DoesNotContain(string.Concat("@page \"/admin/tenant", "/navigation\""));
    }

    [Test]
    public async Task Routes_ShouldInclude_OrganizationCreate_Path_AndNoStaleSingularCreatePath()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/organizations/create\"");
        await Assert.That(routesContent).DoesNotContain("Path = \"/organization/create\"");
    }

    [Test]
    public async Task TenantOnboardingRoute_ShouldUse_MultiTenantOnboardingGuard()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/onboarding/tenant\"");
        await Assert.That(routesContent).Contains("RequireMultiTenantOnboarding()");
        await Assert.That(routesContent).Contains("typeof(MultiTenantOnboardingRouteGuard)");
    }

    [Test]
    public async Task OrganizationProfileRoutes_ShouldRemain_Public()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/organization/profile/:id\", Component = typeof(OrganizationProfile), Transition = RouteTransition.Fade }");
        await Assert.That(routesContent).Contains("Path = \"/organization/reviews/:id\", Component = typeof(OrganizationReviews), Transition = RouteTransition.Fade }");
    }

    [Test]
    public async Task EventSessionRoutes_ShouldBeRegistered_BeforeEventDetailCatchAll()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        const string createSessionRoute = "Path = \"/events/:eventId/sessions/create\", Component = typeof(CreateSession)";
        const string editSessionRoute = "Path = \"/events/:eventId/sessions/:sessionId/edit\", Component = typeof(EditSession)";
        const string detailSessionRoute = "Path = \"/events/:eventId/sessions/:sessionId\", Component = typeof(EventSessionDetail)";
        const string eventDetailRoute = "Path = \"/events/:slugCode\", Component = typeof(EventDetail)";

        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Events.Sessions");
        await Assert.That(routesContent).Contains(createSessionRoute);
        await Assert.That(routesContent).Contains(editSessionRoute);
        await Assert.That(routesContent).Contains(detailSessionRoute);

        await Assert.That(routesContent.IndexOf(createSessionRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(eventDetailRoute, StringComparison.Ordinal));
        await Assert.That(routesContent.IndexOf(editSessionRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(eventDetailRoute, StringComparison.Ordinal));
        await Assert.That(routesContent.IndexOf(detailSessionRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(eventDetailRoute, StringComparison.Ordinal));
    }

    [Test]
    public async Task ModerationReportQueueRoute_ShouldBind_EventId_ParameterName()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        const string moderationReportsRoute =
            "Path = \"/events/:EventId/moderation/reports\", Component = typeof(ModerationReportQueuePage)";

        await Assert.That(routesContent).Contains(moderationReportsRoute);
        await Assert.That(routesContent).DoesNotContain("Path = \"/events/:eventId/moderation/reports\"");
    }

    [Test]
    public async Task EventSessionDetail_ShouldNotExpose_EventModerationActions()
    {
        var sourcePath = FindClientFilePath("Pages", "Events", "Sessions", "EventSessionDetail.razor");
        var source = await File.ReadAllTextAsync(sourcePath);

        await Assert.That(source).DoesNotContain("moderate-light");
        await Assert.That(source).DoesNotContain("moderate-heavy");
        await Assert.That(source).DoesNotContain("Heavy Redact");
        await Assert.That(source).Contains("Complete");
        await Assert.That(source).Contains("Archive");
    }

    [Test]
    public async Task EmbeddedControlPlaneRoutes_ShouldUsePublicTenantPageAndMultiTenantAdminGuard()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Admin.Instance.ControlPlane");
        await Assert.That(routesContent).DoesNotContain("@using Event.ControlPlane.Client.Pages.Tenants");
        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Admin.Instance.ControlPlane");
        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Admin.Instance.ControlPlane");
        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Routing.ControlPlane");
        await Assert.That(routesContent).Contains("typeof(MultiTenantControlPlaneRouteGuard)");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.Overview, Component = typeof(ControlPlaneOverviewPage), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.Tenants, Component = typeof(InstanceTenants), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).DoesNotContain("Component = typeof(ControlPlaneTenantsPage)");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.TenantConfiguration, Component = typeof(InstanceTenantConfiguration), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).DoesNotContain("Component = typeof(ControlPlaneTenantConfigurationPage)");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.Plans, Component = typeof(InstancePlans), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).Contains("Path = \"/admin/instance/plans/:Key\", Component = typeof(InstancePlanDetail), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).DoesNotContain("Component = typeof(ControlPlanePlansPage)");
        await Assert.That(routesContent).DoesNotContain("Component = typeof(ControlPlanePlanDetailPage)");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.Domains, Component = typeof(ControlPlaneDomainsPage), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).Contains("Path = ControlPlaneRoutes.Operations, Component = typeof(ControlPlaneOperationsPage), Transition = RouteTransition.Fade, Guards = RequireMultiTenantAdmin()");
        await Assert.That(routesContent).Contains("Path = \"/settings/instance\", Component = typeof(InstanceAdminSettings), Transition = RouteTransition.Fade, Guards = RequireAdmin()");
    }

    [Test]
    public async Task RoutePathBase_WithTenantDocumentBase_PrefixesConfiguredRoutes()
    {
        var routes = new List<RouteConfig>
        {
            new() { Path = "/", Component = typeof(Routes) },
            new() { Path = "/settings/admin", Component = typeof(Routes) }
        };

        RouteConfigurationPathBase.Apply(routes, "https://event.test/t/acme/");

        await Assert.That(routes[0].Path).IsEqualTo("/t/acme");
        await Assert.That(routes[1].Path).IsEqualTo("/t/acme/settings/admin");
    }

    [Test]
    public async Task RoutePathBase_WithRootDocumentBase_PreservesConfiguredRoutes()
    {
        var routes = new List<RouteConfig>
        {
            new() { Path = "/settings/instance", Component = typeof(Routes) }
        };

        RouteConfigurationPathBase.Apply(routes, "https://event.test/");

        await Assert.That(routes[0].Path).IsEqualTo("/settings/instance");
    }

    [Test]
    public async Task RoutePathBase_WhenAppliedTwice_DoesNotDuplicatePrefix()
    {
        var routes = new List<RouteConfig>
        {
            new() { Path = "/settings", Component = typeof(Routes) }
        };

        RouteConfigurationPathBase.Apply(routes, "https://event.test/t/acme/");
        RouteConfigurationPathBase.Apply(routes, "https://event.test/t/acme/");

        await Assert.That(routes[0].Path).IsEqualTo("/t/acme/settings");
    }

    [Test]
    public async Task TypedOwnerSettingsRoutes_ShouldReadBlazouterRouteParameters()
    {
        var routesContent = await File.ReadAllTextAsync(FindRoutesFilePath());
        var organizationPage = await File.ReadAllTextAsync(
            FindClientFilePath("Pages", "Admin", "Organization", "OrganizationAdminSettings.razor"));
        var groupPage = await File.ReadAllTextAsync(
            FindClientFilePath("Pages", "Admin", "Group", "GroupAdminSettings.razor"));

        await Assert.That(routesContent).Contains("/settings/organization/:OrganizationId");
        await Assert.That(routesContent).Contains("/settings/group/:GroupId");
        await Assert.That(organizationPage).Contains("RouterState.GetParam(nameof(OrganizationId))");
        await Assert.That(groupPage).Contains("RouterState.GetParam(nameof(GroupId))");
    }

    private static string FindRoutesFilePath()
        => FindClientFilePath("Routes.razor");

    private static string FindClientFilePath(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, "src", "Explore.Blazor.Client", .. relativeSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate src/Explore.Blazor.Client/Routes.razor from test base directory.");
    }
}
