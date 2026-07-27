// ABOUTME: DTO for EventTechAspect containing tech/developer-specific event properties.
// Used when retrieving event details with Tech aspect data.

namespace Explore.Application.DTOs.EventAspects;

using Explore.Application.Models.Common;
using Explore.Domain;

/// <summary>
/// DTO representing the Tech aspect of an event.
/// </summary>
public class EventTechAspectDto
{
    public string? GithubRepoUrl { get; set; }
    public string? HackathonTrack { get; set; }
    public SkillLevel SkillLevel { get; set; }
    public string SkillLevelName => SkillLevel.ToString();
    public string? TechStackTags { get; set; }
    public bool RequiresLaptop { get; set; }
    public bool IsCodingCompetition { get; set; }
    public int? MaxTeamSize { get; set; }
    public decimal? PrizePool { get; set; }
    public string? PrizeCurrencyCode { get; set; }
}

/// <summary>
/// DTO for creating or updating the Tech aspect of an event.
/// </summary>
public class CreateUpdateTechAspectDto
{
    public string? GithubRepoUrl { get; set; }
    public string? HackathonTrack { get; set; }
    public SkillLevel SkillLevel { get; set; } = SkillLevel.AllLevels;
    public string? TechStackTags { get; set; }
    public bool RequiresLaptop { get; set; }
    public bool IsCodingCompetition { get; set; }
    public int? MaxTeamSize { get; set; }
    public decimal? PrizePool { get; set; }
    public string? PrizeCurrencyCode { get; set; }
}

public sealed class UpdateEventTechAspectDto
{
    public UpdateEventTechRepositoryDto? Repository { get; set; }
    public UpdateEventTechClassificationDto? Classification { get; set; }
    public UpdateEventTechParticipationDto? Participation { get; set; }
    public UpdateEventTechPrizeDto? Prize { get; set; }
}

public sealed class UpdateEventTechRepositoryDto
{
    public OptionalUpdate<string?> GithubRepoUrl { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventTechClassificationDto
{
    public OptionalUpdate<string?> HackathonTrack { get; set; } = OptionalUpdate<string?>.Unspecified();
    public SkillLevel? SkillLevel { get; set; }
    public OptionalUpdate<string?> TechStackTags { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventTechParticipationDto
{
    public bool? RequiresLaptop { get; set; }
    public bool? IsCodingCompetition { get; set; }
}

public sealed class UpdateEventTechPrizeDto
{
    public OptionalUpdate<int?> MaxTeamSize { get; set; } = OptionalUpdate<int?>.Unspecified();
    public OptionalUpdate<decimal?> PrizePool { get; set; } = OptionalUpdate<decimal?>.Unspecified();
    public OptionalUpdate<string?> PrizeCurrencyCode { get; set; } = OptionalUpdate<string?>.Unspecified();
}
