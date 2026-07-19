// ABOUTME: Operator-safe status DTO for Basic Dispatch Mode email outbox rows.
// ABOUTME: Exposes lifecycle and retry fields while excluding recipient, body, subject, and provider secrets.

namespace Explore.Application.DTOs.EmailDispatch;

public sealed class EmailDispatchStatusDto
{
    public Guid OutboxId { get; set; }
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastFailureCategory { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime? UnknownAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ParkedAt { get; set; }
    public DateTime? ContentRedactedAt { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class EmailDispatchProcessorControlDto
{
    public string ProcessorCode { get; set; } = "smtp";
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public DateTime? PausedAt { get; set; }
    public int? GlobalSmtpRateLimitPerMinuteOverride { get; set; }
    public bool OptionalRemindersDeferred { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
