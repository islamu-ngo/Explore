// ABOUTME: Create DTO for storage object metadata after an upload is accepted.
// ABOUTME: Defaults new metadata to local provider, public-image visibility, and active lifecycle.

using Explore.Domain;

namespace Explore.Application.DTOs.StorageObject;

public class CreateStorageObjectDto
{
    public int FileTypeId { get; set; }
    public required string Uri { get; set; }
    public string? ObjectKey { get; set; }
    public string Provider { get; set; } = StorageProviders.Local;
    public required string FullName { get; set; }
    public string? SafeDisplayName { get; set; }
    public required string Extension { get; set; }
    public string? ContentType { get; set; }
    public string? Sha256Checksum { get; set; }
    public long Size { get; set; }
    public string Visibility { get; set; } = StorageObjectVisibilities.PublicImage;
    public string Purpose { get; set; } = StorageObjectPurposes.LegacyImage;
    public string LifecycleState { get; set; } = StorageObjectLifecycleStates.Active;
    public string? OwningResourceKind { get; set; }
    public Guid? OwningResourceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorId { get; set; }
}
