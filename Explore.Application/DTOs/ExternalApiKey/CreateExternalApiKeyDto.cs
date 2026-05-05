// ABOUTME: Input model for issuing an external API key for any of the five owner types.
// ABOUTME: Supports User, Organization, Group, Tenant, and InstanceAdmin keys with optional credit quota configuration.

namespace Explore.Application.DTOs.ExternalApiKey;

public class CreateExternalApiKeyDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int ExternalApiKeyOwnerTypeId { get; set; } = 1;
    public Guid? OrganizationId { get; set; }
    public Guid? GroupId { get; set; }
    public List<string> Scopes { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
    public int? CreditPeriodId { get; set; }
    public int? CreditLimit { get; set; }
    public int? MaxRolloverCredits { get; set; }
}
