// ABOUTME: Validates centralized Blazouter route registrations used by the client app.
// ABOUTME: Prevents regressions where valid pages exist but are missing from Routes.razor RouteConfig.

namespace Explore.Blazor.Client.Tests.Routing;

public class RoutesConfigurationTests
{
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
    public async Task Routes_ShouldInclude_MyRegistrations_Path()
    {
        var routesFilePath = FindRoutesFilePath();
        var routesContent = await File.ReadAllTextAsync(routesFilePath);

        await Assert.That(routesContent).Contains("Path = \"/my/registrations\"");
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
        const string eventDetailRoute = "Path = \"/events/:eventId\", Component = typeof(EventDetail)";

        await Assert.That(routesContent).Contains("@using Explore.Blazor.Client.Pages.Events.Sessions");
        await Assert.That(routesContent).Contains(createSessionRoute);
        await Assert.That(routesContent).Contains(editSessionRoute);

        await Assert.That(routesContent.IndexOf(createSessionRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(eventDetailRoute, StringComparison.Ordinal));
        await Assert.That(routesContent.IndexOf(editSessionRoute, StringComparison.Ordinal))
            .IsLessThan(routesContent.IndexOf(eventDetailRoute, StringComparison.Ordinal));
    }

    private static string FindRoutesFilePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Explore.Blazor.Client", "Routes.razor");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Explore.Blazor.Client/Routes.razor from test base directory.");
    }
}
