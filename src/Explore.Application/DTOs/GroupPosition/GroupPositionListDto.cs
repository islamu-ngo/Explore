// ABOUTME: List DTO for GroupPosition lookup entity.
// ABOUTME: Identical structure to detail DTO — lookup tables use flat projection.

namespace Explore.Application.DTOs.GroupPosition;

public sealed record GroupPositionListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
