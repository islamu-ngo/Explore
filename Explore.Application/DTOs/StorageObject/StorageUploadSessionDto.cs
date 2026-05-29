// ABOUTME: Response DTO for upload session reservation state.
// ABOUTME: Exposes only application identifiers and policy metadata, not provider filesystem paths.

namespace Explore.Application.DTOs.StorageObject;

public class StorageUploadSessionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public required string Provider { get; set; }
    public long ExpectedSizeBytes { get; set; }
    public long ReservedBytes { get; set; }
    public required string ContentType { get; set; }
    public string? OriginalFileName { get; set; }
    public required string SafeDisplayName { get; set; }
    public string? Extension { get; set; }
    public required string Purpose { get; set; }
    public required string Visibility { get; set; }
    public required string Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? StorageObjectId { get; set; }
    public long? StoredSizeBytes { get; set; }
    public string? Sha256Checksum { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UploadStartedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public long MaxUploadBytes { get; set; }
    public long TenantQuotaBytes { get; set; }
    public long UsedBytes { get; set; }
    public long TotalReservedBytes { get; set; }
}
