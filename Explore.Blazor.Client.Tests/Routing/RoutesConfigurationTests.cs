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
