using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class StorageObject : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey("FileType")]
    public int FileTypeId { get; set; }
    public required FileType FileType { get; set; }

    public required string Uri { get; set; }
    public required string FullName { get; set; }
    public required string Extension { get; set; }
    public long Size { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [ForeignKey("Actor")]
    public Guid? ActorId { get; set; }
    public Actor? Actor { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
