// ABOUTME: Registers shared route metadata required by the embedded Event control-plane UI.
// ABOUTME: API-backed services are registered with the generated client in shared application services.

using Explore.Blazor.Client.Routing.ControlPlane;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.Client.Extensions;

public static class ControlPlaneServiceCollectionExtensions
{
    public static IServiceCollection AddEventControlPlaneClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IControlPlaneRouteCatalog, ControlPlaneRouteCatalog>();

        return services;
    }

}
