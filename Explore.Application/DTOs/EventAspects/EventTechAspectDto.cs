// ABOUTME: DTO for EventTechAspect containing tech/developer-specific event properties.
// Used when retrieving event details with Tech aspect data.

namespace Explore.Application.DTOs.EventAspects;

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
