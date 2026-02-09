using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.StorageObject;

public class StorageObjectDto
{
    public Guid Id { get; set; }
    public int FileTypeId { get; set; }
    public string? FileTypeFullName { get; set; }
    public string? FileTypeMasterCode { get; set; } // For i18n with Tolgee
    public required string Uri { get; set; }
    public required string FullName { get; set; }
    public required string Extension { get; set; }
    public long Size { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantFullName { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
}
