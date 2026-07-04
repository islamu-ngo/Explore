// ABOUTME: Provides HAL link helpers for shared control-plane resources.
// ABOUTME: Enforces link-presence affordance checks without local role or claim inspection.

namespace Event.ControlPlane.Client.Contracts;

public static class ControlPlaneHal
{
    public static readonly IReadOnlyDictionary<string, ControlPlaneHalLink> EmptyLinks =
        new Dictionary<string, ControlPlaneHalLink>(StringComparer.OrdinalIgnoreCase);

    public static bool HasLink(
        IReadOnlyDictionary<string, ControlPlaneHalLink>? links,
        string relation) =>
        links?.ContainsKey(relation) == true;

    public static bool HasLink(IControlPlaneHalResource? resource, string relation) =>
        HasLink(resource?.Links, relation);
}
