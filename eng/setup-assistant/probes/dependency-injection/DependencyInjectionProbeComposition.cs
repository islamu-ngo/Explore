// ABOUTME: Exercises only explicit Microsoft DI service registration and provider disposal.
// ABOUTME: Avoids Generic Host, configuration, logging, service location, and product dependencies.

namespace ISLAMU.Event.SetupAssistant.Probes.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

internal static class DependencyInjectionProbeComposition
{
    internal static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<DependencyInjectionProbeService>();
        return services.BuildServiceProvider();
    }
}

internal sealed class DependencyInjectionProbeService;
