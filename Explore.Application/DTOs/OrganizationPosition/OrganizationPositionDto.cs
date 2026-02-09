namespace Explore.Application.DTOs.OrganizationPosition;

public class OrganizationPositionDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; } // For i18n with Tolgee
    public required string FullName { get; set; } // Fallback default
    public string? Description { get; set; }
}
