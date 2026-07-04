// ABOUTME: Defines the host-neutral route catalog contract for Event control-plane components.
// ABOUTME: Allows Blazor hosts to consume shared route metadata without owning route-string duplication.

namespace Event.ControlPlane.Client.Routing;

public interface IControlPlaneRouteCatalog
{
    string Root { get; }

    IReadOnlyList<ControlPlaneRouteDescriptor> All { get; }
}
