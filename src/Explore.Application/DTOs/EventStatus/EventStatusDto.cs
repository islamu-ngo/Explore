namespace Explore.Application.DTOs.EventStatus;

public class EventStatusDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; } // For i18n with Tolgee
    public required string FullName { get; set; } // Fallback default
    public string? Description { get; set; }
}
