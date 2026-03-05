using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Event : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }

    [ForeignKey("EventType")]
    public int? EventTypeId { get; set; }
    public EventType? EventType { get; set; }

    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }

    [ForeignKey("AudienceGender")]
    public int? AudienceGenderId { get; set; }
    public AudienceGender? AudienceGender { get; set; }

    [ForeignKey("AudienceAge")]
    public int? AudienceAgeId { get; set; }
    public AudienceAge? AudienceAge { get; set; }

    [ForeignKey("Actor")]
    public Guid ActorId { get; set; }
    public required Actor Actor { get; set; }

    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    [ForeignKey("FeaturedImage")]
    public Guid? FeaturedImageId { get; set; }
    public StorageObject? FeaturedImage { get; set; }

    public int TotalViews { get; set; }
    public bool IsRegistrationRequired { get; set; }
    public bool IsUserReported { get; set; }
    public string? EventUrl { get; set; }

    [ForeignKey("Madhab")]
    public int? MadhabId { get; set; }
    public Madhab? Madhab { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public string? Slug { get; set; }

    [ForeignKey("VisibilityType")]
    public int VisibilityTypeId { get; set; }
    public required VisibilityType VisibilityType { get; set; }

    public int? SessionCount { get; set; }

    [ForeignKey("EventStatus")]
    public int EventStatusId { get; set; }
    public required EventStatus EventStatus { get; set; }

    public string? ExternalRegistrationUrl { get; set; }
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    [ForeignKey("EventFormat")]
    public int EventFormatId { get; set; }
    public required EventFormat EventFormat { get; set; }

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

    // Concurrency control
    public Guid ConcurrencyStamp { get; set; }

    // ===== Aspect Navigation Properties =====
    // Optional 1:1 aspects - only present when event has specific characteristics

    /// <summary>
    /// Islamic aspect for events with Islamic characteristics.
    /// Only populated when event is associated with the Islamic module.
    /// </summary>
    public EventIslamicAspect? IslamicAspect { get; set; }

    /// <summary>
    /// Tech aspect for events with tech/developer characteristics.
    /// Only populated when event is associated with the Tech module.
    /// </summary>
    public EventTechAspect? TechAspect { get; set; }

    // Per-event appearance customization
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }

    [ForeignKey("BackgroundImage")]
    public Guid? BackgroundImageId { get; set; }
    public StorageObject? BackgroundImage { get; set; }
}
