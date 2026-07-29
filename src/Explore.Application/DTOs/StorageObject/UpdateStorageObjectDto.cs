// ABOUTME: Grouped PATCH contract for client-editable storage object metadata.
// ABOUTME: Excludes provider-owned location, integrity, lifecycle, tenant, and identity fields.

namespace Explore.Application.DTOs.StorageObject;

public sealed class UpdateStorageObjectDto
{
    public StorageObjectMetadataUpdateDto? Metadata { get; init; }
    public StorageObjectAccessUpdateDto? Access { get; init; }
    public StorageObjectOwnershipUpdateDto? Ownership { get; init; }
}

public sealed class StorageObjectMetadataUpdateDto
{
    public required string FullName { get; init; }
    public string? SafeDisplayName { get; init; }
}

public sealed class StorageObjectAccessUpdateDto
{
    public required string Visibility { get; init; }
    public required string Purpose { get; init; }
}

public sealed class StorageObjectOwnershipUpdateDto
{
    public string? OwningResourceKind { get; init; }
    public Guid? OwningResourceId { get; init; }
    public Guid? ActorId { get; init; }
}
