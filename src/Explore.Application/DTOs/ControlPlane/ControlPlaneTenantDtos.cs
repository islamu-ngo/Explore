// ABOUTME: Control-plane tenant lifecycle DTOs for instance-operator tenant management.
// ABOUTME: Exposes bounded tenant metadata, normalized status fields, and audit trail entries.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneTenantListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class ControlPlaneTenantDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public IReadOnlyList<ControlPlaneTenantLifecycleEntryDto> LifecycleHistory { get; set; } = [];
}

public sealed class ControlPlaneTenantLifecycleEntryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int? OldStatusId { get; set; }
    public string? OldStatusCode { get; set; }
    public string? OldStatusName { get; set; }
    public int NewStatusId { get; set; }
    public string NewStatusCode { get; set; } = string.Empty;
    public string NewStatusName { get; set; } = string.Empty;
    public Guid? TransitionedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime TransitionedAt { get; set; }
}

public sealed class ControlPlaneTenantLifecycleTransitionRequestDto
{
    public string? Reason { get; set; }
    public string? ConfirmationText { get; set; }
}

public sealed class ControlPlaneTenantLifecycleTransitionDto
{
    public Guid TenantId { get; set; }
    public int OldStatusId { get; set; }
    public string OldStatusCode { get; set; } = string.Empty;
    public string OldStatusName { get; set; } = string.Empty;
    public int NewStatusId { get; set; }
    public string NewStatusCode { get; set; } = string.Empty;
    public string NewStatusName { get; set; } = string.Empty;
    public Guid TransitionedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime TransitionedAt { get; set; }
}
