using Explore.Blazor.Client.Models.Enums;

namespace Explore.Blazor.Client.Models.DTOs;

public class OrganizationCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Postcode { get; set; }
    public string Address { get; set; } = string.Empty;
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Postcode { get; set; }
    public string Address { get; set; } = string.Empty;
    public int StatusTypeId { get; set; }
    public string? StatusTypeName { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrganizationListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Postcode { get; set; }
    public string Address { get; set; } = string.Empty;
    public int StatusTypeId { get; set; }
    public string StatusTypeFullName { get; set; } = string.Empty;
    public OrganizationRole? CurrentUserRole { get; set; }
}

public class OrganizationStatusTypeListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}