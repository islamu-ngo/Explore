// ABOUTME: Records a user's explicit consent to share their email with an organisation for communications.
// ABOUTME: Scoped per-organizer (not per-event). Stores an email snapshot at grant time; never uses live PII.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventContactShareConsent : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    /// <summary>
    /// The event that triggered this consent. Informational only — not part of the uniqueness scope.
    /// Updated to the most recent event when consent is re-granted.
    /// </summary>
    [ForeignKey("SourceEvent")]
    public Guid? SourceEventId { get; set; }
    public Event? SourceEvent { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// The actor receiving the shared email. Schema is actor-based for future extensibility,
    /// but business logic currently restricts this to approved organisation actors only.
    /// </summary>
    [ForeignKey("RecipientActor")]
    public Guid RecipientActorId { get; set; }
    public Actor? RecipientActor { get; set; }

    /// <summary>
    /// The registration that triggered this consent. Nullable because the registration
    /// could be deleted while the consent audit trail must persist.
    /// </summary>
    [ForeignKey("SourceEventRegistration")]
    public Guid? SourceEventRegistrationId { get; set; }
    public EventRegistration? SourceEventRegistration { get; set; }

    public required string PurposeCode { get; set; }

    public ConsentStatus Status { get; set; }

    /// <summary>
    /// Copy of the user's email at the moment consent was granted.
    /// Never updated when the user changes their account email.
    /// </summary>
    public required string EmailSnapshot { get; set; }

    /// <summary>
    /// Lowercased version of EmailSnapshot for case-insensitive search/dedup.
    /// </summary>
    public required string EmailNormalizedSnapshot { get; set; }

    /// <summary>
    /// The exact consent text shown to the user at the time of granting.
    /// </summary>
    public required string ConsentTextSnapshot { get; set; }

    public required string ConsentUiVersion { get; set; }

    public DateTime GrantedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
