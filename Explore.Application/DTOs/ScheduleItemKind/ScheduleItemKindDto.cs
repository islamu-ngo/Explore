namespace Explore.Application.DTOs.ScheduleItemKind;

public class ScheduleItemKindDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
