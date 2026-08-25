namespace Explore.Application.DTOs.EventRegistrationPolicy;

public sealed record EventRegistrationPolicyDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
