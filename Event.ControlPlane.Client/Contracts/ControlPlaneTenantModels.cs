// ABOUTME: Defines host-neutral tenant summary models for shared control-plane UI.
// ABOUTME: Carries HAL links from server adapters so tenant lifecycle actions remain API-authoritative.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneTenantList(
    IReadOnlyList<ControlPlaneTenantSummary> Items,
    int TotalCount,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneTenantList Empty() => new([], 0);
}

public sealed record ControlPlaneTenantSummary(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string? PrimaryHost = null,
    long? StorageBytes = null,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}
