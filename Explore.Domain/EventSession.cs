// ABOUTME: Scheduled event child content with UTC start/end as truth and cached local projection columns for queries.
// ABOUTME: Reschedule/ReprojectLocalTimes are the only supported domain write paths for derived local schedule fields.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Scheduling;

namespace Explore.Domain;

public class EventSession : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    [ForeignKey("Event")]
    public Guid EventId { get; set; }
    public required Event Event { get; set; }

    [ForeignKey("EventDay")]
    public Guid? EventDayId { get; set; }
    public EventDay? EventDay { get; set; }

    // Nullable for draft/unscheduled sessions. Scheduled sessions require both non-null with EndTime > StartTime.
    // When null, all cached local projections must also be null (enforced by ReprojectLocalTimes and DB check constraints).
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }

    // Cached local projections — written exclusively via ReprojectLocalTimes/Reschedule.
    // All null when session is unscheduled; complete and consistent when scheduled.
    public DateOnly? LocalStartDate { get; private set; }
    public DateOnly? LocalEndDate { get; private set; }
    public TimeOnly? LocalStartTime { get; private set; }
    public TimeOnly? LocalEndTime { get; private set; }
    public int? LocalStartMinuteOfDay { get; private set; }
    public int? LocalEndMinuteOfDay { get; private set; }

    public int SortOrder { get; set; }

    [ForeignKey("Location")]
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    [ForeignKey("Room")]
    public Guid? RoomId { get; set; }
    public LocationRoom? Room { get; set; }

    public string? Title { get; set; }

    public int? EventSessionKindId { get; set; }
    public EventSessionKind? EventSessionKind { get; set; }

    public int EventSessionStatusId { get; set; } = (int)EventSessionStatusEnum.Draft;
    public EventSessionStatus? EventSessionStatus { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
    public string? Slug { get; set; }
    public int? MaxAudienceAttendees { get; set; }
    public int? CurrentAudienceAttendees { get; set; }
    [ForeignKey("RegistrationMode")]
    public int? RegistrationModeId { get; set; }
    public RegistrationMode? RegistrationMode { get; set; }

    [ForeignKey("FeaturedImage")]
    public Guid? FeaturedImageId { get; set; }
    public StorageObject? FeaturedImage { get; set; }

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

    public Guid? SourceTemplateId { get; set; }
    public string? SourceTemplateKey { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public DateTimeOffset? InstantiatedFromTemplateAt { get; set; }
    public DateTimeOffset? LastSyncedFromTemplateAt { get; set; }

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

    public ICollection<EventSessionGroupSession> SessionGroups { get; set; } = new List<EventSessionGroupSession>();

    /// <summary>
    /// Re-projects cached local fields from the current UTC times and the supplied IANA timezone id.
    /// This is the single authorized write path for LocalStart*/LocalEnd* properties.
    /// Handlers, validators, mappers, and seeders must not write those fields directly.
    /// </summary>
    public void ReprojectLocalTimes(string timezoneId, IEventScheduleProjectionCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);

        // Unscheduled sessions: clear all local projections so DB check constraints pass.
        if (StartTime is null || EndTime is null)
        {
            LocalStartDate = null;
            LocalEndDate = null;
            LocalStartTime = null;
            LocalEndTime = null;
            LocalStartMinuteOfDay = null;
            LocalEndMinuteOfDay = null;
            return;
        }

        var projection = calculator.Project(StartTime.Value, EndTime.Value, timezoneId);
        LocalStartDate = projection.LocalStartDate;
        LocalEndDate = projection.LocalEndDate;
        LocalStartTime = projection.LocalStartTime;
        LocalEndTime = projection.LocalEndTime;
        LocalStartMinuteOfDay = projection.LocalStartMinuteOfDay;
        LocalEndMinuteOfDay = projection.LocalEndMinuteOfDay;
    }

    /// <summary>
    /// Reschedules UTC start/end and recomputes cached local fields in the event timezone.
    /// Handlers call this instead of writing StartTime/EndTime directly so local projection stays in sync.
    /// </summary>
    public void Reschedule(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string timezoneId,
        IEventScheduleProjectionCalculator calculator)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndTime must be strictly greater than StartTime.", nameof(endUtc));
        }

        StartTime = startUtc.ToUniversalTime();
        EndTime = endUtc.ToUniversalTime();
        ReprojectLocalTimes(timezoneId, calculator);
    }

    public bool ContributesToPublicScheduleSummary()
    {
        return !IsDeleted
            && EventSessionStatusId == (int)EventSessionStatusEnum.Published
            && StartTime is not null
            && EndTime is not null;
    }
}

public enum SessionStartTimeType
{
    Fixed = 0,
    RelativeToPrayer = 1
}
