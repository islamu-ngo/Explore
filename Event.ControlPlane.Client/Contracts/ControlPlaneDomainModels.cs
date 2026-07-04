// ABOUTME: Defines host-neutral domain and DNS routing models for shared control-plane UI.
// ABOUTME: Carries HAL links from server adapters so domain actions stay server-authoritative.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneDomainList(
    IReadOnlyList<ControlPlaneDomainSummary> Items,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneDomainList Empty() => new([]);
}

public sealed record ControlPlaneDomainSummary(
    string Host,
    string Purpose,
    string Status,
    string? Target = null,
    string? VerificationMessage = null,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}
