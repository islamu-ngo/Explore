namespace Explore.Blazor.Client.Models.DTOs;

public class OrganizationCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public int? StatusTypeId { get; set; } = 1; // Default naar pending status
}

public class OrganizationDto : OrganizationCreateDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class StatusTypeDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}