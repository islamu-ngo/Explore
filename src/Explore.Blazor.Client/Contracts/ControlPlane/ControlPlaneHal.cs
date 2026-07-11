// ABOUTME: Provides HAL affordance checks for generated control-plane resources.
// ABOUTME: Keeps UI actions driven by generated API links rather than local authorization logic.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.ControlPlane;

public static class ControlPlaneHal
{
    public static bool HasLink(
        IDictionary<string, HalLink>? links,
        string relation) =>
        links?.ContainsKey(relation) == true;
}
