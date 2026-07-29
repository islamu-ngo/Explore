// ABOUTME: Detailed storage object DTO with provider-neutral metadata for local-first file access.
// ABOUTME: Exposes safe display and lifecycle fields while keeping provider paths internal.

namespace Explore.Application.DTOs.StorageObject;

public class StorageObjectDto
{
    public Guid Id { get; set; }
    public int FileTypeId { get; set; }
    public string? FileTypeFullName { get; set; }
    public string? FileTypeMasterCode { get; set; } // For i18n with Tolgee
    public required string Uri { get; set; }
    public required string Provider { get; set; }
    public required string FullName { get; set; }
    public required string SafeDisplayName { get; set; }
    public required string Extension { get; set; }
    public string? ContentType { get; set; }
    public string? Sha256Checksum { get; set; }
    public long Size { get; set; }
    public required string Visibility { get; set; }
    public required string Purpose { get; set; }
    public required string LifecycleState { get; set; }
    public string? OwningResourceKind { get; set; }
    public Guid? OwningResourceId { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantFullName { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? QuarantinedAt { get; set; }
    public string? QuarantineReason { get; set; }
}
