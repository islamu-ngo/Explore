// ABOUTME: Stores external API key credentials for direct machine callers across all ownership levels.
// ABOUTME: TenantId is nullable for platform-scoped InstanceAdmin keys; StatusId FK to lookup table; credit config for quota enforcement.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class ExternalApiKey : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string KeyId { get; set; }
    public required string SecretHash { get; set; }
    public required string Scopes { get; set; }

    public ExternalApiKeyOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }

    public int ExternalApiKeyStatusId { get; set; }
    public required ExternalApiKeyStatus ExternalApiKeyStatus { get; set; }

    public int ExternalApiKeyCreditPeriodId { get; set; }
    public required ExternalApiKeyCreditPeriod ExternalApiKeyCreditPeriod { get; set; }
    public int? CreditLimit { get; set; }
    public int? MaxRolloverCredits { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
