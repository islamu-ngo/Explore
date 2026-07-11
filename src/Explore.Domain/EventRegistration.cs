// ABOUTME: Concrete per-session registration access row derived from a parent EventRegistrationIntent.
// ABOUTME: Denormalizes EventId and carries concurrency metadata for grouped PATCH updates.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventRegistration : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey("EventSession")]
    public Guid EventSessionId { get; set; }
    public required EventSession EventSession { get; set; }

    /// <summary>
    /// Parent registration-intent row. Nullable during rollout so existing session-level rows transition safely;
    /// newly created EventRegistration rows must always link to an <see cref="EventRegistrationIntent"/> once
    /// registration handlers land in a later slice.
    /// </summary>
    [ForeignKey("EventRegistrationIntent")]
    public Guid? EventRegistrationIntentId { get; set; }
    public EventRegistrationIntent? EventRegistrationIntent { get; set; }

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
