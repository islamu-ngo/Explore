namespace Explore.Blazor.Client.Models.DTOs;

public class EventDetailsDto
{
    // Program properties
    public Guid Id { get; set; }
    public int ProgramTypeId { get; set; }
    public string ProgramTypeFullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AudienceGenderId { get; set; }
    public string AudienceGenderFullName { get; set; } = string.Empty;
    public int AudienceAgeId { get; set; }
    public string AudienceAgeFullName { get; set; } = string.Empty;
    public int? AudienceAgeMinAge { get; set; }
    public int? AudienceAgeMaxAge { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationFullName { get; set; } = string.Empty;
    public int? AudienceAttendees { get; set; }
    public double Price { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }
    public bool? IsRegistrationRequired { get; set; }
    public int TotalViews { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public int? PostCode { get; set; }
    public string? Address { get; set; }
    public string? ProgramUrl { get; set; }

    // Event specific properties
    public int EventTypeId { get; set; }
    public string EventTypeFullName { get; set; } = string.Empty;
}
