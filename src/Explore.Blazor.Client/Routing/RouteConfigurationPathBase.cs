// ABOUTME: Applies the browser document path base to Blazouter route configurations.
// ABOUTME: Compensates for Blazouter matching absolute paths while preserving root-host behavior.

using Blazouter.Models;

namespace Explore.Blazor.Client.Routing;

public static class RouteConfigurationPathBase
{
    public static void Apply(IList<RouteConfig> routes, string baseUri)
    {
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var parsedBaseUri))
        {
            return;
        }

        var pathBase = parsedBaseUri.AbsolutePath.TrimEnd('/');
        if (pathBase.Length == 0)
        {
            return;
        }

        foreach (var route in routes)
        {
            if (route.Path.Equals(pathBase, StringComparison.OrdinalIgnoreCase) ||
                route.Path.StartsWith(pathBase + "/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            route.Path = route.Path == "/"
                ? pathBase
                : pathBase + "/" + route.Path.TrimStart('/');
        }
    }
}
