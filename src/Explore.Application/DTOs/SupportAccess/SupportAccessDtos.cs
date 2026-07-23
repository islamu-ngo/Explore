// ABOUTME: API-facing DTOs for support-access session management and audit review.
// ABOUTME: Exposes bounded support metadata while keeping Domain entities internal.

using Explore.Application.Responses;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.SupportAccess;

public sealed class StartSupportAccessSessionRequestDto
{
    public Guid TargetTenantId { get; set; }
    public Guid? TargetTenantUserId { get; set; }
    public SupportAccessModeEnum Mode { get; set; } = SupportAccessModeEnum.ReadOnly;
    public int DurationMinutes { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonText { get; set; } = string.Empty;
    public string? TicketReference { get; set; }
}

public sealed class StopSupportAccessSessionRequestDto
{
    public string? EndReasonText { get; set; }
}

public sealed class ForceStopSupportAccessSessionRequestDto
{
    public string? EndReasonText { get; set; }
}

public sealed class SupportAccessSessionDto
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid TargetTenantId { get; set; }
    public Guid? TargetTenantUserId { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int ModeId { get; set; }
    public string ModeName { get; set; } = string.Empty;
    public bool AllowsWrites { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string TicketReference { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public int? EndReasonId { get; set; }
    public string? EndReasonName { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SupportAccessSessionCommandResponseDto : BaseCommandResponse<Guid>
{
    public SupportAccessSessionDto? Session { get; set; }
}

public sealed class CurrentSupportAccessSessionDto
{
    public bool IsActive { get; set; }
    public SupportAccessSessionDto? Session { get; set; }
}

public sealed class SupportAccessAuditEventDto
{
    public Guid Id { get; set; }
    public Guid SupportAccessSessionId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public Guid TargetTenantId { get; set; }
    public Guid? TargetTenantUserId { get; set; }
    public string? RouteName { get; set; }
    public string? RequestName { get; set; }
    public string? ResourceKind { get; set; }
    public string? ResourceId { get; set; }
    public string? Action { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? SanitizedMetadataJson { get; set; }
}
