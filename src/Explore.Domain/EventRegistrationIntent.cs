// ABOUTME: Parent aggregate expressing why a user registered for an event (whole event, a specific day, or a chosen set of sessions).
// ABOUTME: Concrete session access stays on EventRegistration (child); this row preserves intent + organizer policy snapshot.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventRegistrationIntent : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }

    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey("RegistrationScope")]
    public int RegistrationScopeId { get; set; }
    public required RegistrationScope RegistrationScope { get; set; }

    /// <summary>
    /// Only populated when <see cref="RegistrationScopeId"/> points at <see cref="Enums.RegistrationScopeEnum.Day"/>.
    /// Links the intent to a specific <see cref="EventDay"/> so day-level capacity and business rules can key off it.
    /// </summary>
    [ForeignKey("SelectedEventDay")]
    public Guid? SelectedEventDayId { get; set; }
    public EventDay? SelectedEventDay { get; set; }

    /// <summary>
    /// Snapshot of the organizer-selected registration policy at the time the intent was created.
    /// Preserves "what the rules were when the user registered" even if the organizer later changes policy.
    /// </summary>
    [ForeignKey("RegistrationPolicySnapshot")]
    public int? RegistrationPolicySnapshotId { get; set; }
    public EventRegistrationPolicy? RegistrationPolicySnapshot { get; set; }

    [ForeignKey("ApprovalStatus")]
    public int? ApprovalStatusId { get; set; }
    public ApprovalStatus? ApprovalStatus { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
