// ABOUTME: Query-bindable model for EventController.GetAll — collapses 40+ individual parameters into a single [FromQuery] target.
// ABOUTME: Transport concern only; mapped to GetEventListRequest (MediatR) in the controller.

using Explore.Application.DTOs.CustomPropertyProjection;

namespace Explore.API.Models;

public sealed class EventFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public List<Guid>? IncludedCategoryIds { get; set; }
    public List<Guid>? ExcludedCategoryIds { get; set; }
    public string? CategoryInclusionMode { get; set; }
    public string? CategoryExclusionMode { get; set; }
    public List<Guid>? IncludedTagIds { get; set; }
    public List<Guid>? ExcludedTagIds { get; set; }
    public string? InclusionMode { get; set; }
    public string? ExclusionMode { get; set; }
    public List<int>? FormatIds { get; set; }
    public List<int>? MadhabIds { get; set; }
    public List<Guid>? LocationIds { get; set; }
    public List<int>? RegistrationModeIds { get; set; }
    public List<int>? LanguageIds { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public List<int>? EventTypeIds { get; set; }
    public List<int>? AudienceGenderIds { get; set; }
    public List<int>? AudienceAgeIds { get; set; }
    public List<int>? EventStatusIds { get; set; }

    public List<int>? GenderModeIds { get; set; }
    public bool? IncludesQuranRecitation { get; set; }
    public List<int>? ReferencePrayerIds { get; set; }
    public List<int>? IslamicPrimaryLanguageIds { get; set; }
    public bool? HasIslamicAspect { get; set; }

    public int? SkillLevelId { get; set; }
    public bool? IsCodingCompetition { get; set; }
    public bool? IsHackathon { get; set; }
    public bool? RequiresLaptop { get; set; }
    public string? TechStackTag { get; set; }
    public bool? HasTechAspect { get; set; }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;

    /// <summary>
    /// Indexed query-string binding, e.g.
    /// <c>?CustomPropertyFilters[0].Namespace=tenant&amp;CustomPropertyFilters[0].Key=region&amp;CustomPropertyFilters[0].Operator=Equals&amp;CustomPropertyFilters[0].Value=west</c>.
    /// Silently ignored when tenant flag <c>custom_properties.projection_discovery_enabled</c> is off.
    /// </summary>
    public List<CustomPropertyFilterCriterion>? CustomPropertyFilters { get; set; }

    public string? CustomPropertySearchTerm { get; set; }
}
