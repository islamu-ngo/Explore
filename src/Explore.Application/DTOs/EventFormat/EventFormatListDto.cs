namespace Explore.Application.DTOs.EventFormat;

public class EventFormatListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; } // For i18n with Tolgee
    public required string FullName { get; set; } // Fallback default
    public string? Description { get; set; }
}
