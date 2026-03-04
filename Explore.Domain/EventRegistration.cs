using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventRegistration : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey("EventSession")]
    public Guid EventSessionId { get; set; }
    public required EventSession EventSession { get; set; }

    [ForeignKey("ApprovalStatus")]
    public int? ApprovalStatusId { get; set; }
    public ApprovalStatus? ApprovalStatus { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [ForeignKey("AtprotoRecord")]
    public Guid? AtprotoRecordId { get; set; }
    public AtprotoRecord? AtprotoRecord { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
