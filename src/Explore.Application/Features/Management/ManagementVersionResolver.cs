// ABOUTME: Resolves the Event application version used by managed-control-plane contracts.
// ABOUTME: Centralizes assembly version discovery for both management commands and queries.

using System.Reflection;

namespace Explore.Application.Features.Management;

internal static class ManagementVersionResolver
{
    public static string EventVersion { get; } = ResolveEventVersion();

    private static string ResolveEventVersion()
    {
        var assembly = typeof(ManagementVersionResolver).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
