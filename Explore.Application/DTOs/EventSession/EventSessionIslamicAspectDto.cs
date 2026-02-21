// ABOUTME: DTO for the event session Islamic extension (vertical partition).
// Captures prayer-relative scheduling and ritual requirements per session.

namespace Explore.Application.DTOs.EventSession;

using Explore.Domain;

public class EventSessionIslamicAspectDto
{
    public SessionStartTimeType StartTimeType { get; set; } = SessionStartTimeType.RelativeToPrayer;
    public PrayerTime? ReferencePrayer { get; set; }
    public int? OffsetMinutes { get; set; }
    public bool RequiresWudu { get; set; }
    public string? RitualRequirementsJson { get; set; }
}
