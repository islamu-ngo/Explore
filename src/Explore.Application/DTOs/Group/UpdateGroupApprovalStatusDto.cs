namespace Explore.Application.DTOs.Group;

public sealed record UpdateGroupApprovalStatusDto
{
    public int ApprovalStatusId { get; init; }
    public string? ApprovalNotes { get; init; }
}
