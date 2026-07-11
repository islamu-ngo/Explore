// ABOUTME: List DTO for storage object metadata used by collection endpoints.
// ABOUTME: Includes provider, visibility, and lifecycle fields needed by local-first UI affordances.

namespace Explore.Application.DTOs.StorageObject;

public class StorageObjectListDto
{
    public Guid Id { get; set; }
    public int FileTypeId { get; set; }
    public string? FileTypeFullName { get; set; }
    public required string Uri { get; set; }
    public string? ObjectKey { get; set; }
    public required string Provider { get; set; }
    public required string FullName { get; set; }
    public required string SafeDisplayName { get; set; }
    public required string Extension { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public required string Visibility { get; set; }
    public required string Purpose { get; set; }
    public required string LifecycleState { get; set; }
    public Guid TenantId { get; set; }
}
