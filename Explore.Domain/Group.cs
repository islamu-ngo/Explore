// ABOUTME: Domain entity representing an informal community group that can publish events.
// ABOUTME: Lighter alternative to Organization — no legal entity requirements (address, etc.).

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Group : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }

    [ForeignKey("ProfilePicture")]
    public Guid? ProfilePictureId { get; set; }
    public StorageObject? ProfilePicture { get; set; }

    [ForeignKey("ApprovalStatus")]
    public int ApprovalStatusId { get; set; }
    public required ApprovalStatus ApprovalStatus { get; set; }

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

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Concurrency control
    public Guid ConcurrencyStamp { get; set; }

    // Navigation property for members
    public ICollection<GroupMember>? Members { get; set; }
}
