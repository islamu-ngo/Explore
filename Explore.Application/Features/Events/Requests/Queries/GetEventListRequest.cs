// ABOUTME: MediatR query request for fetching a filtered, paginated event list.
// ABOUTME: Returns PaginatedResult<EventListDto>.
using Explore.Application.DTOs.CustomPropertyProjection;
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
    /// Filter by actor ownership directly.
    /// </summary>
    public Guid? ActorId { get; set; }

    /// <summary>
    /// Filter by organization ownership after resolving the organization actor.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Filter by group ownership after resolving the group actor.
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Filter by category (via EventCategories junction table).
    /// When IncludedCategoryIds is also provided, this is ignored.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Category IDs to include (events must match these categories).
    /// Combined using <see cref="CategoryInclusionMode"/> (AND = all categories, OR = any category).
    /// </summary>
    public List<Guid>? IncludedCategoryIds { get; set; }

    /// <summary>
    /// Category IDs to exclude (events matching these categories are filtered out).
    /// Combined using <see cref="CategoryExclusionMode"/> (AND = all categories, OR = any category).
    /// </summary>
    public List<Guid>? ExcludedCategoryIds { get; set; }

    /// <summary>
    /// How included categories are combined. AND = event must have all categories. OR = event must have at least one.
    /// Defaults to AND.
    /// </summary>
    public TagFilterMode CategoryInclusionMode { get; set; } = TagFilterMode.And;

    /// <summary>
    /// How excluded categories are combined. OR = exclude if event has any category. AND = exclude only if event has all categories.
    /// Defaults to OR.
    /// </summary>
    public TagFilterMode CategoryExclusionMode { get; set; } = TagFilterMode.Or;

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
    /// Supports multiple values — events matching any of the specified formats are returned.
    /// </summary>
    public List<int>? FormatIds { get; set; }

    /// <summary>
    /// Filter by madhab.
    /// Supports multiple values — events matching any of the specified madhabs are returned.
    /// </summary>
    public List<int>? MadhabIds { get; set; }

    /// <summary>
    /// Filter by location (via EventSessions junction).
    /// Supports multiple values — events at any of the specified locations are returned.
    /// </summary>
    public List<Guid>? LocationIds { get; set; }

    /// <summary>
    /// Filter by registration mode (via EventSessions junction).
    /// Supports multiple values — events with any of the specified modes are returned.
    /// </summary>
    public List<int>? RegistrationModeIds { get; set; }

    /// <summary>
    /// Filter by language (via EventSessions → EventSessionLanguages junction).
    /// Supports multiple values — events in any of the specified languages are returned.
    /// </summary>
    public List<int>? LanguageIds { get; set; }

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
    /// Supports multiple values — events matching any of the specified types are returned.
    /// </summary>
    public List<int>? EventTypeIds { get; set; }

    /// <summary>
    /// Filter by audience gender.
    /// Supports multiple values — events matching any of the specified genders are returned.
    /// </summary>
    public List<int>? AudienceGenderIds { get; set; }

    /// <summary>
    /// Filter by audience age group.
    /// Supports multiple values — events matching any of the specified ages are returned.
    /// </summary>
    public List<int>? AudienceAgeIds { get; set; }

    /// <summary>
    /// Filter by event status.
    /// Supports multiple values — events matching any of the specified statuses are returned.
    /// </summary>
    public List<int>? EventStatusIds { get; set; }

    // ===== Islamic Aspect Filter Parameters =====
    // These are only applied when the Islamic module is enabled for the tenant.

    /// <summary>
    /// Filter by Islamic aspect gender segregation mode.
    /// Supports multiple values — events matching any of the specified modes are returned.
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public List<int>? GenderModeIds { get; set; }

    /// <summary>
    /// Filter events that include Quran recitation (Islamic aspect).
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public bool? IncludesQuranRecitation { get; set; }

    /// <summary>
    /// Filter by reference prayer time for scheduling (Islamic aspect).
    /// Supports multiple values — events matching any of the specified prayers are returned.
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public List<int>? ReferencePrayerIds { get; set; }

    /// <summary>
    /// Filter by Islamic content primary language (Islamic aspect).
    /// Supports multiple values — events matching any of the specified languages are returned.
    /// Only applied when the Islamic module ("Mod_Islamic") is enabled.
    /// </summary>
    public List<int>? IslamicPrimaryLanguageIds { get; set; }

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

    // ===== Custom Property Projection Filters (Layer 3 — tenant-gated) =====

    public List<CustomPropertyFilterCriterion>? CustomPropertyFilters { get; set; }

    public string? CustomPropertySearchTerm { get; set; }

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

    
    /// <summary>
    /// Gets or sets the temporal view filter.
    /// </summary>
    public TemporalView? View { get; set; }
}
