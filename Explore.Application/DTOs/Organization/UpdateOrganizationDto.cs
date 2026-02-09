using System;

namespace Explore.Application.DTOs.Organization;

public class UpdateOrganizationDto
{
    public required string FullName { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public int Postcode { get; set; }
    public required string Address { get; set; }
    public string? MetadataJson { get; set; }
}
