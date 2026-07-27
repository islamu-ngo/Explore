// ABOUTME: DTO for EventIslamicAspect containing Islamic-specific event properties.
// Used when retrieving event details with Islamic aspect data.

namespace Explore.Application.DTOs.EventAspects;

using Explore.Application.Models.Common;
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

public sealed class UpdateEventIslamicAspectDto
{
    public UpdateEventIslamicJurisprudenceDto? Jurisprudence { get; set; }
    public UpdateEventIslamicPrayerScheduleDto? PrayerSchedule { get; set; }
    public UpdateEventIslamicParticipationDto? Participation { get; set; }
    public UpdateEventIslamicLanguageDto? Language { get; set; }
}

public sealed class UpdateEventIslamicJurisprudenceDto
{
    public OptionalUpdate<int?> MadhabId { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public sealed class UpdateEventIslamicPrayerScheduleDto
{
    public OptionalUpdate<PrayerTime?> ReferencePrayer { get; set; } = OptionalUpdate<PrayerTime?>.Unspecified();
    public OptionalUpdate<int?> PrayerTimeOffset { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public sealed class UpdateEventIslamicParticipationDto
{
    public GenderSegregationMode? GenderMode { get; set; }
    public bool? IncludesQuranRecitation { get; set; }
}

public sealed class UpdateEventIslamicLanguageDto
{
    public OptionalUpdate<int?> PrimaryLanguageId { get; set; } = OptionalUpdate<int?>.Unspecified();
}
