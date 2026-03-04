// ABOUTME: Detail DTO for GroupPosition lookup entity.
// ABOUTME: MasterCode supports i18n via Tolgee; FullName is fallback display.

namespace Explore.Application.DTOs.GroupPosition;

public class GroupPositionDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
