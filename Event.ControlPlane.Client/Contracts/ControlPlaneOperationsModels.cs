// ABOUTME: Defines host-neutral operations status models for the shared control-plane UI.
// ABOUTME: Keeps generated API operation DTOs out of the Razor class library boundary.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneOperations(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ControlPlaneOperationStatus> Statuses,
    IReadOnlyList<ControlPlaneWarning> Warnings,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneOperations Empty() => new(DateTimeOffset.MinValue, [], []);
}

public sealed record ControlPlaneOperationStatus(
    string Key,
    string Label,
    string Status,
    string Severity,
    string? Message = null,
    IReadOnlyList<ControlPlaneOperationMetric>? Metrics = null)
{
    public IReadOnlyList<ControlPlaneOperationMetric> Metrics { get; init; } = Metrics ?? [];
}

public sealed record ControlPlaneOperationMetric(
    string Key,
    string Label,
    long Value,
    bool IsCapped = false);
