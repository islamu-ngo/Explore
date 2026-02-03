// ABOUTME: DTO for EventIslamicAspect containing Islamic-specific event properties.
// Used when retrieving event details with Islamic aspect data.

namespace Explore.Application.DTOs.EventAspects;

using Explore.Domain;

/// <summary>
/// DTO representing the Islamic aspect of an event.
/// </summary>
public class EventIslamicAspectDto
{
    public int? MadhabId { get; set; }
    public string? MadhabName { get; set; }
    public PrayerTime? ReferencePrayer { get; set; }
    public int? PrayerTimeOffset { get; set; }
    public GenderSegregationMode GenderMode { get; set; }
    public string GenderModeName => GenderMode.ToString();
    public bool IncludesQuranRecitation { get; set; }
    public int? PrimaryLanguageId { get; set; }
    public string? PrimaryLanguageName { get; set; }
}

/// <summary>
/// DTO for creating or updating the Islamic aspect of an event.
/// </summary>
public class CreateUpdateIslamicAspectDto
{
    public int? MadhabId { get; set; }
    public PrayerTime? ReferencePrayer { get; set; }
    public int? PrayerTimeOffset { get; set; }
    public GenderSegregationMode GenderMode { get; set; } = GenderSegregationMode.Mixed;
    public bool IncludesQuranRecitation { get; set; }
    public int? PrimaryLanguageId { get; set; }
}
