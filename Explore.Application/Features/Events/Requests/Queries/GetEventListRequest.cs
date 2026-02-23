using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using Explore.Application.Specifications.Events;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public class GetEventListRequest : IRequest<PaginatedResult<EventListDto>>
{
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;

    // ===== Filter Parameters =====

    /// <summary>
    /// Free-text search across event title and description.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by category (via EventCategories junction table).
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Tag IDs to include (events must match these tags).
    /// Combined using <see cref="InclusionMode"/> (AND = all tags, OR = any tag).
    /// </summary>
    public List<Guid>? IncludedTagIds { get; set; }

    /// <summary>
    /// Tag IDs to exclude (events matching these tags are filtered out).
    /// Combined using <see cref="ExclusionMode"/> (AND = all tags, OR = any tag).
    /// </summary>
    public List<Guid>? ExcludedTagIds { get; set; }

    /// <summary>
    /// How included tags are combined. AND = event must have all tags. OR = event must have at least one.
    /// Defaults to AND.
    /// </summary>
    public TagFilterMode InclusionMode { get; set; } = TagFilterMode.And;

    /// <summary>
    /// How excluded tags are combined. OR = exclude if event has any tag. AND = exclude only if event has all tags.
    /// Defaults to OR.
    /// </summary>
    public TagFilterMode ExclusionMode { get; set; } = TagFilterMode.Or;

    /// <summary>
    /// Filter by event format (online, in-person, hybrid).
    /// </summary>
    public int? FormatId { get; set; }

    /// <summary>
    /// Filter by madhab.
    /// </summary>
    public int? MadhabId { get; set; }

    /// <summary>
    /// Filter by location (via EventSessions junction).
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Filter by registration mode (via EventSessions junction).
    /// </summary>
    public int? RegistrationModeId { get; set; }

    /// <summary>
    /// Filter by language (via EventSessions → EventSessionLanguages junction).
    /// </summary>
    public int? LanguageId { get; set; }

    /// <summary>
    /// Filter events with first session date on or after this date.
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Filter events with first session date on or before this date.
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Filter by event type.
    /// </summary>
    public int? EventTypeId { get; set; }

    /// <summary>
    /// Filter by audience gender.
    /// </summary>
    public int? AudienceGenderId { get; set; }

    /// <summary>
    /// Filter by audience age group.
    /// </summary>
    public int? AudienceAgeId { get; set; }

    /// <summary>
    /// Filter by event status.
    /// </summary>
    public int? EventStatusId { get; set; }

    // ===== Islamic Aspect Filter Parameters =====
    // These are only applied when the Islamic module is enabled for the tenant.

    /// <summary>
    /// Filter by Islamic aspect gender segregation mode.
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public int? GenderModeId { get; set; }

    /// <summary>
    /// Filter events that include Quran recitation (Islamic aspect).
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public bool? IncludesQuranRecitation { get; set; }

    /// <summary>
    /// Filter by reference prayer time for scheduling (Islamic aspect).
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public int? ReferencePrayerId { get; set; }

    /// <summary>
    /// Filter by Islamic content primary language (Islamic aspect).
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public int? IslamicPrimaryLanguageId { get; set; }

    /// <summary>
    /// Filter to only show events with an Islamic aspect configured.
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public bool? HasIslamicAspect { get; set; }

    // ===== Tech Aspect Filter Parameters =====
    // These are only applied when the Tech module is enabled for the tenant.

    /// <summary>
    /// Filter by required skill level (Tech aspect).
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public int? SkillLevelId { get; set; }

    /// <summary>
    /// Filter to only show coding competitions (Tech aspect).
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public bool? IsCodingCompetition { get; set; }

    /// <summary>
    /// Filter to only show hackathons (Tech aspect).
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public bool? IsHackathon { get; set; }

    /// <summary>
    /// Filter events that require a laptop (Tech aspect).
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public bool? RequiresLaptop { get; set; }

    /// <summary>
    /// Filter by tech stack tag (case-insensitive contains search on TechStackTags).
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public string? TechStackTag { get; set; }

    /// <summary>
    /// Filter to only show events with a Tech aspect configured.
    /// Only applied when the Tech module ("Mod_Tech") is enabled.
    /// </summary>
    public bool? HasTechAspect { get; set; }

    // ===== JSONB Metadata Filter Parameters =====

    /// <summary>
    /// JSON fragment for containment filter on MetadataJson JSONB column.
    /// Uses PostgreSQL @&gt; operator. Example: "{\"customField\": \"value\"}".
    /// </summary>
    public string? MetadataJsonContains { get; set; }

    /// <summary>
    /// Key existence check on MetadataJson JSONB column.
    /// Uses PostgreSQL ? operator. Example: "customField".
    /// </summary>
    public string? MetadataJsonKeyExists { get; set; }

    // ===== Sort Parameters =====

    /// <summary>
    /// Sort field name. Supported values: "date", "title", "views", "createdAt".
    /// Defaults to "date" (FirstSessionDate descending).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction. True for descending, false for ascending. Defaults to true.
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
