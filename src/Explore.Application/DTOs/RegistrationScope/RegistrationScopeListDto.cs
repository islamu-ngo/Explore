namespace Explore.Application.DTOs.RegistrationScope;

public sealed record RegistrationScopeListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
