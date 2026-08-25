// ABOUTME: Read and command models for the Control Plane deployment-mode migration runbook.
// ABOUTME: Keeps single-tenant and multi-tenant transitions deliberate, audited, and server-authoritative.

namespace Explore.Application.DTOs.ControlPlane;

public sealed record ControlPlaneDeploymentModeRunbookDto
{
    public string CurrentMode { get; init; } = string.Empty;

    public int ActiveTenantCount { get; init; }

    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public List<ControlPlaneDeploymentModeTargetOptionDto> TargetOptions { get; init; } = [];

    public List<ControlPlaneDeploymentModeRunbookStepDto> Steps { get; init; } = [];
}

public sealed record ControlPlaneDeploymentModeTargetOptionDto
{
    public string TargetMode { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Allowed { get; init; }

    public string ConfirmationText { get; init; } = string.Empty;

    public string? BlockingReason { get; init; }

    public string? Remediation { get; init; }
}

public sealed record ControlPlaneDeploymentModeRunbookStepDto
{
    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Severity { get; init; } = "info";
}

public sealed record ControlPlaneDeploymentModeTransitionRequestDto
{
    public string? TargetMode { get; init; }

    public string? Reason { get; init; }

    public string? ConfirmationText { get; init; }
}

public sealed record ControlPlaneDeploymentModeTransitionDto
{
    public string PreviousMode { get; init; } = string.Empty;

    public string NewMode { get; init; } = string.Empty;

    public int ActiveTenantCount { get; init; }

    public Guid OperatorUserId { get; init; }

    public string? Reason { get; init; }

    public DateTimeOffset TransitionedAtUtc { get; init; }
}
