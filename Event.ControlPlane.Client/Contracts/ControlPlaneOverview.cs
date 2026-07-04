// ABOUTME: Defines the host-neutral overview snapshot consumed by shared control-plane pages.
// ABOUTME: Keeps operational summary data separate from generated API DTOs and host transport details.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneOverview(
    string DeploymentMode,
    string? Version,
    string? PublicHost,
    string? AdminHost,
    IReadOnlyList<ControlPlaneStatusCard> StatusCards,
    IReadOnlyList<ControlPlaneWarning> Warnings,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}

public sealed record ControlPlaneStatusCard(
    string Key,
    string Label,
    string Value,
    string Severity = ControlPlaneSeverity.Neutral,
    string? Detail = null);

public sealed record ControlPlaneWarning(
    string Code,
    string Message,
    string Severity = ControlPlaneSeverity.Warning);
