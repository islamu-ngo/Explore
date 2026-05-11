namespace Explore.Application.DTOs.EventSessionKind;

public class EventSessionKindListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
