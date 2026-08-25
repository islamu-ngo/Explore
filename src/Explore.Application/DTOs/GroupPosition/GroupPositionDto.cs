// ABOUTME: Detail DTO for GroupPosition lookup entity.
// ABOUTME: MasterCode supports i18n via Tolgee; FullName is fallback display.

namespace Explore.Application.DTOs.GroupPosition;

public sealed record GroupPositionDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
