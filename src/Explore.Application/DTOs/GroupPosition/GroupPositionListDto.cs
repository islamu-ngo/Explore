// ABOUTME: List DTO for GroupPosition lookup entity.
// ABOUTME: Identical structure to detail DTO — lookup tables use flat projection.

namespace Explore.Application.DTOs.GroupPosition;

public class GroupPositionListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
