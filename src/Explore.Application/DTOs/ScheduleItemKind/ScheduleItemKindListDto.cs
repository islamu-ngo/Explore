namespace Explore.Application.DTOs.ScheduleItemKind;

public sealed record ScheduleItemKindListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
