// ABOUTME: Islamic extension entity for event sessions using strict 1:1 vertical partitioning.
// Stores prayer-relative scheduling and ritual requirements without bloating the core event_sessions table.

namespace Explore.Domain;

using System.ComponentModel.DataAnnotations.Schema;

public class EventSessionIslamicAspect
{
    /// <summary>
    /// Shared key with EventSession.Id (PK + FK).
    /// </summary>
    [ForeignKey(nameof(EventSession))]
    public Guid EventSessionId { get; set; }
    public EventSession? EventSession { get; set; }

    /// <summary>
    /// Session start-time strategy for Islamic scheduling.
    /// </summary>
    public SessionStartTimeType StartTimeType { get; set; } = SessionStartTimeType.RelativeToPrayer;

    /// <summary>
    /// Prayer reference used when StartTimeType is RelativeToPrayer.
    /// </summary>
    public PrayerTime? ReferencePrayer { get; set; }

    /// <summary>
    /// Offset in minutes from the referenced prayer.
    /// Positive = after prayer, negative = before prayer.
    /// </summary>
    public int? OffsetMinutes { get; set; }

    /// <summary>
    /// Indicates whether this session requires participants to have wudu.
    /// </summary>
    public bool RequiresWudu { get; set; }

    /// <summary>
    /// Optional JSON payload for module-specific ritual requirements.
    /// </summary>
    public string? RitualRequirementsJson { get; set; }
}
