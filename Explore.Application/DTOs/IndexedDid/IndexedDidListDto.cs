namespace Explore.Application.DTOs.IndexedDid;

public class IndexedDidListDto
{
    public required string Did { get; set; }
    public string? Handle { get; set; }
    public required string PdsHost { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastIndexedAt { get; set; }
}
