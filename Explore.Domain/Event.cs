// ABOUTME: Event aggregate root owning tenant-scoped event metadata, publication state, and schedule rollup projections.
// ABOUTME: UTC session instants are authoritative; timezone and local projection updates flow through aggregate methods.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Scheduling;

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
    public string? Content { get; set; }

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

    public ICollection<EventSession> Sessions { get; set; } = new List<EventSession>();
    public ICollection<EventSessionGroup> SessionGroups { get; set; } = new List<EventSessionGroup>();
    public ICollection<EventAgendaItem> AgendaItems { get; set; } = new List<EventAgendaItem>();
    public ICollection<EventDay> Days { get; set; } = new List<EventDay>();
    public ICollection<EventModerationRecord> ModerationRecords { get; set; } = new List<EventModerationRecord>();

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

    public Guid? SourceTemplateId { get; set; }
    public string? SourceTemplateKey { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public DateTimeOffset? InstantiatedFromTemplateAt { get; set; }
    public DateTimeOffset? LastSyncedFromTemplateAt { get; set; }

    // Temporal fields (UTC-based, computed from sessions)
    public DateTimeOffset? FirstSessionStartUtc { get; set; }
    public DateTimeOffset? LastSessionStartUtc { get; set; }
    public string? EventTimeZoneId { get; set; }

    // Provenance metadata for imported/backfilled events (Task 2.7 lifecycle policy)
    public string? ProvenanceSource { get; set; }
    public string? ProvenanceExternalId { get; set; }

    // Series
    [ForeignKey("EventSeries")]
    public Guid? EventSeriesId { get; set; }
    public EventSeries? EventSeries { get; set; }
    public int? SeriesOrder { get; set; }

    [ForeignKey("EventFormat")]
    public int EventFormatId { get; set; }
    public required EventFormat EventFormat { get; set; }

    [ForeignKey("RegistrationPolicy")]
    public int? RegistrationPolicyId { get; set; }
    public EventRegistrationPolicy? RegistrationPolicy { get; set; }

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

    public string GetEffectiveScheduleTimeZoneId()
    {
        return ScheduleTimeZoneResolver.NormalizeOrUtc(EventTimeZoneId ?? Timezone);
    }

    public void ApplyScheduleTimeZone(
        string? timezoneId,
        IEventScheduleProjectionCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);

        var canonicalTimeZoneId = ScheduleTimeZoneResolver.NormalizeOrUtc(timezoneId);
        EventTimeZoneId = canonicalTimeZoneId;
        Timezone = canonicalTimeZoneId;

        var daysByDate = Days
            .Where(day => !day.IsDeleted)
            .GroupBy(day => day.LocalDate)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(day => day.SortOrder).ThenBy(day => day.Id).First());

        foreach (var session in Sessions.Where(session => !session.IsDeleted))
        {
            session.ReprojectLocalTimes(canonicalTimeZoneId, calculator);
            session.EventDayId = session.LocalStartDate is not null && daysByDate.TryGetValue(session.LocalStartDate.Value, out var day) ? day.Id : null;
        }

        foreach (var agendaItem in AgendaItems.Where(item => !item.IsDeleted))
        {
            agendaItem.ReprojectLocalTimes(canonicalTimeZoneId, calculator);
            agendaItem.EventDayId = daysByDate.TryGetValue(agendaItem.LocalStartDate, out var day) ? day.Id : null;
        }

        RecalculateScheduleSummaryFromSessions();
    }

    public void RecalculateScheduleSummaryFromSessions()
    {
        var activeSessions = Sessions
            .Where(session => session.ContributesToPublicScheduleSummary())
            .OrderBy(session => session.StartTime)
            .ThenBy(session => session.SortOrder)
            .ThenBy(session => session.Id)
            .ToList();

        SessionCount = activeSessions.Count;

        if (activeSessions.Count == 0)
        {
            FirstSessionDate = null;
            LastSessionDate = null;
            FirstSessionStartUtc = null;
            LastSessionStartUtc = null;
            return;
        }

        var first = activeSessions.First();
        var last = activeSessions.Last();
        FirstSessionDate = first.LocalStartDate;
        LastSessionDate = last.LocalStartDate;
        FirstSessionStartUtc = first.StartTime;
        LastSessionStartUtc = last.StartTime;
    }
}
