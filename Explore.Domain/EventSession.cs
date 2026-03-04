using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventSession : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    [ForeignKey("Location")]
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public string? Title { get; set; }
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
    public string? Slug { get; set; }
    public int? MaxAudienceAttendees { get; set; }
    public int? CurrentAudienceAttendees { get; set; }
    [ForeignKey("RegistrationMode")]
    public int? RegistrationModeId { get; set; }
    public RegistrationMode? RegistrationMode { get; set; }

    /// <summary>
    /// Optional session-level pricing override.
    /// </summary>
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Optional Islamic extension stored in a dedicated vertical-partition table.
    /// </summary>
    public EventSessionIslamicAspect? IslamicAspect { get; set; }

    public string? Description { get; set; }

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
}

public enum SessionStartTimeType
{
    Fixed = 0,
    RelativeToPrayer = 1
}
