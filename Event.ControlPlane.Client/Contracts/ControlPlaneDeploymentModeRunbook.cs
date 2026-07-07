// ABOUTME: Models the Control Plane deployment-mode migration runbook for shared Blazor pages.
// ABOUTME: Keeps mode-switch affordances HAL-driven while avoiding API transport details in the RCL.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneDeploymentModeRunbook(
    string CurrentMode,
    int ActiveTenantCount,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<ControlPlaneDeploymentModeTargetOption> TargetOptions,
    IReadOnlyList<ControlPlaneDeploymentModeRunbookStep> Steps,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyList<ControlPlaneDeploymentModeTargetOption> TargetOptions { get; init; } = TargetOptions ?? [];

    public IReadOnlyList<ControlPlaneDeploymentModeRunbookStep> Steps { get; init; } = Steps ?? [];

    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneDeploymentModeRunbook Empty() => new(
        string.Empty,
        0,
        DateTimeOffset.MinValue,
        [],
        []);
}

public sealed record ControlPlaneDeploymentModeTargetOption(
    string TargetMode,
    string Label,
    string Description,
    bool Allowed,
    string ConfirmationText,
    string? BlockingReason = null,
    string? Remediation = null);

public sealed record ControlPlaneDeploymentModeRunbookStep(
    string Key,
    string Title,
    string Description,
    string Severity = ControlPlaneSeverity.Info);
