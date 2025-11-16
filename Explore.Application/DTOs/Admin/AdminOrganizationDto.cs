using System.ComponentModel.DataAnnotations;

namespace Explore.Application.DTOs.Admin;

/// <summary>
/// DTO for admin organization list - based on ERD organization table
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
    public DateTime CreatedAt { get; set; }
    public int StatusTypeId { get; set; }
    public string StatusName { get; set; } = string.Empty; // From status_type.full_name via JOIN
}

/// <summary>
/// DTO for updating organization status
/// </summary>
public class UpdateOrganizationStatusDto
{
    [Required]
    [Range(1, 3)] // 1=Pending, 2=Approved, 3=Rejected
    public int StatusTypeId { get; set; }
}