// ABOUTME: Defines the host-neutral route catalog contract for Event control-plane components.
// ABOUTME: Allows Blazor hosts to consume shared route metadata without owning route-string duplication.

namespace Explore.Blazor.Client.Routing.ControlPlane;

public interface IControlPlaneRouteCatalog
{
    string Root { get; }

    IReadOnlyList<ControlPlaneRouteDescriptor> All { get; }

    IReadOnlyList<ControlPlaneRouteDescriptor> Navigation { get; }

    IReadOnlyList<ControlPlaneRouteDescriptor> TenantNavigation { get; }
}
