// ABOUTME: Read and command models for the Control Plane deployment-mode migration runbook.
// ABOUTME: Keeps single-tenant and multi-tenant transitions deliberate, audited, and server-authoritative.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneDeploymentModeRunbookDto
{
    public string CurrentMode { get; set; } = string.Empty;

    public int ActiveTenantCount { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<ControlPlaneDeploymentModeTargetOptionDto> TargetOptions { get; set; } = [];

    public List<ControlPlaneDeploymentModeRunbookStepDto> Steps { get; set; } = [];
}

public sealed class ControlPlaneDeploymentModeTargetOptionDto
{
    public string TargetMode { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Allowed { get; set; }

    public string ConfirmationText { get; set; } = string.Empty;

    public string? BlockingReason { get; set; }

    public string? Remediation { get; set; }
}

public sealed class ControlPlaneDeploymentModeRunbookStepDto
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = "info";
}

public sealed class ControlPlaneDeploymentModeTransitionRequestDto
{
    public string? TargetMode { get; set; }

    public string? Reason { get; set; }

    public string? ConfirmationText { get; set; }
}

public sealed class ControlPlaneDeploymentModeTransitionDto
{
    public string PreviousMode { get; set; } = string.Empty;

    public string NewMode { get; set; } = string.Empty;

    public int ActiveTenantCount { get; set; }

    public Guid OperatorUserId { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset TransitionedAtUtc { get; set; }
}
