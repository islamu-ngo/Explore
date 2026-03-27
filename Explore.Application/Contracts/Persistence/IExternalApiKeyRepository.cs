// ABOUTME: Repository contract for persisted external API key lookups (tenant-scoped and platform-scoped).
// ABOUTME: Exposes explicit tenant-filter bypass paths for auth and platform (InstanceAdmin) key management.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface IExternalApiKeyRepository : IGenericRepository<ExternalApiKey, Guid>
{
    Task<ExternalApiKey?> GetByKeyIdForAuthentication(string keyId);
    Task<bool> TouchUsageMetadata(Guid id, DateTime usedAtUtc, string? lastUsedIp, TimeSpan minUpdateInterval, CancellationToken cancellationToken = default);
    Task<List<ExternalApiKey>> GetByOwner(ExternalApiKeyOwnerType ownerType, Guid ownerId);
    Task<List<ExternalApiKey>> GetByOwners(ExternalApiKeyOwnerType ownerType, IReadOnlyCollection<Guid> ownerIds);
    Task<bool> ExistsByOwnerAndName(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name);

    /// <summary>Lookup by ID bypassing tenant filter. Used by management handlers for platform-scoped keys.</summary>
    Task<ExternalApiKey?> GetByIdIgnoringTenantFilter(Guid id);

    /// <summary>List keys by owner bypassing tenant filter. Used for InstanceAdmin platform-scoped key listing.</summary>
    Task<List<ExternalApiKey>> GetByOwnerIgnoringTenantFilter(ExternalApiKeyOwnerType ownerType, Guid ownerId);

    /// <summary>Name uniqueness check bypassing tenant filter. Used for platform-scoped key creation.</summary>
    Task<bool> ExistsByOwnerAndNameIgnoringTenantFilter(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name);
}
