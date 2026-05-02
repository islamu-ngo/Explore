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
