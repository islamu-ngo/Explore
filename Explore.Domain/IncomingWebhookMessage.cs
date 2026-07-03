// ABOUTME: Tenant-scoped incoming integration callback ledger captured after raw-body signature verification.
// ABOUTME: Provides idempotency and safe processing state for Coop, Osprey, Svix operational callbacks, and future providers.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class IncomingWebhookMessage : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string Provider { get; set; }
    public required string ProviderMessageId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? EventType { get; set; }
    public string? HeadersJson { get; set; }
    public string? PayloadJson { get; set; }
    public required string PayloadHash { get; set; }
    public IncomingWebhookMessageStatus Status { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureCategory { get; set; }
    public string? SafeDetail { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public enum IncomingWebhookMessageStatus
{
    Received = 1,
    Verified = 2,
    Processing = 3,
    Processed = 4,
    Rejected = 5,
    Failed = 6,
    Duplicate = 7
}
