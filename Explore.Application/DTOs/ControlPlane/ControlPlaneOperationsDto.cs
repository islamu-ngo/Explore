// ABOUTME: Read model for multi-tenant control-plane operational status.
// ABOUTME: Exposes bounded job, outbox, email, and storage signals without payloads or secrets.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneOperationsDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<ControlPlaneOperationStatusDto> Statuses { get; set; } = [];
    public IReadOnlyList<ControlPlaneWarningDto> Warnings { get; set; } = [];
}

public sealed class ControlPlaneOperationStatusDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Message { get; set; }
    public IReadOnlyList<ControlPlaneOperationMetricDto> Metrics { get; set; } = [];
}

public sealed class ControlPlaneOperationMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long Value { get; set; }
    public bool IsCapped { get; set; }
}
