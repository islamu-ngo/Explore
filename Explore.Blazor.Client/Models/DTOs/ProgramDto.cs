namespace Explore.Blazor.Client.Models.DTOs;

public class ProgramListDto
{
    public Guid Id { get; set; }
    public int ProgramTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AudienceGenderId { get; set; }
    public int AudienceAgeId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? AudienceAttendees { get; set; }
    public double Price { get; set; }
    public Guid? FeaturedImageId { get; set; }
    
    // S3 Image Key for the program/event image
    public string? ImageKey { get; set; }
    
    public int TotalViews { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public int? PostCode { get; set; }
    public string? Address { get; set; }
    public string? ProgramUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ProgramDto : ProgramListDto
{
    public bool? IsRegistrationRequired { get; set; }
    public EventDto? Event { get; set; }
    public EducationDto? Education { get; set; }
}

public class EventDto
{
    public int EventTypeId { get; set; }
}

public class EducationDto
{
    public int EducationTypeId { get; set; }
}

public class EventTypeListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ProgramTypeListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
