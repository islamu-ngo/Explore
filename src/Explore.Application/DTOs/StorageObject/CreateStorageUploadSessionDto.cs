// ABOUTME: Client payload for reserving tenant storage quota before accepting upload bytes.
// ABOUTME: Carries provider-neutral metadata used to validate and create an upload session.

using Explore.Domain;

namespace Explore.Application.DTOs.StorageObject;

public class CreateStorageUploadSessionDto
{
    public long ExpectedSizeBytes { get; set; }
    public required string ContentType { get; set; }
    public string? OriginalFileName { get; set; }
    public string? SafeDisplayName { get; set; }
    public string? Extension { get; set; }
    public string Purpose { get; set; } = StorageObjectPurposes.Attachment;
    public string Visibility { get; set; } = StorageObjectVisibilities.PrivateOwner;
    public string? OwningResourceKind { get; set; }
    public Guid? OwningResourceId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
