// ABOUTME: Stores tenant-bound external API key credentials for direct machine callers.
// ABOUTME: Persists only public key id and secret hash, keeping raw secrets out of storage and logs.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class ExternalApiKey : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public required string Name { get; set; }
    public required string KeyId { get; set; }
    public required string SecretHash { get; set; }
    public required string Scopes { get; set; }

    public ExternalApiKeyOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public ExternalApiKeyStatus Status { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
