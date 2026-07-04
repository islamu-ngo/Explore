// ABOUTME: Registers host-neutral services required by the shared Event control-plane client library.
// ABOUTME: Provides a single DI entry point for embedded and separate Blazor hosts.

using Event.ControlPlane.Client.Routing;
using Event.ControlPlane.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.ControlPlane.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventControlPlaneClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IControlPlaneRouteCatalog, ControlPlaneRouteCatalog>();
        services.TryAddScoped<IControlPlaneOverviewService, UnconfiguredControlPlaneClient>();
        services.TryAddScoped<IControlPlaneTenantService, UnconfiguredControlPlaneClient>();
        services.TryAddScoped<IControlPlaneDomainService, UnconfiguredControlPlaneClient>();

        return services;
    }
}
