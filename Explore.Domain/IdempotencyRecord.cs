// ABOUTME: Domain entity for idempotency key tracking to prevent duplicate write operations.
// ABOUTME: Records are keyed by (Key, TenantId) and expire after 24 hours.

namespace Explore.Domain;

public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
