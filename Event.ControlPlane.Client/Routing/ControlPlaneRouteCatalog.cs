// ABOUTME: Provides a route catalog for Event control-plane navigation and host registration.
// ABOUTME: Centralizes shared route metadata so embedded and separate hosts do not duplicate route strings.

namespace Event.ControlPlane.Client.Routing;

public sealed class ControlPlaneRouteCatalog : IControlPlaneRouteCatalog
{
    private static readonly ControlPlaneRouteDescriptor[] Routes =
    [
        new(ControlPlaneRouteKeys.Overview, ControlPlaneRoutes.Overview),
        new(ControlPlaneRouteKeys.Tenants, ControlPlaneRoutes.Tenants),
        new(ControlPlaneRouteKeys.Domains, ControlPlaneRoutes.Domains),
        new(ControlPlaneRouteKeys.Onboarding, ControlPlaneRoutes.Onboarding),
        new(ControlPlaneRouteKeys.Health, ControlPlaneRoutes.Health),
        new(ControlPlaneRouteKeys.Storage, ControlPlaneRoutes.Storage),
        new(ControlPlaneRouteKeys.Jobs, ControlPlaneRoutes.Jobs),
        new(ControlPlaneRouteKeys.Security, ControlPlaneRoutes.Security),
        new(ControlPlaneRouteKeys.Policies, ControlPlaneRoutes.Policies)
    ];

    public string Root => ControlPlaneRoutes.Root;

    public IReadOnlyList<ControlPlaneRouteDescriptor> All => Routes;
}
