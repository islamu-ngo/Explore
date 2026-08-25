namespace Explore.Application.DTOs.CategoryType;

public sealed record CategoryTypeDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
