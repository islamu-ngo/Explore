// ABOUTME: Input model for issuing an external API key for any of the five owner types.
// ABOUTME: Supports User, Organization, Group, Tenant, and InstanceAdmin keys with optional credit quota configuration.

namespace Explore.Application.DTOs.ExternalApiKey;

public sealed record CreateExternalApiKeyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int ExternalApiKeyOwnerTypeId { get; init; } = 1;
    public Guid? OrganizationId { get; init; }
    public Guid? GroupId { get; init; }
    public List<string> Scopes { get; init; } = [];
    public DateTime? ExpiresAt { get; init; }
    public int? CreditPeriodId { get; init; }
    public int? CreditLimit { get; init; }
    public int? MaxRolloverCredits { get; init; }
}
