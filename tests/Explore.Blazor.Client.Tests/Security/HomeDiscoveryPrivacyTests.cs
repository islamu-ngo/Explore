// ABOUTME: Source and generated-client privacy guards for area-only browser home discovery.
// ABOUTME: Prevents raw origin from entering API parameters, URL/storage/analytics code, logs, or persistent state.

using System.Reflection;

namespace Explore.Blazor.Client.Tests.Security;

public sealed class HomeDiscoveryPrivacyTests
{
    [Test]
    public async Task GeneratedCompositeClientAcceptsOnlyCoarseAreaAndModeContext()
    {
        var method = typeof(IEventApiClient).GetMethod("GetHomeDiscoveryAsync")!;
        var parameterNames = method.GetParameters().Select(parameter => parameter.Name).ToArray();

        await Assert.That(parameterNames).Contains("areaId");
        await Assert.That(parameterNames).Contains("mode");
        await Assert.That(parameterNames).DoesNotContain("latitude");
        await Assert.That(parameterNames).DoesNotContain("longitude");
        await Assert.That(parameterNames).DoesNotContain("origin");
    }

    [Test]
    public async Task BrowserGeolocationModuleHasNoPersistenceNetworkAnalyticsOrLoggingSink()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Explore.Blazor.Client",
            "wwwroot",
            "js",
            "home-discovery.js"));

        foreach (var forbidden in new[]
                 {
                     "localStorage",
                     "sessionStorage",
                     "fetch(",
                     "XMLHttpRequest",
                     "sendBeacon",
                     "analytics",
                     "console.",
                     "location.href",
                     "history.pushState"
                 })
        {
            await Assert.That(source).DoesNotContain(forbidden);
        }
    }

    [Test]
    public async Task PersistentHomeStateContainsCompositePayloadButNoBrowserPosition()
    {
        var componentType = typeof(Explore.Blazor.Client.Components.Discovery.HomeDiscoveryExperience);
        var persistentProperties = componentType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.GetCustomAttribute<PersistentStateAttribute>() is not null)
            .ToArray();

        await Assert.That(persistentProperties.Length).IsEqualTo(1);
        await Assert.That(persistentProperties[0].PropertyType).IsEqualTo(typeof(HomeDiscoveryDto));
        await Assert.That(persistentProperties.Select(property => property.PropertyType))
            .DoesNotContain(typeof(Explore.Blazor.Client.Contracts.Interop.HomeDiscoveryGeolocationResult));
    }

    [Test]
    public async Task PersistedCompositePayloadRestoresBeforeAnyServiceAccess()
    {
        var expected = new HomeDiscoveryDto
        {
            Context = new HomeDiscoveryContextDto
            {
                Mode = HomeDiscoveryMode.Area,
                SelectedAreaDisplayName = "Brussels"
            }
        };
        var component = new Explore.Blazor.Client.Components.Discovery.HomeDiscoveryExperience
        {
            PersistedDiscovery = expected
        };
        var translation = Substitute.For<ITranslationService>();
        translation.T(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(call => call.ArgAt<string?>(1) ?? call.ArgAt<string>(0));
        component.GetType().GetProperty(
                "Translation",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(component, translation);
        var lifecycleMethod = component.GetType().GetMethod(
            "OnParametersSetAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)lifecycleMethod.Invoke(component, null)!;

        var restoredField = component.GetType().GetField(
            "_discovery",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        await Assert.That(restoredField.GetValue(component)).IsSameReferenceAs(expected);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
