namespace Explore.Blazor.Client.Models.DTOs;

/// <summary>
/// DTO pour admin organisatie lijst - gebaseerd op ERD organization tabel
/// </summary>
public class AdminOrganizationListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public int? Postcode { get; set; }
    public string? Address { get; set; }
    public int StatusTypeId { get; set; }
    public string StatusName { get; set; } = string.Empty; // Van status_type.full_name via JOIN
}

/// <summary>
/// DTO voor het updaten van organisatie status
/// </summary>
public class UpdateOrganizationStatusDto
{
    public int StatusTypeId { get; set; }
}