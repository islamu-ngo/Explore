namespace Explore.Blazor.Client.Models.DTOs;

public class CreateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AudienceGenderId { get; set; }
    public int AudienceAgeId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? AudienceAttendees { get; set; }
    public double Price { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public bool? IsRegistrationRequired { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public int? PostCode { get; set; }
    public string? Address { get; set; }
    public string? ProgramUrl { get; set; }
    public int EventTypeId { get; set; }
}
