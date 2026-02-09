using System;

namespace Explore.Application.DTOs.StorageObject;

public class UpdateStorageObjectDto
{
    public Guid Id { get; set; }
    public int FileTypeId { get; set; }
    public required string Uri { get; set; }
    public required string FullName { get; set; }
    public required string Extension { get; set; }
    public long Size { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorId { get; set; }
}
